using System;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    /// <summary>Screen helpers, markup without a newline and the one-call status, in redirected and CI output.</summary>
    [Collection("Live sessions")]
    public sealed class TerminalScreenTests {
        private static readonly TerminalProfile Stream = TerminalProfile.Fixed(60, 20, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream);

        [Fact]
        public void WriteMarkup_WithoutNewLine_LeavesTheLineOpen() {
            using (StringTerminalWriter writer = new StringTerminalWriter()) {
                Terminal.WriteMarkup("[cadetblue]Checking[/] ", false, Stream, writer);
                Terminal.WriteMarkup("[pass]done[/]", true, Stream, writer);
                Terminal.WriteMarkupLine("[muted]next[/]", Stream, writer);
                Assert.Equal("Checking done\nnext\n", writer.ToString().Replace("\r\n", "\n"));
            }
        }

        [Fact]
        public void ClearAndCursor_InStreamMode_AreNoOps() {
            try {
                Terminal.Profile = Stream;
                Terminal.Clear();
                using (Terminal.HideCursor()) {
                    Assert.False(Terminal.IsCursorHidden);
                }
                Terminal.ShowCursor();
                Assert.False(Terminal.IsCursorHidden);
            } finally {
                Terminal.Refresh();
            }
        }

        [Fact]
        public void Status_Redirected_PrintsEachNewTextOnceAsAPlainLine() {
            using (StringTerminalWriter writer = new StringTerminalWriter()) {
                using (StatusContext status = new StatusContext(Stream, "Resolving [accent]dc01[/]", writer)) {
                    Assert.False(status.IsLive);
                    status.Update("Resolving [accent]dc01[/]");
                    status.Update("Querying 3 of 10");
                    Assert.Equal("Querying 3 of 10", status.Text);
                }
                Assert.Equal("Resolving dc01\nQuerying 3 of 10\n", writer.ToString().Replace("\r\n", "\n"));
            }
        }

        [Fact]
        public void Status_ReturnsTheResultAndEndsWhenTheWorkThrows() {
            try {
                Terminal.Profile = Stream;
                Assert.Equal(42, Terminal.Status("Working", ctx => 42));
                Assert.Throws<InvalidOperationException>(() => Terminal.Status("Working", ctx => { throw new InvalidOperationException(); }));
            } finally {
                Terminal.Refresh();
            }
        }

        [Fact]
        public async Task StatusAsync_AwaitsTheWork() {
            try {
                Terminal.Profile = Stream;
                int value = await Terminal.StatusAsync("Waiting", async ctx => {
                    await Task.Yield();
                    ctx.Update("Still waiting");
                    return 7;
                });
                Assert.Equal(7, value);
                bool ran = false;
                await Terminal.StatusAsync("Waiting", async ctx => {
                    await Task.Yield();
                    ran = true;
                });
                Assert.True(ran);
            } finally {
                Terminal.Refresh();
            }
        }
    }
}
