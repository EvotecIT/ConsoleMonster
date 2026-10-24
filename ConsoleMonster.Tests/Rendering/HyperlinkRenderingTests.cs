using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Presenters;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed class HyperlinkRenderingTests {
        private const string Osc = "\u001b]8;;";
        private const string St = "\u001b\\";
        private static TerminalProfile Profile => TerminalProfile.Fixed(100, 20).WithHyperlinks(true);

        [Theory]
        [InlineData("/tmp/report.txt", "file:///tmp/report.txt")]
        [InlineData("/tmp/daily #1.txt", "file:///tmp/daily%20%231.txt")]
        [InlineData("C:\\Reports\\daily #1.txt", "file:///C:/Reports/daily%20%231.txt")]
        [InlineData("\\\\server\\share\\daily #1.txt", "file://server/share/daily%20%231.txt")]
        public void ExplicitPaths_NormalizeInDirectLinesAndFrames(string path, string expected) {
            CellBuffer buffer = new CellBuffer(6, 1);
            buffer.Write(0, 0, "report", Style.Plain.WithLink(path));
            foreach (bool inline in new[] { false, true }) {
                Assert.Contains(Osc + expected + St + "report" + Osc + St, Encode(buffer, Profile.WithColorDepth(ColorDepth.None), inline));
                StringTerminalWriter writer = new StringTerminalWriter();
                using (ITerminalOutput output = inline ? (ITerminalOutput)new InlineOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)
                    : new VtOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)) {
                    output.WriteLine(new StyledLine(new[] { new TextSpan("report", Style.Plain.WithLink(path)) }));
                }
                Assert.Contains(Osc + expected + St + "report" + Osc + St, writer.ToString());
            }
        }

        [Theory]
        [InlineData("https://example.com/docs", "https://example.com/docs")]
        [InlineData("mailto:admin@example.com", "mailto:admin@example.com")]
        [InlineData("C:\\Reports\\daily #1.txt", "file:///C:/Reports/daily%20%231.txt")]
        [InlineData("\\\\server\\share\\daily.txt", "file://server/share/daily.txt")]
        [InlineData("/var/log/daily #1.txt", "file:///var/log/daily%20%231.txt")]
        [InlineData("file:///tmp/report.txt", "file:///tmp/report.txt")]
        public void Presenter_UrlAndPaths_KeepLabelAndEncodeTarget(string address, string expected) {
            PresentedValue presented = new LinkPresenter(_ => "Open", _ => address).Present(address, Context(Profile));
            Assert.Equal("Open", presented.Text);
            Assert.Equal(expected, presented.Line[0].Style.Link);
            Assert.Equal("Open (" + address + ")", new LinkPresenter(_ => "Open", _ => address).Present(address, Context(Profile.WithHyperlinks(false))).Text);
        }

        [Theory]
        [InlineData("script")]
        [InlineData("data")]
        [InlineData("escape")]
        [InlineData("c1-terminator")]
        [InlineData("c1-control")]
        public void UnsafeTarget_NeverEmitsOscEvenWhenAssignedDirectlyToStyle(string caseName) {
            string address = caseName switch {
                "script" => "javascript:alert(1)",
                "data" => "data:text/html,test",
                "escape" => "https://example.com/\u001b]0;injected\u0007",
                "c1-terminator" => "https://example.com/\u009c",
                "c1-control" => "https://example.com/\u0085",
                _ => throw new ArgumentException("Unknown case.", nameof(caseName))
            };
            PresentedValue presented = new LinkPresenter(_ => "Open", _ => address).Present(address, Context(Profile));
            Assert.Null(presented.Line[0].Style.Link);
            CellBuffer buffer = new CellBuffer(10, 1);
            buffer.Write(0, 0, "Open", Style.Plain.WithLink(address));
            Assert.DoesNotContain(Osc, Encode(buffer, Profile, false));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Outputs_UseCapabilityAndCloseLinkBeforeFollowingText(bool inline) {
            CellBuffer buffer = new CellBuffer(20, 1);
            buffer.Write(0, 0, "Open", Style.Plain.WithLink("https://example.com"));
            buffer.Write(4, 0, " next", Style.Plain);
            string encoded = Encode(buffer, Profile.WithColorDepth(ColorDepth.None), inline);
            Assert.Contains(Osc + "https://example.com" + St + "Open" + Osc + St + " next", encoded);
            Assert.DoesNotContain(Osc, Encode(buffer, Profile.WithHyperlinks(false), inline));
            Assert.DoesNotContain(Osc, Encode(buffer, Profile.WithMode(OutputMode.Stream), inline));
            Assert.DoesNotContain(Osc, Encode(buffer, Profile.WithMode(OutputMode.Plain), inline));
            foreach (bool redirected in new[] { true, false }) {
                TerminalProfile guarded = new TerminalProfile(ColorDepth.None, GlyphTier.Full, OutputMode.Interactive,
                    kind: TerminalKind.WindowsTerminal, supportsVirtualTerminal: true,
                    isOutputRedirected: redirected, isContinuousIntegration: !redirected, supportsHyperlinks: true);
                Assert.DoesNotContain(Osc, Encode(buffer, guarded, inline));
            }
        }

        [Fact]
        public void TablesAndLists_UseAutomaticPathsAndDeclaredLinkLabels() {
            Table table = new Table();
            table.AddColumn(new TableColumn("Report") { Presenter = new LinkPresenter(_ => "Daily report", _ => "C:\\Reports\\daily.txt") });
            table.AddRow(TableCell.FromValue("C:\\Reports\\daily.txt"));
            Assert.Contains(Osc + "file:///C:/Reports/daily.txt" + St, Terminal.RenderEncoded(table, Profile, Theme.Dark));
            KeyValueList list = new KeyValueList().AddValue("Report", "/var/log/daily.txt").AddValue("Docs", "https://example.com/docs");
            string encoded = Terminal.RenderEncoded(list, Profile, Theme.Dark);
            Assert.Contains(Osc + "file:///var/log/daily.txt" + St, encoded);
            Assert.Contains(Osc + "https://example.com/docs" + St, encoded);
            string plain = Terminal.RenderEncoded(table, Profile.WithMode(OutputMode.Plain), Theme.Dark);
            Assert.DoesNotContain("\u001b", plain, StringComparison.Ordinal);
            Assert.Contains("Daily report (C:\\Reports\\daily.txt)", plain);
        }

        [Fact]
        public void Logs_LiteralUrlsAndPathsAndRichLabels_AreLinked() {
            LogRegion log = new LogRegion { ShowElapsed = false };
            log.Write("Saved /var/log/daily.txt; docs https://example.com/docs");
            log.Write(RichText.FromMarkup("[link=file:///tmp/daily.txt]Daily report[/]"));
            string encoded = Terminal.RenderEncoded(log, Profile, Theme.Dark);
            Assert.Contains(Osc + "file:///var/log/daily.txt" + St, encoded);
            Assert.Contains(Osc + "https://example.com/docs" + St, encoded);
            Assert.Contains(Osc + "file:///tmp/daily.txt" + St, encoded);
            Assert.DoesNotContain(Osc, Terminal.RenderEncoded(log, Profile.WithHyperlinks(false), Theme.Dark));
        }

        [Fact]
        public void AutoPresenter_KeepsOrdinaryTextAndRecognizesFileObjects() {
            Assert.IsType<TextPresenter>(AutoPresenter.For("ordinary words"));
            Assert.IsType<LinkPresenter>(AutoPresenter.For(new System.IO.FileInfo("report.txt")));
            PresentedValue value = CellPresenters.Auto.Present("/tmp/report.txt", Context(Profile));
            Assert.Equal("/tmp/report.txt", value.Text);
            Assert.Equal("file:///tmp/report.txt", value.Line[0].Style.Link);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DirectLines_LinksSurviveStylesAndWideGlyphsAndCloseAtLineEnd(bool inline) {
            StringTerminalWriter writer = new StringTerminalWriter();
            StyledLine line = new StyledLine(new[] {
                new TextSpan("界 docs ", Style.Plain), new TextSpan("https://example.com/docs", Style.Plain.With(TextDecoration.Bold)),
                new TextSpan(" report", Style.Plain.WithLink("/tmp/report.txt"))
            });
            using (ITerminalOutput output = inline ? (ITerminalOutput)new InlineOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)
                : new VtOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)) {
                output.WriteLine(line);
            }
            string encoded = writer.ToString();
            Assert.Contains("界 docs " + Osc + "https://example.com/docs" + St + "https://example.com/docs", encoded);
            Assert.Contains(Osc + "file:///tmp/report.txt" + St + " report" + Osc + St, encoded);
        }

        [Fact]
        public void LivePartialUpdate_UsesFullExplicitTargetForClippedLabel() {
            StringTerminalWriter writer = new StringTerminalWriter();
            CellBuffer frame = new CellBuffer(20, 2);
            string address = "https://example.com/a/very/long/report";
            frame.Write(0, 0, "Report", Style.Plain.WithLink(address));
            using (VtOutput output = new VtOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)) {
                output.BeginLive();
                output.RenderFrame(frame);
                writer.Clear();
                frame.Write(0, 0, "r", Style.Plain.WithLink(address));
                output.RenderFrame(frame);
                Assert.Contains(Osc + address + St + "r" + Osc + St, writer.ToString());
            }
        }

        [Theory]
        [InlineData(false, "accent")]
        [InlineData(true, "accent")]
        [InlineData(false, "surrogate")]
        [InlineData(true, "surrogate")]
        [InlineData(false, "flag")]
        [InlineData(true, "flag")]
        [InlineData(false, "zwj")]
        [InlineData(true, "zwj")]
        public void DirectLines_PreserveGraphemesAcrossStyledSpans(bool inline, string example) {
            string first;
            string second;
            switch (example) {
                case "accent": first = "e"; second = "\u0301"; break;
                case "surrogate": first = "\ud83d"; second = "\udcbb"; break;
                case "flag": first = "\U0001f1f5"; second = "\U0001f1f1"; break;
                default: first = "\U0001f469"; second = "\u200d\U0001f4bb"; break;
            }
            StyledLine line = new StyledLine(new[] {
                new TextSpan(first, Style.Plain),
                new TextSpan(second, Style.Plain.With(TextDecoration.Bold)),
                new TextSpan(" tail", Style.Plain)
            });
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = inline ? (ITerminalOutput)new InlineOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)
                : new VtOutput(writer, Profile.WithColorDepth(ColorDepth.None), false)) {
                output.WriteLine(line);
            }
            Assert.Equal(first + second + " tail\r\n", writer.ToString().Replace("\u001b[K", string.Empty));
        }

        private static PresenterContext Context(TerminalProfile profile) => new PresenterContext(new RenderContext(profile, Theme.Dark));

        private static string Encode(CellBuffer buffer, TerminalProfile profile, bool inline) {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = inline ? (ITerminalOutput)new InlineOutput(writer, profile, false) : new VtOutput(writer, profile, false)) {
                output.WriteBlock(buffer);
            }
            return writer.ToString();
        }
    }
}
