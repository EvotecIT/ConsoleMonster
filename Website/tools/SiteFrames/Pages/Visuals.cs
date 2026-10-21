using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster.Examples.Showcases;
using SiteFrames.Frames;

namespace SiteFrames.Pages {
    /// <summary>Navigation thumbnails: a miniature of real output for each destination, in the dark and light themes.</summary>
    internal static class Visuals {
        private const int Rows = 20;
        private const int Columns = 64;

        public static void Render(Site site) {
            string folder = site.Static("site/nav");
            Site.Clean(folder);
            JsonObject nav = new JsonObject();
            Dictionary<string, Func<ITheme, CellBuffer?>> items = new Dictionary<string, Func<ITheme, CellBuffer?>> {
                ["widgets"] = theme => Site.Render(WidgetGallery.Build(), 70, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["tables"] = theme => Site.Render(TableShowcases.DnsRecords(), 80, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["live"] = LiveFrame,
                ["interactive"] = theme => InteractivePage.Thumbnail(site, theme),
                ["powershell"] = theme => Site.Render(Catalog.Terminal.Capture(Catalog.PageExamples.Controllers), 80, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["docs"] = theme => Site.Render(Profile(), 80, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["guides"] = theme => Site.Render(Migration(), 80, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["api"] = theme => Site.Render(Namespaces(), 80, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["commands"] = theme => Site.Render(Commands(site), 80, ColorDepth.TrueColor, GlyphTier.Full, theme),
                ["benchmarks"] = theme => StressFrame(site)
            };
            foreach (KeyValuePair<string, Func<ITheme, CellBuffer?>> item in items) {
                bool wide = item.Key == "live" || item.Key == "benchmarks" || item.Key == "interactive";
                JsonObject entry = new JsonObject { ["w"] = 96, ["h"] = 60 };
                foreach ((Screen screen, ITheme theme) in new[] { (Screen.Dark, (ITheme)Theme.Dark), (Screen.Light, Theme.Light) }) {
                    CellBuffer? frame = item.Value(theme);
                    if (frame == null) {
                        continue;
                    }
                    // Recorded runs exist in the dark theme only; the light thumbnail shows the same recording.
                    Screen shown = item.Key == "benchmarks" ? Screen.Dark : screen;
                    int rows = wide ? Math.Min(frame.Height, 36) : Rows;
                    int columns = wide ? (int)Math.Round(rows * 3.2) : Columns;
                    string svg = FrameMinimap.Svg(frame, ColorDepth.TrueColor, shown, columns, rows);
                    string file = item.Key + "-" + screen.Name + ".svg";
                    site.Write(Path.Combine(folder, file), svg);
                    entry[screen.Name] = "/site/nav/" + file;
                }
                nav[item.Key] = entry;
            }
            site.WriteJson(site.Data("visuals"), new JsonObject { ["nav"] = nav, ["cards"] = DocCards.Render(site) });
        }

        // The last live frame of the recorded stress test.
        private static CellBuffer? StressFrame(Site site) {
            string path = Path.Combine(site.Captures, "live", "stress.cast");
            if (!File.Exists(path)) {
                return null;
            }
            List<Replay.TimedFrame> frames = Replay.Replays.FromCast(File.ReadAllText(path), 3, true, out _, out _, out _);
            return frames.Count > 0 ? frames[frames.Count - 1].Frame : null;
        }
        // The assessment dashboard after the whole recording was applied.
        private static CellBuffer LiveFrame(ITheme theme) {
            Dashboard dashboard = ParallelRun.Build(out _);
            Dictionary<string, ILiveTarget> targets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            Collect(dashboard, targets);
            long last = 0;
            foreach (LiveEvent e in ParallelRunRecording.Generate()) {
                last = Math.Max(last, e.Time);
                if (targets.TryGetValue(e.Target, out ILiveTarget? target)) {
                    target.Apply(e);
                }
            }
            RenderContext context = ConsoleMonster.Terminal.CreateContext(TerminalProfile.Fixed(120, 36, ColorDepth.TrueColor, GlyphTier.Full).WithCulture(Site.Culture), theme);
            return FrameComposer.Compose(dashboard, 120, 36, context.WithClock(0, TimeSpan.FromMilliseconds(last)));
        }

        private static void Collect(IWidget widget, Dictionary<string, ILiveTarget> targets) {
            if (widget is ILiveTarget target && !targets.ContainsKey(target.Name)) {
                targets.Add(target.Name, target);
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    Collect(child, targets);
                }
            }
        }

        private static IWidget Profile() {
            return new Panel(new KeyValueList()
                .Add("Colour", "24-bit [muted](CONSOLEMONSTER_COLOR)[/]")
                .Add("Glyphs", "Full [muted](console font)[/]")
                .Add("Mode", "Interactive")
                .Add("Theme", "dark [muted](auto)[/]")
                .Add("Culture", "en-US")
                .Add("Size", "120 × 36"), "[bold]Terminal profile[/]");
        }

        private static IWidget Migration() {
            return new Rows(
                new Rule("[bold]Write-Color[/] [muted]→[/] [bold]Write-ConsoleText[/]"),
                new TextBlock("[muted]-Text[/]  [accent]-Text[/]   [muted]-Color[/]  [accent]-Color[/]"),
                new TextBlock("[muted]-LogFile[/]  [accent]-LogFile[/]  [pass]√ same encoding[/]"),
                new Rule("[bold]Spectre.Console[/] [muted]→[/] [bold]markup[/]"),
                new TextBlock("[[bold red]] [bold red]text[/] [[/]]  [pass]√ unchanged[/]"));
        }

        private static IWidget Namespaces() {
            Tree tree = new Tree("[bold]ConsoleMonster[/]");
            IEnumerable<string> names = typeof(ConsoleMonster.Terminal).Assembly.GetExportedTypes()
                .Select(t => t.Namespace ?? string.Empty)
                .Where(n => n.StartsWith("ConsoleMonster.", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal);
            foreach (string name in names) {
                int count = typeof(ConsoleMonster.Terminal).Assembly.GetExportedTypes().Count(t => t.Namespace == name);
                tree.Add(name.Substring("ConsoleMonster.".Length), null, count + " types");
            }
            return tree;
        }

        private static IWidget Commands(Site site) {
            string manifest = File.ReadAllText(Path.Combine(site.Repository, "Module", "ConsoleMonster.psd1"));
            Match list = Regex.Match(manifest, @"CmdletsToExport\s*=\s*@\(([^)]*)\)");
            Dictionary<string, int> verbs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(list.Groups[1].Value, "'([A-Za-z]+)-[^']+'")) {
                verbs[m.Groups[1].Value] = verbs.TryGetValue(m.Groups[1].Value, out int n) ? n + 1 : 1;
            }
            BarChart chart = new BarChart { BarToken = ThemeToken.Series3 };
            foreach (KeyValuePair<string, int> verb in verbs.OrderByDescending(v => v.Value).ThenBy(v => v.Key, StringComparer.Ordinal)) {
                chart.Add(verb.Key + "-Console*", verb.Value);
            }
            return chart;
        }

    }
}
