using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using SiteFrames.Frames;

namespace SiteFrames.Pages {
    /// <summary>
    /// Card thumbnails for the docs catalogue: a miniature of a frame the page shows. Pages that render figures or demos
    /// offer their frames by id; a docs page names one in its <c>card</c> front matter, and the miniature is written to
    /// <c>static/site/cards/</c> and listed in <c>data/visuals.json</c> under <c>cards</c>, keyed by that id. Every docs page
    /// must name a known <c>group</c> (from <c>docsCatalog</c> in <c>data/copy.json</c>) and a card, or the render fails, so no
    /// page drops out of the catalogue unnoticed.
    /// </summary>
    internal static class DocCards {
        private const int Columns = 72;
        private const int Rows = 22;
        private static readonly Dictionary<string, (CellBuffer Frame, Screen? Only)> Offered = new Dictionary<string, (CellBuffer, Screen?)>(StringComparer.Ordinal);

        /// <summary>Offers a frame under a figure or demo id. <paramref name="only"/> names the one screen the frame was drawn
        /// for (recorded and captured frames carry dark-theme colours; a light-theme render belongs on the light screen), and
        /// that miniature is shown in both site themes; null draws it on each screen.</summary>
        public static void Offer(string id, CellBuffer frame, Screen? only) => Offered[id] = (frame, only);

        public static JsonObject Render(Site site) {
            string folder = site.Static("site/cards");
            Site.Clean(folder);
            JsonObject cards = new JsonObject();
            List<string> problems = new List<string>();
            HashSet<string> groups = Groups(site);
            HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
            // Docs pages need a group and a card; guides and section indexes may name a card, which is then their social
            // card artwork (site.json Social.GeneratedCardImages: /site/cards/{meta.card}.svg) and must exist too.
            List<(string File, bool Catalogue)> files = new List<(string, bool)>();
            foreach (string section in new[] { "docs", "guides" }) {
                string[] found = Directory.GetFiles(Path.Combine(site.Website, "content", section), "*.md");
                Array.Sort(found, StringComparer.Ordinal);
                foreach (string file in found) {
                    files.Add((file, section == "docs" && !Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal)));
                }
            }
            foreach ((string file, bool catalogue) in files) {
                string page = Path.GetRelativePath(Path.Combine(site.Website, "content"), file).Replace('\\', '/');
                string? id = FrontMatter(file, "card");
                if (catalogue) {
                    string? group = FrontMatter(file, "group");
                    if (group == null || !groups.Contains(group)) {
                        problems.Add(page + ": group \"" + (group ?? string.Empty) + "\" is not one of docsCatalog.groups in data/copy.json, so the page is missing from the catalogue.");
                    }
                    if (id == null) {
                        problems.Add(page + ": no card in the front matter; name a figure or demo the page shows.");
                        continue;
                    }
                } else if (id == null) {
                    continue;
                }
                if (cards.ContainsKey(id) || reported.Contains(id)) {
                    continue;
                }
                if (!Offered.TryGetValue(id, out (CellBuffer Frame, Screen? Only) offer)) {
                    reported.Add(id);
                    problems.Add(page + ": card \"" + id + "\" is not a figure or demo the site tool renders.");
                    continue;
                }
                JsonObject entry = new JsonObject();
                foreach (Screen screen in offer.Only != null ? new[] { offer.Only! } : new[] { Screen.Dark, Screen.Light }) {
                    string name = id + (offer.Only != null ? string.Empty : "-" + screen.Name) + ".svg";
                    site.Write(Path.Combine(folder, name), FrameMinimap.Svg(offer.Frame, ColorDepth.TrueColor, screen, Columns, Rows));
                    entry[offer.Only != null ? "src" : screen.Name] = "/site/cards/" + name;
                }
                cards[id] = entry;
            }
            WidgetsPage.Report(problems, "docs");
            return cards;
        }

        private static HashSet<string> Groups(Site site) {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            JsonNode? copy = JsonNode.Parse(File.ReadAllText(site.Data("copy")));
            if (copy?["docsCatalog"]?["groups"] is JsonArray list) {
                foreach (JsonNode? g in list) {
                    string? id = g?["id"]?.GetValue<string>();
                    if (id != null) {
                        ids.Add(id);
                    }
                }
            }
            return ids;
        }

        // A scalar from the front matter block at the top of a Markdown file, or null.
        private static string? FrontMatter(string file, string key) {
            string[] lines = File.ReadAllLines(file);
            if (lines.Length == 0 || lines[0].Trim() != "---") {
                return null;
            }
            for (int i = 1; i < lines.Length && lines[i].Trim() != "---"; i++) {
                string line = lines[i];
                if (line.StartsWith(key + ":", StringComparison.Ordinal)) {
                    string value = line.Substring(key.Length + 1).Trim().Trim('"');
                    return value.Length == 0 ? null : value;
                }
            }
            return null;
        }
    }
}
