using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// An immutable, validated theme document. JSON and data-dictionary imports share the same validation;
    /// loading never changes <see cref="Terminal.Theme"/>. PowerShell data-file syntax belongs to the PowerShell adapter.
    /// </summary>
    /// <remarks>
    /// Documents contain SchemaVersion (1), Name, optional BaseTheme and Tokens. Keys and token names are
    /// case-insensitive. BaseTheme must name a built-in preset; omitted tokens and style fields inherit from it.
    /// Without BaseTheme, omitted values use terminal defaults. An explicit null foreground/background clears inheritance.
    /// Exports contain every token, both colours, all fallback indices and decorations, without an inheritance dependency.
    /// </remarks>
    public sealed partial class ThemeFile {
        private readonly Theme _theme;

        private ThemeFile(Theme theme) { _theme = theme; }

        /// <summary>The name used when the document is converted to a theme.</summary>
        public string Name => _theme.Name;

        /// <summary>Returns the immutable theme represented by this document without changing the active theme.</summary>
        /// <returns>A complete theme, including resolved inheritance.</returns>
        public Theme ToTheme() => _theme;

        /// <summary>Captures a theme for lossless export.</summary>
        /// <param name="theme">A built-in or custom theme. Custom implementations must be representable by ThemeStyle.</param>
        /// <returns>A validated snapshot independent of subsequent changes to a custom theme.</returns>
        /// <exception cref="ArgumentException">A custom theme uses tier-specific decorations or colours that cannot be represented losslessly.</exception>
        /// <exception cref="FormatException">A name, fallback index or decoration is invalid.</exception>
        public static ThemeFile FromTheme(ITheme theme) {
            if (theme == null) { throw new ArgumentNullException(nameof(theme)); }
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in Theme.AllTokens()) {
                styles.Add(token, theme is Theme concrete ? concrete.Get(token) : Capture(theme, token));
            }
            // Apply the same rules as file import, including validation of programmatically created ThemeColor values.
            return Import(Data(new Theme(theme.Name, styles)));
        }

        /// <summary>Parses JSON and validates every key, token, colour, fallback and decoration before returning.</summary>
        /// <param name="json">One JSON theme object. Comments, duplicate keys and trailing content are rejected.</param>
        /// <returns>A validated document.</returns>
        /// <exception cref="FormatException">The JSON or theme data is invalid.</exception>
        public static ThemeFile Parse(string json) {
            object? value = MiniJson.Read(json);
            if (!(value is IDictionary data)) { throw Error("Theme", "must be an object"); }
            return Import(data);
        }

        /// <summary>Reads a UTF-8 JSON theme file without changing global state.</summary>
        /// <param name="path">A file-system path. PSD1 must be read by the PowerShell adapter.</param>
        /// <returns>The validated document.</returns>
        /// <exception cref="FormatException">The document is invalid.</exception>
        public static ThemeFile Load(string path) => Parse(File.ReadAllText(path, new UTF8Encoding(false, true)));

        /// <summary>Validates data supplied by an adapter, such as a safely imported PowerShell hashtable.</summary>
        /// <param name="data">SchemaVersion (optional, defaults to 1), Name, optional BaseTheme, and Tokens.</param>
        /// <returns>A complete, validated document; the input dictionary is not retained or modified.</returns>
        /// <exception cref="FormatException">An unknown key, token, preset or invalid value is present.</exception>
        public static ThemeFile Import(IDictionary data) {
            Dictionary<string, object?> root = Map(data, "Theme", "SchemaVersion", "Name", "BaseTheme", "Tokens");
            if (root.TryGetValue("SchemaVersion", out object? version)) { Integer(version, "SchemaVersion", 1, 1); }
            string name = RequiredString(Get(root, "Name"), "Name");
            Theme? basis = root.TryGetValue("BaseTheme", out object? baseName) ? Preset(RequiredString(baseName, "BaseTheme")) : null;
            Dictionary<string, object?> tokens = Map(Get(root, "Tokens"), "Tokens");
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in Theme.AllTokens()) { styles.Add(token, basis?.Get(token) ?? default); }
            HashSet<ThemeToken> specified = new HashSet<ThemeToken>();
            foreach (KeyValuePair<string, object?> pair in tokens) {
                if (!Theme.TryParseToken(pair.Key, out ThemeToken token)) { throw Error("Tokens." + pair.Key, "is an unknown theme token"); }
                if (!specified.Add(token)) { throw Error("Tokens." + pair.Key, "is a duplicate theme token"); }
                styles[token] = ReadStyle(pair.Value, "Tokens." + pair.Key, styles[token]);
            }
            return new ThemeFile(new Theme(name, styles));
        }

        /// <summary>Returns a complete data-only dictionary suitable for an adapter's native serialization.</summary>
        /// <returns>A fresh dictionary; editing it cannot modify this document.</returns>
        public IDictionary<string, object?> ToData() => Data(_theme);

        /// <summary>Writes complete, indented JSON with every token and explicit fallback tier.</summary>
        /// <returns>The JSON document, ending in a newline.</returns>
        public string ToJson() => MiniJson.Write(Data(_theme)) + "\n";

        /// <summary>Writes this document as UTF-8 JSON without a byte order mark.</summary>
        /// <param name="path">The file to create or replace. The parent directory must already exist.</param>
        /// <remarks>Serialization and validation finish before opening the destination. No global theme is changed.</remarks>
        public void Save(string path) => File.WriteAllText(path, ToJson(), new UTF8Encoding(false));

        private static Dictionary<string, object?> Data(Theme theme) {
            Dictionary<string, object?> tokens = new Dictionary<string, object?>();
            foreach (ThemeToken token in Theme.AllTokens()) {
                ThemeStyle style = theme.Get(token);
                tokens.Add(token.ToString(), new Dictionary<string, object?> {
                    ["Foreground"] = ColorData(style.Foreground),
                    ["Background"] = ColorData(style.Background),
                    ["Decoration"] = style.Decoration.ToString()
                });
            }
            return new Dictionary<string, object?> { ["SchemaVersion"] = 1, ["Name"] = theme.Name, ["Tokens"] = tokens };
        }

        private static object? ColorData(ThemeColor? color) => color.HasValue ? new Dictionary<string, object?> {
            ["TrueColor"] = color.Value.TrueColor.ToString(), ["Ansi16"] = color.Value.Ansi16, ["Ansi256"] = color.Value.Ansi256
        } : null;

        private static ThemeStyle Capture(ITheme theme, ThemeToken token) {
            Style rgb = theme.Resolve(token, ColorDepth.TrueColor);
            Style ansi16 = theme.Resolve(token, ColorDepth.Ansi16);
            Style ansi256 = theme.Resolve(token, ColorDepth.Ansi256);
            ThemeStyle captured = new ThemeStyle(CaptureColor(rgb.Foreground, ansi16.Foreground, ansi256.Foreground),
                CaptureColor(rgb.Background, ansi16.Background, ansi256.Background), rgb.Decoration);
            if (!captured.Resolve(ColorDepth.TrueColor).Equals(rgb) || !captured.Resolve(ColorDepth.Ansi16).Equals(ansi16)
                || !captured.Resolve(ColorDepth.Ansi256).Equals(ansi256) || !theme.Resolve(token, ColorDepth.None).Equals(Style.Plain)) {
                throw new ArgumentException("Theme token '" + token + "' cannot be exported losslessly as a ThemeStyle.", nameof(theme));
            }
            return captured;
        }

        private static ThemeColor? CaptureColor(Color rgb, Color ansi16, Color ansi256) {
            if (rgb.IsDefault && ansi16.IsDefault && ansi256.IsDefault) { return null; }
            return new ThemeColor(rgb, ansi16.IsDefault ? -1 : ansi16.Index, ansi256.IsDefault ? -1 : ansi256.Index);
        }
    }
}
