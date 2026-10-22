using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Input edits and drawing share the control lock; callbacks remain safe to read the control.</summary>
    public sealed class InputControlConcurrencyTests {
        /// <summary>Every activation commits once even while other producers edit and a renderer reads the same control.</summary>
        [Fact]
        public async Task ManyProducers_KeepEveryEditAndRenderValidFrames() {
            const int producers = 4;
            const int edits = 501;
            Toggle toggle = new Toggle("Enabled");
            int changes = 0;
            toggle.Changed += (_, change) => {
                Assert.IsType<bool>(change.Value);
                _ = toggle.Value;
                Interlocked.Increment(ref changes);
            };
            using (ManualResetEventSlim start = new ManualResetEventSlim()) {
                Task[] writers = Enumerable.Range(0, producers).Select(_ => Task.Run(() => {
                    RenderContext context = Context();
                    start.Wait();
                    for (int i = 0; i < edits; i++) { Assert.True(toggle.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), context)); }
                })).ToArray();
                Task renderer = Task.Run(() => {
                    RenderContext context = Context();
                    start.Wait();
                    for (int i = 0; i < edits; i++) {
                        CellBuffer frame = new CellBuffer(30, 3);
                        toggle.Render(new Region(frame), context);
                        string text = frame.ToPlainText();
                        Assert.True(text.Contains("[x]") || text.Contains("[ ]"));
                    }
                });
                start.Set();
                Task completed = Task.WhenAll(writers.Concat(new[] { renderer }));
                Assert.Same(completed, await Task.WhenAny(completed, Task.Delay(TimeSpan.FromSeconds(30))));
                await completed;
            }
            Assert.Equal(producers * edits, changes);
            Assert.False(toggle.Value);
        }

        private static RenderContext Context() => new RenderContext(TerminalProfile.Fixed(30, 3, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
    }
}
