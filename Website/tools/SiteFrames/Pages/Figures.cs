using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using System.IO;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Single frames used across the site: the gallery's tier ribbon, the not-found page and figures in docs and guides.</summary>
    internal static class Figures {
        private static readonly JsonObject All = new JsonObject();

        public static void Add(string id, JsonObject figure) => All[id] = figure;

        public static JsonObject Figure(Site site, string title, string label, IWidget widget, int width, string[]? meta = null, ColorDepth depth = ColorDepth.TrueColor, GlyphTier glyphs = GlyphTier.Full, Func<IWidget>? rebuild = null) {
            JsonObject frame = site.Frame(rebuild ?? (() => widget), width, depth, glyphs);
            JsonArray chips = new JsonArray();
            foreach (string m in meta ?? new[] { width + "×" + frame["h"], TierLabel(depth), GlyphLabel(glyphs) }) {
                chips.Add(m);
            }
            return new JsonObject { ["title"] = title, ["label"] = label, ["meta"] = chips, ["frame"] = frame };
        }

        public static string TierLabel(ColorDepth depth) => depth == ColorDepth.TrueColor ? "24-bit" : depth == ColorDepth.Ansi256 ? "256" : depth == ColorDepth.Ansi16 ? "16" : "No colour";

        public static string GlyphLabel(GlyphTier tier) => tier == GlyphTier.Full ? "Full" : tier == GlyphTier.Basic ? "Basic" : "ASCII";

        /// <summary>The assessment summary program (examples/home/summary.cs), the composition shown at four terminal profiles and in each theme.</summary>
        public static string SummaryProgram(Site site) => Path.Combine(site.Website, "examples", "home", "summary.cs");

        /// <summary>What the summary program prints at a width, tier and theme.</summary>
        public static CellBuffer Summary(Site site, int width, ColorDepth depth, GlyphTier glyphs, ITheme theme) => CSharpRunner.Run(SummaryProgram(site), width, depth, glyphs, theme);

        /// <summary>Adds a program's code to a figure: the short form, the whole program and its file name.</summary>
        public static JsonObject WithProgram(JsonObject figure, string path) {
            (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(path));
            figure["csharp"] = shortForm;
            figure["csharpFull"] = full;
            return figure;
        }

        public static void Render(Site site) {
            JsonArray tiers = new JsonArray();
            (ColorDepth depth, GlyphTier glyphs)[] profiles = {
                (ColorDepth.TrueColor, GlyphTier.Full),
                (ColorDepth.Ansi256, GlyphTier.Full),
                (ColorDepth.Ansi16, GlyphTier.Basic),
                (ColorDepth.None, GlyphTier.Ascii)
            };
            foreach ((ColorDepth depth, GlyphTier glyphs) in profiles) {
                CellBuffer dark = Summary(site, 40, depth, glyphs, Theme.Dark);
                JsonObject frame = site.Frame(dark, null, depth);
                string label = TierLabel(depth) + (depth == ColorDepth.None ? "" : " colour") + ", " + GlyphLabel(glyphs) + " glyphs";
                tiers.Add(new JsonObject {
                    ["title"] = depth == ColorDepth.None ? "NO_COLOR" : TierLabel(depth) + " colour",
                    ["label"] = "The same summary at " + label,
                    ["meta"] = new JsonArray(TierLabel(depth), GlyphLabel(glyphs)),
                    ["frame"] = frame
                });
            }
            Add("tier-ribbon", WithProgram(new JsonObject { ["tiers"] = tiers }, SummaryProgram(site)));

            string notFound = Path.Combine(site.Website, "examples", "home", "not-found.cs");
            JsonObject figure = new JsonObject {
                ["title"] = "pwsh",
                ["label"] = "A tree of the site's main pages; this address is marked as failed.",
                ["meta"] = new JsonArray("56 columns", "24-bit", "Full"),
                ["frame"] = site.Frame(CSharpRunner.Run(notFound, 56, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark), CSharpRunner.Run(notFound, 56, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light))
            };
            Add("not-found", WithProgram(figure, notFound));
        }

        public static void Write(Site site) => site.WriteJson(site.Data("figures"), All);
    }
}
