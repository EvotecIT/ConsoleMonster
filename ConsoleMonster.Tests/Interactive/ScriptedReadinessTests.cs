using System;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    [Collection("Live sessions")]
    public sealed class ScriptedReadinessTests {
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
