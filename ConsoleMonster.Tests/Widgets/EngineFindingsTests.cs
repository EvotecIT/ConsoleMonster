using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Data;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Findings {
    /// <summary>Narrow header bars, markup wrapping, empty text keeping values, and identifier columns.</summary>
    public sealed class EngineFindingsTests {
        private static HeaderBar LongHeader() => new HeaderBar("Patching the production web farm") {
            Detail = "waiting for approval from the change board",
            Status = StatusKind.Warn,
            HeaderRight = "wave 2 of 5",
            ShowElapsed = true,
            Elapsed = TimeSpan.FromSeconds(754)
        };

        [Theory]
        [InlineData(64, GlyphTier.Full)]
        [InlineData(64, GlyphTier.Ascii)]
        [InlineData(48, GlyphTier.Full)]
        [InlineData(48, GlyphTier.Ascii)]
        public void HeaderBar_Narrow_KeepsTheTitleAndShortensTheRight(int width, GlyphTier glyphs) {
            string text = TestTerminal.RenderText(LongHeader(), width, 1, glyphs).TrimEnd('\n', '\r');
            Assert.Contains("Patching", text);
            Assert.True(text.Length <= width);
            Golden.Match("widgets/header.narrow." + width + "." + glyphs.ToString().ToLowerInvariant() + ".txt", text);
        }

        [Fact]
        public void HeaderBar_Wide_ShowsEverything() {
            string text = TestTerminal.RenderText(LongHeader(), 140, 1);
            Assert.Contains("Patching the production web farm", text);
            Assert.Contains("waiting for approval from the change board", text);
            Assert.Contains("wave 2 of 5", text);
            Assert.Contains("12:34", text);
        }

        [Fact]
        public void HeaderBar_VeryNarrow_DropsTheRightPartInsteadOfOverlapping() {
            string text = TestTerminal.RenderText(LongHeader(), 20, 1).TrimEnd();
            Assert.StartsWith(" Patching", text);
            Assert.DoesNotContain("wave", text);
        }

        [Fact]
        public void WriteMarkup_LongLine_WrapsAtWordBoundaries() {
            TerminalProfile profile = TerminalProfile.Fixed(20, 10, ColorDepth.None, GlyphTier.Ascii);
            using (StringTerminalWriter writer = new StringTerminalWriter()) {
                Terminal.WriteMarkupLine("[pass]alpha beta gamma delta epsilon zeta[/]", profile, writer);
                string[] lines = writer.ToString().Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
                Assert.Equal(new[] { "alpha beta gamma", "delta epsilon zeta" }, lines.Select(l => l.TrimEnd()).ToArray());
            }
        }

        [Fact]
        public void WriteMarkup_Redirected_KeepsWholeLines() {
            TerminalProfile profile = TerminalProfile.Fixed(20, 10, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream);
            using (StringTerminalWriter writer = new StringTerminalWriter()) {
                Terminal.WriteMarkupLine("alpha beta gamma delta epsilon zeta", profile, writer);
                Assert.Equal("alpha beta gamma delta epsilon zeta\n", writer.ToString().Replace("\r\n", "\n"));
            }
        }

        [Fact]
        public void EmptyText_KeepsLabelsAndDetails() {
            Phases phases = new Phases();
            phases.Set("discover", StatusKind.Running, "Discover servers", "3 found");
            phases.Set("discover", StatusKind.Pass, string.Empty, string.Empty);
            string text = TestTerminal.RenderText(phases, 60);
            Assert.Contains("Discover servers", text);
            Assert.Contains("3 found", text);

            StatusList status = new StatusList();
            status.Set("dc01", "DC01 replication", StatusKind.Running, "checking");
            status.Set("dc01", "", StatusKind.Pass, "");
            text = TestTerminal.RenderText(status, 60);
            Assert.Contains("DC01 replication", text);
            Assert.Contains("checking", text);

            Lanes lanes = new Lanes();
            lanes.Add("web", "Web servers", 10);
            lanes.Add("web", string.Empty);
            lanes.Apply(new LiveEvent("lanes", "advance", key: "web", text: "patching", value: 1));
            lanes.Apply(new LiveEvent("lanes", "advance", key: "web", text: "", value: 1));
            text = TestTerminal.RenderText(lanes, 80);
            Assert.Contains("Web servers", text);
            Assert.Contains("patching", text);

            Spinner spinner = new Spinner("Waiting for DNS", "spin") { Detail = "dc01" };
            spinner.Apply(new LiveEvent("spin", "start", text: ""));
            spinner.Apply(new LiveEvent("spin", "state", text: "", status: "running"));
            text = TestTerminal.RenderText(spinner, 60);
            Assert.Contains("Waiting for DNS", text);
            Assert.Contains("dc01", text);
        }

        [Fact]
        public void IdentifierColumns_AreNotGrouped() {
            Assert.True(IdentifierPresenter.LooksLikeIdentifier("Id"));
            Assert.True(IdentifierPresenter.LooksLikeIdentifier("ProcessId"));
            Assert.True(IdentifierPresenter.LooksLikeIdentifier("PID"));
            Assert.True(IdentifierPresenter.LooksLikeIdentifier("LocalPort"));
            Assert.True(IdentifierPresenter.LooksLikeIdentifier("Year"));
            Assert.False(IdentifierPresenter.LooksLikeIdentifier("Size"));
            Assert.False(IdentifierPresenter.LooksLikeIdentifier("Count"));
            PresentedValue value = new IdentifierPresenter().Present(12345, new PresenterContext(Terminal.CreateContext(TerminalProfile.Fixed(40, 5), Theme.Dark), System.Globalization.CultureInfo.GetCultureInfo("pl-PL")));
            Assert.Equal("12345", value.Text);
            Assert.Same(CellPresenters.Identifier, CellPresenters.FromName("identifier"));
        }
    }
}
