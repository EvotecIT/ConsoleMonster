using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    [Collection("Live sessions")]
    public sealed class ScriptedReadinessTests {
        [Fact]
        public async Task ReadinessObservesCompletedCopiesDuringConcurrentPresentation() {
            var capture = new ScreenCapture(16, 3);
            var frame = new CellBuffer(16, 3);
            void Present(string text) {
                for (int row = 0; row < frame.Height; row++) { frame.Write(0, row, text, Style.Plain); }
                capture.Surface.Present(frame, null);
                frame.Clear(); // The renderer can reuse its buffer as soon as Present returns.
            }
            Present("initial");
            using var input = new ScriptedInput(string.Empty);
            input.WaitForOutput(capture, "Ready", TimeSpan.FromSeconds(30));
            input.Append("Enter");
            using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Task renderer = Task.Factory.StartNew(() => {
                for (int index = 0; index < 2000; index++) {
                    watchdog.Token.ThrowIfCancellationRequested();
                    Present(index % 2 == 0 ? "first" : "second");
                }
                Present("Ready");
            }, watchdog.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            try {
                InputEvent? next;
                do {
                    CellBuffer? observed = capture.LastFrame;
                    Assert.NotNull(observed);
                    string row = observed!.GetRowText(0);
                    Assert.Contains(row.TrimEnd(), new[] { "initial", "first", "second", "Ready" });
                    Assert.Equal(row, observed.GetRowText(1));
                    Assert.Equal(row, observed.GetRowText(2));
                    next = input.Read(TimeSpan.FromMilliseconds(1), watchdog.Token);
                } while (next == null);
                Assert.True(next.Is(ConsoleKey.Enter));
                await renderer;
                Assert.Same(capture.Frames[capture.Frames.Count - 1], capture.LastFrame);
                Assert.Contains("Ready", capture.LastFrame!.ToPlainText());
            } finally {
                watchdog.Cancel();
                try { await renderer; } catch (OperationCanceledException) when (watchdog.IsCancellationRequested) { }
            }
        }

        [Fact]
        public void InputWaitsForTheRealPromptWithoutBlockingItsRepaintEvenWhenWaitsAreSkipped() {
            var capture = new ScreenCapture(40, 12);
            var input = new ScriptedInput(string.Empty);
            input.WaitForOutput(capture, "Continue?", TimeSpan.FromSeconds(5));
            input.Append("y");
            Assert.Null(input.Read(TimeSpan.FromMilliseconds(1), CancellationToken.None));
            Assert.True(new ConfirmPrompt("Continue?").Show(new InteractiveOptions {
                Capture = capture, Input = input, Mode = InteractionMode.Rich, Theme = Theme.Dark,
                Profile = TerminalProfile.Fixed(40, 12, ColorDepth.None)
            }));
            Assert.Contains("Continue?", capture.LastFrame!.ToPlainText());
        }

        [Fact]
        public void OutputThatArrivesAfterTheDeadlineCannotReleaseTheQueuedKey() {
            var capture = new ScreenCapture(40, 12);
            var input = new ScriptedInput(string.Empty);
            input.WaitForOutput(capture, "Continue?", TimeSpan.FromMilliseconds(20));
            input.Append("y");
            Assert.Null(input.Read(TimeSpan.FromMilliseconds(1), CancellationToken.None));
            Thread.Sleep(100);
            Assert.Throws<TimeoutException>(() => new ConfirmPrompt("Continue?").Show(new InteractiveOptions {
                Capture = capture, Input = input, Mode = InteractionMode.Rich, Theme = Theme.Dark,
                Profile = TerminalProfile.Fixed(40, 12, ColorDepth.None)
            }));
            Assert.Contains("Continue?", capture.LastFrame!.ToPlainText());
        }

        [Fact]
        public void MissingReadyOutputFailsAtItsDeadlineInsteadOfConsumingTheNextKey() {
            var input = new ScriptedInput(string.Empty);
            input.WaitForOutput(new ScreenCapture(40, 12), "Ready", TimeSpan.FromMilliseconds(20));
            input.Append("Enter");
            var deadline = Stopwatch.StartNew();
            Assert.Throws<TimeoutException>(() => {
                while (deadline.Elapsed < TimeSpan.FromSeconds(2)) {
                    Assert.Null(input.Read(TimeSpan.FromMilliseconds(5), CancellationToken.None));
                }
            });
        }
    }
}
