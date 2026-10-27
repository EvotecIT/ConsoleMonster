using System;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed class DefaultColorSiblingTests {
        private const string Baseline = "\u001b[0;30;107m";
        private static TerminalProfile Profile => TerminalProfile.Fixed(60, 10).WithDefaultColors(Color.FromAnsi16(0), Color.FromAnsi16(15));

        [Fact]
        public void InlineSurface_FrameAndCompletionSummary_UseTheSameProfileBaseline() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (InlineSurface surface = new InlineSurface(Profile, writer, sizeSource: () => new[] { 60, 10 })) {
                CellBuffer frame = new CellBuffer(8, 1);
                frame.Write(0, 0, "member", Style.Plain);
                surface.Present(frame, null);
                surface.Complete(new[] { StyledLine.From("done", new Style(Color.FromAnsi16(1))) });
            }
            string encoded = writer.ToString();
            Assert.Contains(Baseline + "member" + Baseline, encoded, StringComparison.Ordinal);
            Assert.Contains("\u001b[0;31;107mdone" + Baseline, encoded, StringComparison.Ordinal);
            Assert.DoesNotContain("\u001b[0m", encoded, StringComparison.Ordinal);
        }

        [Fact]
        public void InlineArea_FrameRows_RestoreTheBaselineBeforeErase() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (InlineArea area = new InlineArea(Profile, writer, new UnavailableScreen())) {
                CellBuffer frame = new CellBuffer(8, 1);
                frame.Write(0, 0, "member", Style.Plain);
                area.Draw(frame);
            }
            Assert.Contains(Baseline + "member" + Baseline + "\u001b[K", writer.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void ShellPrompt_ToAnsi_PreservesTheBaselineAtTheInputBoundaryAndPlainSuppressesSgr() {
            ShellPrompt prompt = new ShellPrompt { Path = "C:/work", Succeeded = false };
            string encoded = prompt.ToAnsi(Terminal.CreateContext(Profile, Theme.Light), 60);
            Assert.EndsWith(Baseline, encoded, StringComparison.Ordinal);
            Assert.DoesNotContain("\u001b[0m", encoded, StringComparison.Ordinal);
            string plain = prompt.ToAnsi(Terminal.CreateContext(Profile.WithMode(OutputMode.Plain), Theme.Light), 60);
            Assert.Empty(Regex.Matches(plain, "\u001b\\[[0-9;]*m"));
        }

        [Fact]
        public void LightThemeTree_DefaultTextStaysDefaultInCellsAndInheritsBaselineOnlyInEncoder() {
            RenderContext context = Terminal.CreateContext(Profile, Theme.Light);
            Tree tree = new Tree();
            tree.Add("OriginalName");
            CellBuffer frame = FrameComposer.RenderBlock(tree, 60, context);
            string row = frame.ToPlainText();
            int x = row.IndexOf("OriginalName", StringComparison.Ordinal);
            Assert.True(frame[x, 0].Style.Color.IsDefault);
            Assert.True(context.Style(ThemeToken.Text).Color.IsDefault);
            StringTerminalWriter writer = new StringTerminalWriter();
            using (LineOutput output = new LineOutput(writer, Profile, false)) output.WriteBlock(frame);
            Assert.Contains(Baseline + "OriginalName", writer.ToString(), StringComparison.Ordinal);
            Assert.True(Theme.Light.Resolve(ThemeToken.Text, ColorDepth.TrueColor).Color.IsDefault);
        }

        private sealed class UnavailableScreen : IConsoleScreen {
            public bool TryGetCursor(out int x, out int y, out int width, out int windowTop) {
                x = y = width = windowTop = 0;
                return false;
            }

            public string? ReadRow(int y, int width) => null;
        }
    }
}
