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
    /// Tables and complex data: complete C# programs (<c>Website/examples/tables/&lt;id&gt;.cs</c>) run at a range of widths,
    /// so the page can show columns hiding by priority, text wrapping and rows turning into cards as the terminal narrows.
    /// Every frame is what the program shown beside it printed at that width.
    /// </summary>
    internal static class TablesPage {
        public static readonly int[] Widths = { 120, 100, 80, 64, 48, 36 };

        public const int DefaultWidth = 100;

        private sealed class Section {
            public Section(string id, string title, string summary, string[] points) {
                Id = id;
                Title = title;
                Summary = summary;
                Points = points;
            }

            public string Id { get; }
            public string Title { get; }
            public string Summary { get; }
            public string[] Points { get; }
        }

        /// <summary>The C# program of a section.</summary>
        public static string ProgramPath(Site site, string id) => Path.Combine(site.Website, "examples", "tables", id + ".cs");

        /// <summary>What a section's program prints at a width, in a theme (24-bit colour, full glyphs unless given).</summary>
        public static CellBuffer Run(Site site, string id, int width, ITheme theme, ColorDepth depth = ColorDepth.TrueColor, GlyphTier glyphs = GlyphTier.Full) =>
            CSharpRunner.Run(ProgramPath(site, id), width, depth, glyphs, theme, 200);

        public static void Render(Site site) {

            Section[] sections = {
                new Section("dns", "Priority, overflow and a footer for what is hidden", "DNS records. The key column never hides; the SPF value wraps at its separators; the cache path is shortened in the middle; low-priority columns hide first and the footer names them.",
                    new[] { "Priority, MinWidth and MaxWidth per column", "Wrap with MaxLines, Truncate, TruncateMiddle or Never", "A footer such as +2 columns (TTL, Cache file)" }),
                new Section("checks", "Status, durations and tone rules", "Mail-security checks. Status presenters show a symbol and a word, so the result reads without colour; durations use the presenter's units.",
                    new[] { "Cell presenters shared by tables, cards, key/value lists and trees", "A tone rule colours a value by threshold", "Below 60 cells the rows become cards" }),
                new Section("summary", "Groups, subtotals and totals", "An assessment run summary grouped by target type, with subtotals, a totals row and a score drawn as a data bar inside the cell.",
                    new[] { "AddGroup with ShowSubtotals", "Sum, Count, Average, Min and Max per column", "A data bar presenter in a cell" }),
                new Section("artifacts", "Bytes, relative dates and top N", "Build artifacts: sizes in bytes, build times, relative dates against a fixed clock, and the first five of 1,234 rows.",
                    new[] { "Bytes, Duration and Date presenters", "MaxRows with TotalRowCount prints Showing 5 of 1,234", "Numbers follow the culture; the site renders en-US" }),
                new Section("certificate", "One record as a record view", "A certificate as sections, fields and nested tables. Empty fields are hidden and dates show both absolute and relative forms.",
                    new[] { "Sections, groups and nested widgets", "Boolean presenters can mark either value as the problem", "A single record from Format-ConsoleTable renders this way" }),
                new Section("inspector", "The object inspector", "Nested dictionaries and lists, shown as indented sections and nested tables. The inspector also reads .NET objects, PSObjects and JSON, with depth, item and node limits.",
                    new[] { "MaxDepth, MaxItems and MaxNodes bound one inspection", "Cycles are detected and marked", "Format-ConsoleTree -AsInspector in PowerShell" })
            };

            JsonArray list = new JsonArray();
            Dictionary<int, JsonObject> bundles = new Dictionary<int, JsonObject>();
            foreach (Section section in sections) {
                JsonArray points = new JsonArray();
                foreach (string p in section.Points) {
                    points.Add(p);
                }
                (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(ProgramPath(site, section.Id)));
                list.Add(new JsonObject {
                    ["id"] = section.Id,
                    ["title"] = section.Title,
                    ["summary"] = section.Summary,
                    ["points"] = points,
                    ["csharp"] = shortForm,
                    ["csharpFull"] = full,
                    ["csharpFile"] = "Program.cs",
                    ["frame"] = site.Frame(Run(site, section.Id, DefaultWidth, Theme.Dark), Run(site, section.Id, DefaultWidth, Theme.Light))
                });
                foreach (int width in Widths) {
                    if (!bundles.TryGetValue(width, out JsonObject? bundle)) {
                        bundle = new JsonObject();
                        bundles[width] = bundle;
                    }
                    bundle[section.Id] = site.Frame(Run(site, section.Id, width, Theme.Dark), Run(site, section.Id, width, Theme.Light));
                }
            }
            JsonArray widths = new JsonArray();
            foreach (int w in Widths) {
                widths.Add(w);
            }
            site.WriteJson(site.Data("tables"), new JsonObject { ["widths"] = widths, ["default"] = DefaultWidth, ["sections"] = list });
            string folder = site.Static("frames/tables");
            Site.Clean(folder);
            foreach (KeyValuePair<int, JsonObject> bundle in bundles) {
                site.WriteJson(Path.Combine(folder, bundle.Key + ".json"), bundle.Value);
            }
        }
    }
}
