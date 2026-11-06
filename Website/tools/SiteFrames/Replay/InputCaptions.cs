using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace SiteFrames.Replay {
    /// <summary>One input annotation in player data: what was pressed or clicked to reach a moment.</summary>
    internal sealed record InputCaption(long Time, string Label, int? Column = null, int? Row = null, string? Kind = null);

    /// <summary>
    /// Turns a storyboard's capture scripts (the same scripted keys and pointer gestures that drew each state) into the
    /// captions the player shows: each state is labelled with the input it adds to the state before it, and the pointer
    /// marks the cell of its last mouse gesture. The keys that close the program, common to every state, are left out.
    /// </summary>
    internal static class InputCaptions {
        private static readonly HashSet<string> CloseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Escape", "Esc", "Ctrl+Q", "Ctrl+C" };
        private const int MaxLabel = 44;

        /// <summary>The annotation for each state (null where a state adds no input), timed by the caller.</summary>
        public static InputCaption?[] ForStates(JsonArray states, IReadOnlyList<long> times) {
            List<string>[] scripts = states.Select(state => Tokens((state?["startKeys"]?.GetValue<string>() ?? string.Empty) + " " + (state?["keys"]?.GetValue<string>() ?? string.Empty))).ToArray();
            // Drop the keys that close the program after each state is drawn.
            foreach (List<string> script in scripts) {
                while (script.Count > 0 && CloseKeys.Contains(script[script.Count - 1])) { script.RemoveAt(script.Count - 1); }
            }
            var captions = new InputCaption?[scripts.Length];
            for (int i = 0; i < scripts.Length; i++) {
                List<string> added = scripts[i];
                if (i > 0 && StartsWith(scripts[i], scripts[i - 1])) { added = scripts[i].Skip(scripts[i - 1].Count).ToList(); }
                added = added.Where(token => !token.StartsWith("wait:", StringComparison.OrdinalIgnoreCase)).ToList();
                // A state can name its input in words ("Drag the divider"); the pointer still comes from the script.
                string? caption = states[i]?["caption"]?.GetValue<string>();
                if (added.Count == 0 && caption == null) { continue; }
                string label = caption ?? Label(added);
                (int Column, int Row, string Kind)? point = null;
                foreach (string token in added) { if (Pointer(token) is { } p) { point = p; } }
                captions[i] = new InputCaption(times[i], label, point?.Column, point?.Row, point?.Kind);
            }
            return captions;
        }

        private static bool StartsWith(List<string> script, List<string> prefix) =>
            prefix.Count > 0 && script.Count >= prefix.Count && script.Take(prefix.Count).SequenceEqual(prefix, StringComparer.Ordinal);

        // The scripted-input grammar: whitespace-separated tokens, quoted text kept whole, paste:"…" kept whole.
        internal static List<string> Tokens(string script) {
            var tokens = new List<string>();
            int i = 0;
            while (i < script.Length) {
                if (char.IsWhiteSpace(script[i])) { i++; continue; }
                int start = i;
                char? quote = null;
                while (i < script.Length && (quote != null || !char.IsWhiteSpace(script[i]))) {
                    char c = script[i];
                    if (quote == null && (c == '"' || c == '\'')) { quote = c; }
                    else if (quote == c) { quote = null; }
                    else if (quote != null && c == '\\') { i++; }
                    i++;
                }
                string token = script.Substring(start, Math.Min(i, script.Length) - start);
                // Repeats expand, so a state that adds one more Down shows one Down.
                int star = token.LastIndexOf('*');
                if (token[0] != '"' && token[0] != '\'' && star > 0 && int.TryParse(token.AsSpan(star + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int count) && count <= 100) {
                    for (int r = 0; r < count; r++) { tokens.Add(token.Substring(0, star)); }
                } else { tokens.Add(token); }
            }
            return tokens;
        }

        private static (int Column, int Row, string Kind)? Pointer(string token) {
            int colon = token.IndexOf(':');
            if (colon <= 0) { return null; }
            string verb = token.Substring(0, colon).ToLowerInvariant();
            string kind = verb switch { "click" => "click", "rclick" => "right", "dclick" => "double", "wheel" => "wheel", "drag" => "drag", "move" => "move", _ => string.Empty };
            if (kind.Length == 0) { return null; }
            int[] n = token.Substring(colon + 1).Split(',').Select(part => int.Parse(part, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)).ToArray();
            return verb == "drag" ? (n[2], n[3], kind) : (n[0], n[1], kind);
        }

        private static string Name(string token) {
            if (token.Length > 1 && (token[0] == '"' || token[0] == '\'')) { return "“" + token.Substring(1, token.Length - 2) + "”"; }
            int colon = token.IndexOf(':');
            if (colon > 0) {
                string verb = token.Substring(0, colon).ToLowerInvariant();
                string rest = token.Substring(colon + 1);
                switch (verb) {
                    case "click": return "Click";
                    case "rclick": return "Right-click";
                    case "dclick": return "Double-click";
                    case "move": return "Point";
                    case "drag": return "Drag";
                    case "wheel": return rest.Split(',')[2].StartsWith("-", StringComparison.Ordinal) ? "Wheel ↓" : "Wheel ↑";
                    case "paste": return "Paste";
                    case "resize": return "Resize " + rest.Replace('x', '×');
                }
            }
            return token switch {
                "Down" => "↓", "Up" => "↑", "Left" => "←", "Right" => "→",
                "Shift+Left" => "Shift+←", "Shift+Right" => "Shift+→", "Shift+Up" => "Shift+↑", "Shift+Down" => "Shift+↓",
                "Ctrl+Shift+Left" => "Ctrl+Shift+←", "Ctrl+Shift+Right" => "Ctrl+Shift+→",
                "Escape" => "Esc", "Spacebar" => "Space", " " => "Space",
                _ => token
            };
        }

        private static string Label(List<string> tokens) {
            var parts = new List<string>();
            for (int i = 0; i < tokens.Count;) {
                string name = Name(tokens[i]);
                int run = 1;
                while (i + run < tokens.Count && Name(tokens[i + run]) == name && !name.StartsWith("“", StringComparison.Ordinal)) { run++; }
                parts.Add(run > 1 ? name + " ×" + run.ToString(CultureInfo.InvariantCulture) : name);
                i += run;
            }
            var label = new StringBuilder();
            foreach (string part in parts) {
                if (label.Length + part.Length + 1 > MaxLabel) { label.Append(" …"); break; }
                if (label.Length > 0) { label.Append(' '); }
                label.Append(part);
            }
            return label.ToString();
        }
    }
}
