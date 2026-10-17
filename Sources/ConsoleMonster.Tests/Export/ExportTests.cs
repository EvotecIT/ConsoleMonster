using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    /// <summary>
    /// The frame exporter: HTML, SVG and text per glyph tier, escaping, links, wide characters on the grid, box/block/braille
    /// shapes, live snapshots, and recordings as a player, an animated SVG and valid asciicast v3.
    /// </summary>
    public sealed class ExportTests {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // A small scene with everything the writers treat specially: a border, a link, a wide glyph, a bar, braille, a background.
        private static IWidget Scene() {
            Table table = new Table { Border = TableBorder.Rounded };
            table.AddColumn("Check");
            table.AddColumn("Status");
            table.AddRow("SPF", "[pass]PASS[/]");
            table.AddRow("DKIM", "[warn on #3a2a00]WARN[/]");
            table.AddRow("[link=https://example.org/docs]docs[/]", "日本");
            return new Rows(
                new Panel(table, "Mail <checks> & \"more\""),
                new TextBlock("██▌ ⣿⣀ ═══ <script>alert(1)</script>") { IsMarkup = false }
            );
        }

        private static ExportOptions Options(GlyphTier tier, ExportTheme theme = ExportTheme.Dark) {
            return new ExportOptions { Width = 40, Theme = theme, Glyphs = tier, Culture = Invariant };
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void ToHtml_PerTier_MatchesGolden(GlyphTier tier) {
            string html = ConsoleExporter.ToHtml(Scene(), Options(tier));
            Golden.Match("export/scene." + tier.ToString().ToLowerInvariant() + ".html", html);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void ToSvg_PerTier_MatchesGolden(GlyphTier tier) {
            string svg = ConsoleExporter.ToSvg(Scene(), Options(tier));
            Golden.Match("export/scene." + tier.ToString().ToLowerInvariant() + ".svg", svg);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void ToText_PerTier_MatchesGolden(GlyphTier tier) {
            Golden.Match("export/scene." + tier.ToString().ToLowerInvariant() + ".txt", ConsoleExporter.ToText(Scene(), Options(tier)));
        }

        [Fact]
        public void ToHtml_EscapesTextAndAttributes() {
            string html = ConsoleExporter.ToHtml(Scene(), new ExportOptions { Width = 40, Title = "A <b>\"title\"</b>", WindowChrome = true });
            Assert.DoesNotContain("<script>", html);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
            Assert.Contains("Mail &lt;checks&gt; &amp; &quot;more&quot;", html);
            Assert.Contains("<title>A &lt;b&gt;&quot;title&quot;&lt;/b&gt;</title>", html);
            Assert.DoesNotContain("<b>", html);
        }

        [Fact]
        public void ToSvg_EscapesText() {
            string svg = ConsoleExporter.ToSvg(Scene(), Options(GlyphTier.Full));
            Assert.DoesNotContain("<script>", svg);
            Assert.Contains("&lt;", svg);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("JaVaScRiPt:alert(1)")]
        [InlineData(" javascript:alert(1)")]
        [InlineData("java\tscript:alert(1)")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("file:///c:/windows/win.ini")]
        [InlineData("/relative/path")]
        public void Links_UnsafeTargets_AreDropped(string link) {
            CellBuffer frame = new CellBuffer(10, 1);
            frame.Write(0, 0, "click", Style.Plain.WithLink(link));
            string html = ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Dark });
            string svg = ConsoleExporter.ToSvg(frame, new ExportOptions { Theme = ExportTheme.Dark });
            Assert.DoesNotContain("href", html);
            Assert.DoesNotContain("href", svg);
            Assert.Contains("click", html);
        }

        [Theory]
        [InlineData("https://example.org/a?b=1&c=2")]
        [InlineData("http://example.org")]
        [InlineData("mailto:ops@example.org")]
        public void Links_WebAndMailTargets_BecomeLinks(string link) {
            CellBuffer frame = new CellBuffer(10, 1);
            frame.Write(0, 0, "open", Style.Plain.WithLink(link));
            string escaped = link.Replace("&", "&amp;");
            Assert.Contains("<a href=\"" + escaped + "\"", ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Dark }));
            Assert.Contains("<a href=\"" + escaped + "\"", ConsoleExporter.ToSvg(frame, new ExportOptions { Theme = ExportTheme.Dark }));
        }

        [Fact]
        public void WideCharacters_StayOnTheGrid() {
            CellBuffer frame = new CellBuffer(8, 1);
            frame.Write(0, 0, "a日b", Style.Plain);
            string html = ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Dark });
            Assert.Contains("a<span class=\"w\">日</span>b", html);
            ExportOptions options = new ExportOptions { Theme = ExportTheme.Dark };
            string svg = ConsoleExporter.ToSvg(frame, options);
            // Centres: a in cell 0, the wide glyph across cells 1-2, b in cell 3.
            string cw = options.CellWidth.ToString(Invariant);
            Match text = Regex.Match(svg, "<text class=\"cm-t\" x=\"([^\"]+)\"");
            Assert.True(text.Success, svg);
            double[] xs = text.Groups[1].Value.Split(' ').Select(v => double.Parse(v, Invariant)).ToArray();
            Assert.Equal(new[] { options.CellWidth * 0.5, options.CellWidth * 2, options.CellWidth * 3.5 }.Select(v => Math.Round(v, 2)), xs);
            Assert.Contains(">a日b<", svg);
        }

        [Fact]
        public void Svg_DrawsBoxBlockAndBrailleAsShapes() {
            CellBuffer frame = new CellBuffer(6, 1);
            frame.Write(0, 0, "╭─█⣿▐╯", Style.Plain);
            string svg = ConsoleExporter.ToSvg(frame, new ExportOptions { Theme = ExportTheme.Dark });
            Assert.DoesNotContain("─", svg);
            Assert.DoesNotContain("█", svg);
            Assert.DoesNotContain("⣿", svg);
            Assert.Equal(16, Regex.Matches(svg, " 0 1 0 ").Count);
            Assert.Equal(2, Regex.Matches(svg, "A[0-9.]+ [0-9.]+ 0 0 ").Count);
        }

        [Fact]
        public void Html_PaintsBlocksAndBrailleAsBackgrounds() {
            CellBuffer frame = new CellBuffer(4, 1);
            frame.Write(0, 0, "█▄⣿─", Style.Plain);
            string html = ConsoleExporter.ToHtml(frame, new ExportOptions { Theme = ExportTheme.Dark });
            Assert.Contains("<span class=\"b b2588\">█</span>", html);
            Assert.Contains(".b2584{background:linear-gradient(to top,currentColor 50%,transparent 0)}", html);
            Assert.Contains(".b28ff{background:radial-gradient(", html);
            Assert.Contains("─", html);
        }

        [Fact]
        public void AutoTheme_RendersDarkAndLightForWidgetsAndVariablesForFrames() {
            string widget = ConsoleExporter.ToHtml(Scene(), new ExportOptions { Width = 40 });
            Assert.Contains("cm-screen cm-dark", widget);
            Assert.Contains("cm-screen cm-light", widget);
            string svg = ConsoleExporter.ToSvg(Scene(), new ExportOptions { Width = 40 });
            Assert.Contains("class=\"cm-layer cm-dark\"", svg);
            Assert.Contains("class=\"cm-layer cm-light\"", svg);
            CellBuffer frame = new CellBuffer(4, 1);
            frame.Write(0, 0, "hi", Style.Plain);
            string adaptive = ConsoleExporter.ToSvg(frame);
            Assert.Contains("style=\"fill:var(--cm-fg)\"", adaptive);
            Assert.Contains("@media (prefers-color-scheme: light)", adaptive);
        }

        [Fact]
        public void Options_RejectUnsafeFontFamilies() {
            Assert.Throws<ArgumentException>(() => ConsoleExporter.ToHtml(Scene(), new ExportOptions { FontFamily = "x;}</style><script>" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleExporter.ToSvg(Scene(), new ExportOptions { Width = 0 }));
        }

        [Fact]
        public void ToText_TrimsTrailingSpaceAndEmptyLines() {
            CellBuffer frame = new CellBuffer(10, 4);
            frame.Write(0, 0, "one", Style.Plain);
            frame.Write(2, 1, "two", Style.Plain);
            Assert.Equal("one\n  two\n", ConsoleExporter.ToText(frame));
        }

        [Fact]
        public void ScreenCapture_DrivesAPromptAndExportsTheScreen() {
            ScreenCapture capture = new ScreenCapture(50, 12, inline: true);
            SelectPrompt<string> prompt = new SelectPrompt<string>("Environment", new[] { new Choice("prod", "Production"), new Choice("lab", "Lab") });
            string chosen = prompt.Show(new InteractiveOptions { Mode = InteractionMode.Rich, Input = new ScriptedInput("Down Enter") { Paced = true }, Capture = capture, Theme = Theme.Dark });
            Assert.Equal("lab", chosen);
            CellBuffer? screen = capture.FindFrame("Lab");
            Assert.NotNull(screen);
            Assert.Contains("Production", ConsoleExporter.ToText(screen!));
            Assert.Contains("Environment", ConsoleExporter.ToHtml(screen!));
        }

        // ---- recordings ----------------------------------------------------------------------------------------------

        private static IWidget Build(out string recording) {
            ProgressBar progress = new ProgressBar("Build", "build") { Total = 10 };
            StatusList steps = new StatusList("steps");
            steps.Set("compile", "Compile", StatusKind.Pending);
            steps.Set("test", "Test", StatusKind.Pending);
            StringWriter writer = new StringWriter();
            using (LiveRecorder recorder = new LiveRecorder(writer)) {
                recorder.Write(new LiveEvent("steps", "set", key: "compile", state: "running", time: 1));
                recorder.Write(new LiveEvent("build", "advance", value: 4, time: 400));
                recorder.Write(new LiveEvent("steps", "set", key: "compile", state: "pass", time: 800));
                recorder.Write(new LiveEvent("build", "advance", value: 6, time: 1500));
                recorder.Write(new LiveEvent("steps", "set", key: "test", state: "fail", time: 1600));
            }
            recording = writer.ToString();
            return new Rows(progress, steps);
        }

        [Fact]
        public void Recording_Asciicast_IsValidV3() {
            IWidget root = Build(out string recording);
            string cast = Recording.ExportAsciicast(root, new StringReader(recording), new RecordingExportOptions { Width = 40, Height = 6, FramesPerSecond = 5, Title = "Build", Timestamp = new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero) });
            string[] lines = cast.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith("{\"version\":3,\"term\":{\"cols\":40,\"rows\":6,\"type\":\"xterm-256color\",\"theme\":{\"fg\":\"#cccccc\",\"bg\":\"#0c0c0c\",\"palette\":\"#0c0c0c:", lines[0]);
            Assert.Contains("\"timestamp\":1792195200", lines[0]);
            Assert.Contains("\"title\":\"Build\"", lines[0]);
            Assert.True(lines.Length >= 4, cast);
            double total = 0;
            Regex eventLine = new Regex("^\\[(?<t>\\d+(\\.\\d+)?),\"o\",\"(?<data>(?:[^\"\\\\]|\\\\.)*)\"\\]$");
            foreach (string line in lines.Skip(1)) {
                Match m = eventLine.Match(line);
                Assert.True(m.Success, line);
                double interval = double.Parse(m.Groups["t"].Value, Invariant);
                Assert.True(interval >= 0);
                total += interval;
                Assert.DoesNotContain("\u001b", m.Groups["data"].Value, StringComparison.Ordinal);
            }
            Assert.Contains("\\u001b[2J", lines[1]);
            Assert.True(total >= 1.6, "playback covers the recording");
        }

        [Fact]
        public void Recording_AnimatedSvg_UsesKeyframesAndSharedRows() {
            IWidget root = Build(out string recording);
            string svg = Recording.ExportSvg(root, new StringReader(recording), new RecordingExportOptions { Width = 40, Height = 6, FramesPerSecond = 5 });
            Assert.Contains("@keyframes cm-play{0%{transform:translateX(0px)}", svg);
            Assert.Contains("animation:cm-play ", svg);
            Assert.DoesNotContain("<script", svg);
            int defs = Regex.Matches(svg, "<g id=\"r\\d+\">").Count;
            int uses = Regex.Matches(svg, "<use ").Count;
            Assert.True(uses > defs, "rows that do not change are drawn once and reused");
        }

        [Fact]
        public void Recording_HtmlPlayer_KeepsRowsInsideTheScript() {
            IWidget root = new Rows(new LogRegion("log"));
            string recording = "{\"consolemonster\":\"recording\",\"version\":1}\n{\"t\":10,\"target\":\"log\",\"op\":\"write\",\"text\":\"</script><script>alert(1)</script>\"}\n";
            string html = Recording.ExportHtml(root, new StringReader(recording), new RecordingExportOptions { Width = 50, Height = 4 });
            Assert.Single(Regex.Matches(html, "<script>"));
            Assert.Single(Regex.Matches(html, "</script>"));
            Assert.Contains("\\u0026lt;/script\\u0026gt;", html);
            Assert.Contains("id=\"cm-seek\"", html);
            Assert.Contains("location.hash", html);
        }

        [Fact]
        public void Recording_IdenticalFramesAreMerged() {
            // A progress bar only (no spinner), so frames change only when an event arrives.
            IWidget root = new Rows(new ProgressBar("Build", "build") { Total = 10 });
            string recording = "{\"consolemonster\":\"recording\",\"version\":1}\n{\"t\":400,\"target\":\"build\",\"op\":\"advance\",\"value\":4}\n{\"t\":1600,\"target\":\"build\",\"op\":\"advance\",\"value\":6}\n";
            string cast = Recording.ExportAsciicast(root, new StringReader(recording), new RecordingExportOptions { Width = 40, Height = 3, FramesPerSecond = 50 });
            int events = cast.Split('\n').Count(l => l.StartsWith("[", StringComparison.Ordinal));
            Assert.Equal(4, events);
        }
    }
    /// <summary>Live-session snapshots, in the live-session collection because sessions take the console.</summary>
    [Collection("Live sessions")]
    public sealed class ExportSnapshotTests {
        [Fact]
        public void Snapshot_ExportsWhatTheSessionShows() {
            ProgressBar progress = new ProgressBar("Deploy", "deploy") { Total = 4 };
            ConsoleMonster.Tests.Live.FakeOutput output = new ConsoleMonster.Tests.Live.FakeOutput();
            LiveSessionOptions options = ConsoleMonster.Tests.Live.LiveSessionTests.Options(output);
            options.WindowSource = () => new[] { 0, 0, 40, 5 };            using (LiveSession session = new LiveSession(new Rows(progress), options)) {
                Assert.Null(session.Snapshot());
                session.Start();
                session.Post(new LiveEvent("deploy", "advance", value: 2));
                SpinWait.SpinUntil(() => session.Snapshot() != null && ConsoleExporter.ToText(session).Contains("50%"), 3000);
                string text = ConsoleExporter.ToText(session);
                Assert.Contains("Deploy", text);
                Assert.Contains("50%", text);
                Assert.Contains("cm-screen", ConsoleExporter.ToHtml(session));
                session.Stop();
            }
        }
    }
}
