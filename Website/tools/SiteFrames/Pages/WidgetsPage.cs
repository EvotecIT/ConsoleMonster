using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Catalog;

namespace SiteFrames.Pages {
    /// <summary>The widget gallery: every example at every tier, its C# and PowerShell source and the PowerShell parity check.</summary>
    internal static class WidgetsPage {
        public static readonly (string Id, string Title, string Summary)[] Groups = {
            ("text", "Text", "Markup, rules and pixel lettering. Text is measured in terminal cells, so emoji and CJK keep the grid."),
            ("layout", "Layout", "Containers that place other widgets. Any widget can sit inside any container, including a live dashboard."),
            ("status", "Status and progress", "The widgets a live session updates: facts, steps, trees, bars, phases, spinners and the log region."),
            ("charts", "Charts", "Bars, stacked bars, sparklines, histograms and braille line and scatter charts, all scaled to their cells.")
        };

        private static string Script(Site site, string id) => Twins.Script(site, "widgets", id);

        /// <summary>Runs every PowerShell example on both hosts and saves what they drew.</summary>
        public static void Capture(Site site) {
            foreach (Example example in WidgetExamples.All()) {
                Twins.Capture(site, "widgets", example.Id, example.Width);
            }
        }

        public static void Render(Site site, JsonObject hosts) {
            string source = Path.Combine(site.Tool, "Catalog", "WidgetExamples.cs");
            JsonArray examples = new JsonArray();
            Dictionary<string, JsonObject> bundles = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            List<string> problems = new List<string>();
            foreach (Example example in WidgetExamples.All()) {
                Func<ConsoleMonster.Layout.IWidget> build = () => Catalog.Terminal.Capture(example.Code);
                JsonObject entry = new JsonObject {
                    ["id"] = example.Id,
                    ["title"] = example.Title,
                    ["group"] = example.Group,
                    ["summary"] = example.Summary,
                    ["width"] = example.Width,
                    ["csharp"] = SourceSnippets.Split(SourceSnippets.Get(source, example.Method)).Short,
                    ["csharpFull"] = Longer(SourceSnippets.Split(SourceSnippets.Get(source, example.Method))),
                    ["powershell"] = File.Exists(Script(site, example.Id)) ? SourceSnippets.Split(Site.ReadText(Script(site, example.Id))).Short : null,
                    ["powershellFull"] = File.Exists(Script(site, example.Id)) ? Longer(SourceSnippets.Split(Site.ReadText(Script(site, example.Id)))) : null,
                    ["frame"] = site.Frame(build, example.Width)
                };
                foreach (ColorTier color in ColorTier.All) {
                    foreach (GlyphLevel glyphs in GlyphLevel.All) {
                        string key = color.Id + "-" + glyphs.Id;
                        if (!bundles.TryGetValue(key, out JsonObject? bundle)) {
                            bundle = new JsonObject();
                            bundles[key] = bundle;
                        }
                        bundle[example.Id] = site.Frame(build, example.Width, color.Depth, glyphs.Tier);
                    }
                }
                JsonObject parity = Twins.Check(site, "widgets", example.Id, example.Width, build, hosts, problems);
                entry["parity"] = parity;
                examples.Add(entry);
            }

            JsonArray groups = new JsonArray();
            foreach ((string id, string title, string summary) in Groups) {
                int count = 0;
                foreach (JsonNode? e in examples) {
                    if ((string?)e!["group"] == id) {
                        count++;
                    }
                }
                groups.Add(new JsonObject { ["id"] = id, ["title"] = title, ["summary"] = summary, ["count"] = count });
            }
            JsonArray colors = new JsonArray();
            foreach (ColorTier c in ColorTier.All) {
                colors.Add(new JsonObject { ["id"] = c.Id, ["label"] = c.Label });
            }
            JsonArray glyphTiers = new JsonArray();
            foreach (GlyphLevel g in GlyphLevel.All) {
                glyphTiers.Add(new JsonObject { ["id"] = g.Id, ["label"] = g.Label });
            }
            site.WriteJson(site.Data("widgets"), new JsonObject {
                ["groups"] = groups,
                ["colors"] = colors,
                ["glyphs"] = glyphTiers,
                ["examples"] = examples
            });
            string folder = site.Static("frames/widgets");
            Site.Clean(folder);
            foreach (KeyValuePair<string, JsonObject> bundle in bundles) {
                site.WriteJson(Path.Combine(folder, bundle.Key + ".json"), bundle.Value);
            }
            Report(problems, "widgets");
        }

        /// <summary>The full program when it is longer than the short form shown first, otherwise null.</summary>
        public static string? Longer((string Short, string Full) code) => code.Short == code.Full ? null : code.Full;

        public static void Report(List<string> problems, string area) {
            foreach (string problem in problems) {
                Console.Error.WriteLine("[" + area + "] " + problem);
            }
            if (problems.Count > 0) {
                Program.Failures += problems.Count;
            }
        }
    }
}
