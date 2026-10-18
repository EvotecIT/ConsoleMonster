using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster.Examples.Showcases;

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

        /// <summary>One small assessment summary, the composition the gallery stage shows at four terminal profiles.</summary>
        public static IWidget Assessment() {
            Phases phases = new Phases()
                .Set("Discover", StatusKind.Pass, null, "2.1s")
                .Set("Collect", StatusKind.Pass, null, "14s")
                .Set("Evaluate", StatusKind.Running, null, "64%");
            StackedBar results = new StackedBar()
                .Add("Passed", 412, ThemeToken.Pass)
                .Add("Warnings", 51, ThemeToken.Warn)
                .Add("Failed", 23, ThemeToken.Fail)
                .Add("Skipped", 14, ThemeToken.Skip);
            Sparkline rate = new Sparkline { Label = "rules/s", Unit = "/s" };
            rate.AddRange(new double[] { 18, 21, 19, 24, 27, 26, 31, 29, 34, 38, 36, 41, 39, 44, 47, 45, 50, 48, 53, 57 });
            return new Panel(new Rows(phases, results, rate), "[bold]TestimoX[/]") { Caption = "ad.evotec.xyz" };
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
                CellBuffer dark = Site.Render(Assessment(), 40, depth, glyphs, Theme.Dark);
                JsonObject frame = site.Frame(dark, null, depth);
                string label = TierLabel(depth) + (depth == ColorDepth.None ? "" : " colour") + ", " + GlyphLabel(glyphs) + " glyphs";
                tiers.Add(new JsonObject {
                    ["title"] = depth == ColorDepth.None ? "NO_COLOR" : TierLabel(depth) + " colour",
                    ["label"] = "The same summary at " + label,
                    ["meta"] = new JsonArray(TierLabel(depth), GlyphLabel(glyphs)),
                    ["frame"] = frame
                });
            }
            Add("tier-ribbon", new JsonObject { ["tiers"] = tiers });

            Tree routes = new Tree("[bold]consolemonster.dev[/]");
            routes.Add("/widgets/", StatusKind.Pass, "every widget, every tier");
            routes.Add("/tables/", StatusKind.Pass, "width negotiation");
            routes.Add("/live/", StatusKind.Pass, "recorded dashboards");
            routes.Add("/powershell/", StatusKind.Pass, "cmdlets on 5.1 and 7");
            routes.Add("/docs/", StatusKind.Pass, "install and configure");
            routes.Add("this address", StatusKind.Fail, "no page here");
            Add("not-found", Figure(site, "pwsh", "A tree of the site's main pages; this address is marked as failed.", routes, 56));
        }

        public static void Write(Site site) => site.WriteJson(site.Data("figures"), All);
    }
}
