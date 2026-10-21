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
    /// or in a code block.
    /// </summary>
    internal static class FrameSources {
        private static readonly HashSet<string> SourceKeys = new HashSet<string>(StringComparer.Ordinal) {
            "script", "csharp", "powershell", "source", "code", "program"
        };

        private static readonly Regex Shortcode = new Regex(@"\{\{<\s*cm-frame\s+(?<attrs>[^>]*?)>\}\}", RegexOptions.CultureInvariant);

        private static readonly Regex Attribute = new Regex(@"(?<name>\w+)=""(?<value>[^""]*)""", RegexOptions.CultureInvariant);

        public static void Check(Site site) {
            List<string> problems = new List<string>();
            string[] files = Directory.GetFiles(Path.Combine(site.Website, "data"), "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files) {
                problems.AddRange(Find(JsonNode.Parse(File.ReadAllText(file)), Path.GetFileNameWithoutExtension(file)));
            }
            JsonObject figures = JsonNode.Parse(File.ReadAllText(site.Data("figures")))!.AsObject();
            string[] pages = Directory.GetFiles(Path.Combine(site.Website, "content"), "*.md", SearchOption.AllDirectories);
            Array.Sort(pages, StringComparer.Ordinal);
            foreach (string page in pages) {
                problems.AddRange(FindOnPage(Site.ReadText(page), figures, Path.GetRelativePath(site.Website, page).Replace('\\', '/')));
            }
            WidgetsPage.Report(problems, "sources");
        }

        /// <summary>Frames in a data file whose entry carries no code, as "file.path" problems.</summary>
        internal static IEnumerable<string> Find(JsonNode? root, string name) {
            List<string> problems = new List<string>();
            Walk(root, null, name, false, problems);
            return problems;
        }

        private static void Walk(JsonNode? node, JsonObject? parent, string where, bool sourced, List<string> problems) {
            switch (node) {
                case JsonObject obj:
                    bool here = sourced || HasSource(obj, parent);
                    foreach (KeyValuePair<string, JsonNode?> property in obj) {
                        if ((property.Key == "frame" || property.Key == "poster") && property.Value is JsonObject && !here) {
                            problems.Add(where + "." + property.Key + " has no code linked to it: add the script or program that drew it (script, csharp, powershell, source), a *Repo link, or sourceOf.");
                        }
                        Walk(property.Value, obj, where + "." + property.Key, here, problems);
                    }
                    break;
                case JsonArray array:
                    for (int i = 0; i < array.Count; i++) {
                        Walk(array[i], parent, where + "[" + i + "]", sourced, problems);
                    }
                    break;
            }
        }

        private static bool HasSource(JsonObject obj, JsonObject? parent) {
            foreach (KeyValuePair<string, JsonNode?> property in obj) {
                if (SourceKeys.Contains(property.Key) && property.Value != null) {
                    return true;
                }
                if (property.Key.EndsWith("Repo", StringComparison.Ordinal) && property.Value is JsonValue) {
                    return true;
                }
            }
            // A variant of another entry (the same program at another size) names it, and that entry must carry the code.
            if (obj["sourceOf"] is JsonValue of && parent?[of.GetValue<string>()] is JsonObject sibling) {
                return HasSource(sibling, null);
            }
            return false;
        }

        /// <summary>Figures a page shows with script="false" whose code appears nowhere else on that page.</summary>
        internal static IEnumerable<string> FindOnPage(string markdown, JsonObject figures, string page) {
            List<(string Id, bool Hidden)> shown = new List<(string, bool)>();
            foreach (Match match in Shortcode.Matches(markdown)) {
                Dictionary<string, string> attrs = Attribute.Matches(match.Groups["attrs"].Value).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);
                string ids = attrs.TryGetValue("ids", out string? list) ? list : attrs.TryGetValue("id", out string? one) ? one : string.Empty;
                bool hidden = attrs.TryGetValue("script", out string? script) && script == "false";
                foreach (string id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    shown.Add((id, hidden));
                }
            }
            string fences = string.Join("\n", CodeGuard.Fences(markdown).Select(f => f.Code.Replace("\n   ", "\n").Trim()));
            foreach ((string id, bool hidden) in shown) {
                if (!hidden) {
                    continue;
                }
                JsonObject? figure = figures[id] as JsonObject;
                string? code = figure?["script"]?.GetValue<string>() ?? figure?["csharp"]?.GetValue<string>();
                if (code == null) {
                    yield return page + ": figure " + id + " has script=\"false\" and no code of its own.";
                    continue;
                }
                bool elsewhere = shown.Any(s => !s.Hidden && figures[s.Id] is JsonObject other
                    && (other["script"]?.GetValue<string>() == code || other["csharp"]?.GetValue<string>() == code));
                if (!elsewhere && !fences.Contains(code.Trim(), StringComparison.Ordinal)) {
                    yield return page + ": figure " + id + " hides its code (script=\"false\"), and the page shows that code nowhere else.";
                }
            }
        }
    }
}
