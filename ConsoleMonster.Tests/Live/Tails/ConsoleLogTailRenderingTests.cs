using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Live.Tails {
    [Collection("Live sessions")]
    public sealed class ConsoleLogTailRenderingTests {
        [Fact]
        public void RunnerRendersAndNavigatesRealVtFramesThenRestoresAlternateScreen() {
            TerminalProfile profile = TerminalProfile.Fixed(160, 4, ColorDepth.None, GlyphTier.Ascii);
            using (ScreenWriter writer = new ScreenWriter())
            using (VtOutput output = new VtOutput(writer, profile, false)) {
                int reads = 0;
                Navigation input = new Navigation(writer);
                ConsoleLogTail.Run(() => {
                    if (Interlocked.Increment(ref reads) == 1) {
                        return Enumerable.Range(1, 6).Select(i => new TailedLogEntry("[red]L" + i + "[/]")).ToArray();
                    }
                    if (input.SupplyNewLine && Interlocked.CompareExchange(ref input.Supplied, 1, 0) == 0) {
                        return new[] { new TailedLogEntry("[red]L7[/]") };
                    }
                    return Array.Empty<TailedLogEntry>();
                }, new LogTailOptions {
                    Profile = profile, Output = output, Input = input, HandleCancelKey = false, Filter = "[RED]l",
                    PollInterval = TimeSpan.FromMilliseconds(1)
                });
                Assert.Equal(4, input.Step);
                Assert.True(input.Supplied == 1);
                Assert.True(writer.Began);
                Assert.True(writer.Ended);
                Assert.False(writer.Disposed);
                Assert.Contains("q/Esc/Ctrl+C", writer.LastRows[3]);
            }
        }

        private sealed class Navigation : IInputSource {
            private readonly ScreenWriter _writer;
            internal int Step;
            internal volatile bool SupplyNewLine;
            internal int Supplied;
            internal Navigation(ScreenWriter writer) { _writer = writer; }
            public string Name => "frame-synchronized navigation";
            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                switch (Step) {
                    case 0:
                        _writer.Wait(rows => rows[0] == "[red]L5[/]" && rows[2].StartsWith("Following", StringComparison.Ordinal));
                        Step++;
                        return InputEvent.Press(ConsoleKey.UpArrow);
                    case 1:
                        _writer.Wait(rows => rows[0] == "[red]L4[/]" && rows[2].StartsWith("Paused", StringComparison.Ordinal));
                        SupplyNewLine = true;
                        Step++;
                        return null;
                    case 2:
                        // Wait for a source read to prove a new line arrived while the anchor remains in place.
                        if (Volatile.Read(ref Supplied) == 0) { cancellationToken.WaitHandle.WaitOne(timeout); return null; }
                        _writer.Wait(rows => rows[0] == "[red]L4[/]" && rows[2].StartsWith("Paused", StringComparison.Ordinal));
                        Step++;
                        return InputEvent.Press(ConsoleKey.End);
                    default:
                        _writer.Wait(rows => rows[0] == "[red]L6[/]" && rows[1] == "[red]L7[/]" && rows[2].StartsWith("Following", StringComparison.Ordinal));
                        Step++;
                        return InputEvent.Character('q');
                }
            }
            public void Dispose() { }
        }

        private sealed class ScreenWriter : ITerminalWriter {
            private readonly object _gate = new object();
            private readonly FakeVtScreen _screen = new FakeVtScreen(160, 4, DisplayWidth.Measure);
            internal bool Began;
            internal bool Ended;
            internal bool Disposed;
            internal string[] LastRows = Array.Empty<string>();
            public bool IsConsole => false;
            public void Write(string text) {
                lock (_gate) {
                    if (text.Contains("\u001b[?1049h")) { Began = true; }
                    if (text.Contains("\u001b[?1049l")) { Ended = true; }
                    _screen.Feed(text);
                    LastRows = Enumerable.Range(0, 4).Select(y => _screen.Row(y).TrimEnd()).ToArray();
                    Monitor.PulseAll(_gate);
                }
            }
            internal void Wait(Func<string[], bool> ready) {
                Stopwatch watch = Stopwatch.StartNew();
                lock (_gate) {
                    while (!ready(LastRows.Length == 4 ? LastRows : new[] { "", "", "", "" })) {
                        TimeSpan left = TimeSpan.FromSeconds(5) - watch.Elapsed;
                        Assert.True(left > TimeSpan.Zero, "Expected VT frame was not rendered.");
                        Monitor.Wait(_gate, left);
                    }
                }
            }
            public void Flush() { }
            public void Dispose() { Disposed = true; }
        }
    }
}
