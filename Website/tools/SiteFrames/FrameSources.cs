using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SiteFrames.Pages;

namespace SiteFrames {
    /// <summary>
    /// Fails the render when a frame on the site has no code linked to it. In <c>data/*.json</c>, every <c>frame</c> or
    /// <c>poster</c> must sit in an entry that carries the code that drew it (<c>script</c>, <c>csharp</c>,
    /// <c>powershell</c>, <c>source</c>, <c>code</c> or <c>program</c>), a <c>*Repo</c> link to the repository program a
    /// page tells the reader to run, or <c>sourceOf</c> naming a sibling entry that does. On docs pages, a
    /// <c>cm-frame</c> with <c>script="false"</c> must have its code shown elsewhere on the same page: in another figure
    /// or in a code block. A <c>cm-try</c> block shows the code of each example's first figure.
    /// </summary>
    internal static class FrameSources {
        private static readonly HashSet<string> SourceKeys = new HashSet<string>(StringComparer.Ordinal) {
            "script", "csharp", "powershell", "source", "code", "program"
        };

        // Attribute values may contain > (a caption with 6>&1), so quoted values are read whole.
        private static readonly Regex Shortcode = new Regex(@"\{\{<\s*cm-(?<kind>frame|try)\s+(?<attrs>(?:""[^""]*""|[^"">])*?)\s*>\}\}", RegexOptions.CultureInvariant);

        private static readonly Regex LiveShortcode = new Regex(@"\{\{<\s*cm-live\s+[^>]*?\bid=""(?<id>[^""]*)""", RegexOptions.CultureInvariant);

        private static readonly Regex Attribute = new Regex(@"(?<name>\w+)=""(?<value>[^""]*)""", RegexOptions.CultureInvariant);

        public static void Check(Site site) {
            List<string> problems = new List<string>();
            string[] files = Directory.GetFiles(Path.Combine(site.Website, "data"), "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files) {
                problems.AddRange(Find(JsonNode.Parse(File.ReadAllText(file)), Path.GetFileNameWithoutExtension(file), site.Repository));
            }
            JsonObject figures = JsonNode.Parse(File.ReadAllText(site.Data("figures")))!.AsObject();
            string[] pages = Directory.GetFiles(Path.Combine(site.Website, "content"), "*.md", SearchOption.AllDirectories);
            Array.Sort(pages, StringComparer.Ordinal);
            JsonObject live = JsonNode.Parse(File.ReadAllText(site.Data("live")))!.AsObject();
            foreach (string page in pages) {
                string markdown = Site.ReadText(page);
                string name = Path.GetRelativePath(site.Website, page).Replace('\\', '/');
                problems.AddRange(FindOnPage(markdown, figures, name));
                // A live recording embedded by id must exist; the shortcode alone would render nothing.
                foreach (Match embed in LiveShortcode.Matches(markdown)) {
                    if (live[embed.Groups["id"].Value] == null) {
                        problems.Add(name + ": cm-live id=\"" + embed.Groups["id"].Value + "\" is not a recording in data/live.json.");
                    }
                }
            }
            WidgetsPage.Report(problems, "sources");
        }

        /// <summary>Frames in a data file whose entry carries no code, as "file.path" problems.</summary>
        /// <param name="root">The data file.</param>
        /// <param name="name">The file's name, for messages.</param>
        /// <param name="repository">The repository a <c>*Repo</c> path must exist in; null skips that check.</param>
        internal static IEnumerable<string> Find(JsonNode? root, string name, string? repository = null) {
            List<string> problems = new List<string>();
            // The file's own top level is a collection of entries (or page text), never the code for all of them.
            if (root is JsonObject top) {
                foreach (KeyValuePair<string, JsonNode?> property in top) {
                    if ((property.Key == "frame" || property.Key == "poster") && IsFrame(property.Value)) {
                        problems.Add(name + "." + property.Key + " is a frame at the top of its file, with no entry to carry its code.");
                    }
                    Walk(property.Value, top, name + "." + property.Key, false, repository, problems);
                }
            } else {
                Walk(root, null, name, false, repository, problems);
            }
            return problems;
        }

        private static void Walk(JsonNode? node, JsonObject? parent, string where, bool sourced, string? repository, List<string> problems) {
            switch (node) {
                case JsonObject obj:
                    bool here = sourced || HasSource(obj, parent, repository);
                    foreach (KeyValuePair<string, JsonNode?> property in obj) {
                        if ((property.Key == "frame" || property.Key == "poster") && IsFrame(property.Value) && !here) {
                            problems.Add(where + "." + property.Key + " has no code linked to it: add the script or program that drew it (script, csharp, powershell, source), a *Repo link, or sourceOf.");
                        }
                        Walk(property.Value, obj, where + "." + property.Key, here, repository, problems);
                    }
                    break;
                case JsonArray array:
                    for (int i = 0; i < array.Count; i++) {
                        Walk(array[i], parent, where + "[" + i + "]", sourced, repository, problems);
                    }
                    break;
            }
        }

        // A rendered frame: an object with the frame's HTML rows (page text that happens to be called "frame" is not one).
        private static bool IsFrame(JsonNode? node) =>
            node is JsonObject frame && (frame.ContainsKey("dark") || frame.ContainsKey("light") || frame.ContainsKey("darkOnly") || frame.ContainsKey("lightOnly"));

        // Code: a non-empty string, or an object that itself carries code (a program's sources by language).
        private static bool IsCode(JsonNode? node) {
            if (node is JsonValue value) {
                return value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text);
            }
            if (node is JsonObject obj) {
                foreach (KeyValuePair<string, JsonNode?> property in obj) {
                    if ((SourceKeys.Contains(property.Key) || property.Key == "text") && IsCode(property.Value)) {
                        return true;
                    }
                }
            }
            // A list of code blocks (steps with their code).
            if (node is JsonArray list) {
                return list.Any(IsCode);
            }
            return false;
        }

        private static bool HasSource(JsonObject obj, JsonObject? parent, string? repository) {
            foreach (KeyValuePair<string, JsonNode?> property in obj) {
                if (SourceKeys.Contains(property.Key) && IsCode(property.Value)) {
                    return true;
                }
                // A link to a program in the repository counts only when that file exists.
                if (property.Key.EndsWith("Repo", StringComparison.Ordinal) && property.Value is JsonValue link && link.TryGetValue(out string? path) && !string.IsNullOrWhiteSpace(path)
                    && (repository == null || File.Exists(Path.Combine(repository, path!)))) {
                    return true;
                }
            }
            // A variant of another entry (the same program at another size) names it, and that entry must carry the code.
            if (obj["sourceOf"] is JsonValue of && of.TryGetValue(out string? name) && parent?[name!] is JsonObject sibling) {
                return HasSource(sibling, null, repository);
            }
            return false;
        }

        /// <summary>Figures a page shows with script="false" whose code appears nowhere else on that page.</summary>
        internal static IEnumerable<string> FindOnPage(string markdown, JsonObject figures, string page) {
            List<(string Id, bool Hidden)> shown = new List<(string, bool)>();
            foreach (Match match in Shortcode.Matches(markdown)) {
                Dictionary<string, string> attrs = Attribute.Matches(match.Groups["attrs"].Value).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);
                string ids = attrs.TryGetValue("ids", out string? list) ? list : attrs.TryGetValue("id", out string? one) ? one : string.Empty;
                bool hidden = match.Groups["kind"].Value == "frame" && attrs.TryGetValue("script", out string? script) && script == "false";
                if (match.Groups["kind"].Value == "try") {
                    // A Try-it block shows the code of each example's first figure; the other frames of that example are
                    // shown without code of their own, like script="false".
                    string[] examples = attrs.TryGetValue("scenarios", out string? named) ? named.Split('|') : new[] { ids };
                    foreach (string example in examples) {
                        string figuresOf = example.Contains(':') ? example.Substring(example.IndexOf(':') + 1) : example;
                        string[] each = figuresOf.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        for (int i = 0; i < each.Length; i++) {
                            shown.Add((each[i], i > 0));
                        }
                    }
                    continue;
                }
                foreach (string id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    shown.Add((id, hidden));
                }
            }
            foreach ((string id, bool _) in shown) {
                if (figures[id] is not JsonObject) {
                    yield return page + ": figure " + id + " is not in data/figures.json.";
                }
            }
            string fences = string.Join("\n", CodeGuard.Fences(markdown).Select(f => f.Code.Trim()));
            foreach ((string id, bool hidden) in shown) {
                if (!hidden) {
                    continue;
                }
                JsonObject? figure = figures[id] as JsonObject;
                // Every language the figure carries must be on the page somewhere else.
                List<string> codes = new List<string>();
                foreach (string key in new[] { "script", "csharp" }) {
                    if (figure?[key] is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text)) {
                        codes.Add(text!);
                    }
                }
                if (codes.Count == 0) {
                    yield return page + ": figure " + id + " has script=\"false\" and no code of its own.";
                    continue;
                }
                foreach (string code in codes) {
                    bool elsewhere = shown.Any(s => !s.Hidden && figures[s.Id] is JsonObject other
                        && (other["script"]?.GetValue<string>() == code || other["csharp"]?.GetValue<string>() == code));
                    if (!elsewhere && !fences.Contains(code.Trim(), StringComparison.Ordinal)) {
                        yield return page + ": figure " + id + " hides its code (script=\"false\"), and the page shows that code nowhere else.";
                        break;
                    }
                }
            }
        }
    }
}
