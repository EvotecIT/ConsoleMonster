using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    /// <summary>Chrome stays consistent across frames, dashboards and terminal-session exports.</summary>
    public sealed class WindowChromeStyleTests {
        public static IEnumerable<object[]> Styles() {
            foreach (WindowChromeStyle style in new[] { WindowChromeStyle.None, WindowChromeStyle.MacOS, WindowChromeStyle.Windows, WindowChromeStyle.Linux }) {
                foreach (ExportTheme theme in new[] { ExportTheme.Dark, ExportTheme.Light }) { yield return new object[] { style, theme }; }
            }
        }

        [Theory, MemberData(nameof(Styles))]
        public void FrameStyleHasReviewedHtmlAndSvg(WindowChromeStyle style, ExportTheme theme) {
            ExportOptions options = Options(style, theme);
            string stem = "export/chrome-" + style.ToString().ToLowerInvariant() + "." + theme.ToString().ToLowerInvariant();
            string svg = ConsoleExporter.ToSvg(Frame(), options);
            string html = ConsoleExporter.ToHtml(Frame(), options);
            Golden.Match(stem + ".svg", svg);
            Golden.Match(stem + ".html", html);
            XElement root = XElement.Parse(svg);
            Assert.Equal(style == WindowChromeStyle.None ? 57.6 : Math.Ceiling(16.8 * 2 + 40 + Bar(style)), Number(root, "height"), 2);
            if (style == WindowChromeStyle.None) {
                Assert.DoesNotContain("cm-titlebar", html);
                Assert.DoesNotContain(root.Elements(), e => (string?)e.Attribute("class") == "cm-title-area");
            } else {
                Assert.Contains("cm-chrome-" + style.ToString().ToLowerInvariant(), html);
                Assert.Contains("height:" + Bar(style) + "px", html);
                XElement area = Assert.Single(root.Elements(), e => (string?)e.Attribute("class") == "cm-title-area");
                XElement title = Assert.Single(area.Elements());
                Assert.Null(title.Attribute("textLength"));
                Assert.Null(title.Attribute("lengthAdjust"));
                Assert.Equal(options.Title, Assert.Single(title.Elements()).Value);
            }
        }

        [Theory, MemberData(nameof(Styles))]
        public void MinimumWidthsKeepControlsAndTruncateWithoutFontMetrics(WindowChromeStyle style, ExportTheme theme) {
            ExportOptions options = Options(style, theme);
            options.Padding = 0; options.FontSize = 4;
            CellBuffer frame = new CellBuffer(1, 1);
            string markup = ConsoleExporter.ToSvg(frame, options);
            XElement svg = XElement.Parse(markup);
            Assert.Equal(style == WindowChromeStyle.None ? 2.4 : Minimum(style) + 16, Number(svg, "width"));
            XElement? area = svg.Elements().SingleOrDefault(e => (string?)e.Attribute("class") == "cm-title-area");
            if (style == WindowChromeStyle.Windows) {
                Assert.NotNull(area);
                XElement title = Assert.Single(area!.Elements());
                Assert.Equal("Bu…", string.Concat(title.Nodes().OfType<XText>().Select(t => t.Value)));
                Assert.Null(title.Attribute("textLength"));
                Assert.Null(title.Attribute("lengthAdjust"));
            } else { Assert.Null(area); }
            string html = ConsoleExporter.ToHtml(frame, options);
            if (style != WindowChromeStyle.None) { Assert.Contains("min-width:" + Minimum(style) + "px", html); }
            Golden.Match("export/chrome-" + style.ToString().ToLowerInvariant() + "." + theme.ToString().ToLowerInvariant() + ".minimum.svg", markup);
            Golden.Match("export/chrome-" + style.ToString().ToLowerInvariant() + "." + theme.ToString().ToLowerInvariant() + ".minimum.html", html);
        }

        [Theory, MemberData(nameof(Styles))]
        public void DashboardAndSessionCarryStyleThroughPlayerAndAnimation(WindowChromeStyle style, ExportTheme theme) {
            RecordingExportOptions options = new RecordingExportOptions { Width = 36, Height = 2, Title = "Build <release>", WindowChrome = true, ChromeStyle = style, Theme = theme };
            LiveEvent[] events = { new LiveEvent("log", "write", text: "Complete", time: 20) };
            string dashboardData = Recording.ExportPlayerData(new LogRegion("log"), events, options);
            string dashboardSvg = Recording.Export(new LogRegion("log"), events, ExportFormat.Svg, options);
            SessionRecording session = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":36,\"height\":2}\n[0.02,\"o\",\"Complete\"]\n"));
            foreach (string json in new[] { dashboardData, session.ExportPlayerData(options) }) {
                Assert.Contains("\"chromeStyle\":\"" + style.ToString().ToLowerInvariant() + "\"", json);
                Assert.Contains("\"chrome\":" + (style == WindowChromeStyle.None ? "false" : "true"), json);
                Assert.DoesNotContain("<release>", json);
                if (style != WindowChromeStyle.None) { Assert.Contains("cm-titlebar", json); }
            }
            foreach (string markup in new[] { dashboardSvg, session.Export(ExportFormat.Svg, options) }) {
                XElement svg = XElement.Parse(markup);
                XElement viewport = Assert.Single(svg.Descendants(), e => e.Name.LocalName == "svg" && (string?)e.Attribute("class") != "cm-title-area");
                Assert.Equal(Bar(style) + (style == WindowChromeStyle.None ? 0 : 8), Number(viewport, "y"));
            }
            Assert.Contains("chromeBar", session.Export(ExportFormat.Html, options));
            Assert.Equal(style, options.ChromeStyle);
        }

        [Fact]
        public void MixedStyleFragmentsKeepEachTitleBarRuleScoped() {
            string html = string.Empty;
            foreach (WindowChromeStyle style in new[] { WindowChromeStyle.MacOS, WindowChromeStyle.Windows, WindowChromeStyle.Linux }) {
                ExportOptions options = Options(style, ExportTheme.Dark);
                options.Fragment = true;
                string fragment = ConsoleExporter.ToHtml(Frame(), options);
                Assert.Contains(".cm-window.cm-chrome-" + style.ToString().ToLowerInvariant() + " .cm-titlebar{", fragment);
                Assert.DoesNotContain(".cm-export .cm-titlebar{", fragment);
                html += fragment + "\n";
            }
            Golden.Match("export/chrome.mixed-fragments.html", html.TrimEnd() + "\n");
        }

        [Fact]
        public void BoolDefaultsAndNonePreserveExistingOptInContract() {
            Assert.Equal(WindowChromeStyle.MacOS, new ExportOptions().ChromeStyle);
            Assert.Equal(WindowChromeStyle.MacOS, new RecordingExportOptions().ChromeStyle);
            ExportOptions hidden = Options(WindowChromeStyle.Windows, ExportTheme.Dark); hidden.WindowChrome = false;
            ExportOptions none = Options(WindowChromeStyle.None, ExportTheme.Dark);
            Assert.Equal(ConsoleExporter.ToSvg(Frame(), none), ConsoleExporter.ToSvg(Frame(), hidden));
            Assert.Equal(ConsoleExporter.ToHtml(Frame(), none), ConsoleExporter.ToHtml(Frame(), hidden));
            Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleExporter.ToSvg(Frame(), new ExportOptions { ChromeStyle = (WindowChromeStyle)77 }));
        }

        [Fact]
        public void ChromeUsesCustomSurfaceAndMutedTokensInEveryWriter() {
            Theme theme = Theme.Dark.With(ThemeToken.Surface, new ThemeStyle(null, ThemeColor.Hex("#314159", 4, 4)))
                .With(ThemeToken.Muted, new ThemeStyle(ThemeColor.Hex("#abcdef", 7, 7)));
            ExportOptions options = Options(WindowChromeStyle.Windows, ExportTheme.Auto);
            options.DarkTheme = theme; options.LightTheme = theme;
            foreach (string output in new[] { ConsoleExporter.ToSvg(Frame(), options), ConsoleExporter.ToHtml(Frame(), options) }) {
                Assert.Contains("--cm-chrome:#314159", output);
                Assert.Contains("--cm-chrome-fg:#abcdef", output);
            }
            RecordingExportOptions recording = new RecordingExportOptions { Width = 36, Height = 2, WindowChrome = true, ChromeStyle = WindowChromeStyle.Linux, CustomTheme = theme };
            Assert.Contains("--cm-chrome:#314159", Recording.ExportPlayerData(new LogRegion("log"), Array.Empty<LiveEvent>(), recording));
        }

        private static ExportOptions Options(WindowChromeStyle style, ExportTheme theme) => new ExportOptions { WindowChrome = true, ChromeStyle = style, Theme = theme, Title = "Build <release> & verify / " + new string('W', 40) };
        private static CellBuffer Frame() { CellBuffer frame = new CellBuffer(36, 2); frame.Write(0, 0, "Build complete", Style.Plain); return frame; }
        private static double Bar(WindowChromeStyle style) => style == WindowChromeStyle.MacOS ? 28 : style == WindowChromeStyle.Windows ? 32 : style == WindowChromeStyle.Linux ? 36 : 0;
        private static double Minimum(WindowChromeStyle style) => style == WindowChromeStyle.Windows ? 208 : style == WindowChromeStyle.Linux ? 96 : 76;
        private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    }
}
