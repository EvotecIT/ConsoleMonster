using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// The widget gallery. Each example is a complete C# program (<c>Website/examples/widgets/&lt;id&gt;.cs</c>) and its
    /// PowerShell twin (<c>&lt;id&gt;.ps1</c>). The tool compiles and runs the program at every colour and glyph tier in both
    /// themes, so each frame is what that program printed; the capture runs the script on both PowerShell hosts and the
    /// render compares their cells with the program's.
    /// </summary>
    internal static class WidgetsPage {
        public static readonly (string Id, string Title, string Summary)[] Groups = {
            ("text", "Text", "Markup, rules and pixel lettering. Text is measured in terminal cells, so emoji and CJK keep the grid."),
            ("layout", "Layout", "Containers that place other widgets. Any widget can sit inside any container, including a live dashboard."),
            ("status", "Status and progress", "The widgets a live session updates: facts, steps, trees, bars, phases, spinners and the log region."),
            ("charts", "Charts", "Bars, stacked bars, sparklines, histograms and braille line and scatter charts, all scaled to their cells.")
        };

        /// <summary>One gallery entry: the programs are examples/widgets/{Id}.cs and {Id}.ps1.</summary>
        public sealed class Example {
            public Example(string id, string title, string group, string summary, int width = 60) {
                Id = id;
                Title = title;
                Group = group;
                Summary = summary;
                Width = width;
            }

            public string Id { get; }
            public string Title { get; }
            public string Group { get; }
            public string Summary { get; }
            public int Width { get; }
        }

        public static readonly Example[] Examples = {
            new Example("markup", "Text and markup", "text", "Spectre-style markup: theme roles, colours and decorations, reduced for each tier."),
            new Example("rule", "Rule", "text", "A horizontal line with an optional title, the separator inside panels and reports."),
            new Example("panel", "Panel", "layout", "A border around any widget, with a title, a caption on the right and a footer."),
            new Example("columns", "Rows and columns", "layout", "Stack widgets vertically or side by side; columns share the width by weight."),
            new Example("grid", "Grid", "layout", "Equal cells in a fixed number of columns, filled left to right."),
            new Example("align", "Padding and align", "layout", "Space around a widget, and placement inside the space it is given."),
            new Example("stack", "Stack", "layout", "Layers drawn on top of each other: a badge over a chart."),
            new Example("keyvalue", "Key/value list", "status", "Aligned facts with optional status marks, as a summary beside a run."),
            new Example("statuslist", "Status list", "status", "Steps with a status symbol and a detail on the right."),
            new Example("tree", "Tree", "status", "Nested items with guides, status symbols and notes."),
            new Example("progress", "Progress", "status", "Labelled bars with a percentage and a count, in line or block shape."),
            new Example("phases", "Phases", "status", "A pipeline of stages: done, running and waiting."),
            new Example("spinner", "Spinner", "status", "An activity with a detail, or its final state once it has finished."),
            new Example("log", "Log region", "status", "Timed log lines with levels; in a live session it collects Console and PowerShell output."),
            new Example("bar", "Bar chart", "charts", "Labelled bars scaled to the largest value, with the values on the right."),
            new Example("stacked", "Stacked bar", "charts", "Parts of a whole in one bar, with a legend of counts and shares."),
            new Example("sparkline", "Sparkline", "charts", "A compact trend in one or more rows, with the latest value."),
            new Example("histogram", "Histogram", "charts", "Values counted into buckets with labelled edges."),
            new Example("line", "Line chart", "charts", "Braille line series with an axis and a legend."),
            new Example("scatter", "Scatter chart", "charts", "The same braille canvas as points instead of lines."),
            new Example("logo", "Logo", "text", "Large pixel lettering with a gradient and a tagline, for the top of a tool.")
        };

        private static string Script(Site site, string id) => Twins.Script(site, "widgets", id);

        /// <summary>The C# program of a gallery entry.</summary>
        public static string ProgramPath(Site site, string id) => Path.Combine(site.Website, "examples", "widgets", id + ".cs");

        /// <summary>Runs every PowerShell example on both hosts and saves what they drew.</summary>
        public static void Capture(Site site) {
            foreach (Example example in Examples) {
                Twins.Capture(site, "widgets", example.Id, example.Width);
            }
        }

        /// <summary>What a gallery program prints at a tier, in a theme.</summary>
        public static CellBuffer Run(Site site, Example example, ColorDepth depth, GlyphTier glyphs, ITheme theme) =>
            CSharpRunner.Run(ProgramPath(site, example.Id), example.Width, depth, glyphs, theme);

        public static void Render(Site site, JsonObject hosts) {
            JsonArray examples = new JsonArray();
            Dictionary<string, JsonObject> bundles = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            List<string> problems = new List<string>();
            foreach (Example example in Examples) {
                (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(ProgramPath(site, example.Id)));
                CellBuffer dark = Run(site, example, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                JsonObject entry = new JsonObject {
                    ["id"] = example.Id,
                    ["title"] = example.Title,
                    ["group"] = example.Group,
                    ["summary"] = example.Summary,
                    ["width"] = example.Width,
                    ["csharp"] = shortForm,
                    ["csharpFull"] = full,
                    ["csharpFile"] = "Program.cs",
                    ["powershell"] = File.Exists(Script(site, example.Id)) ? SourceSnippets.Program(Site.ReadText(Script(site, example.Id))).Short : null,
                    ["powershellFull"] = File.Exists(Script(site, example.Id)) ? Longer(SourceSnippets.Program(Site.ReadText(Script(site, example.Id)))) : null,
                    ["powershellFile"] = example.Id + ".ps1",
                    ["frame"] = site.Frame(dark, Run(site, example, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light))
                };
                foreach (ColorTier color in ColorTier.All) {
                    foreach (GlyphLevel glyphs in GlyphLevel.All) {
                        string key = color.Id + "-" + glyphs.Id;
                        if (!bundles.TryGetValue(key, out JsonObject? bundle)) {
                            bundle = new JsonObject();
                            bundles[key] = bundle;
                        }
                        bundle[example.Id] = site.Frame(Run(site, example, color.Depth, glyphs.Tier, Theme.Dark), Run(site, example, color.Depth, glyphs.Tier, Theme.Light), color.Depth);
                    }
                }
                entry["parity"] = Twins.Check(site, "widgets", example.Id, example.Width, dark, hosts, problems);
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
