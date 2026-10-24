using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class RecordingPromptOverlayTests {
        private static LiveSession Session(RecordingTarget target) => new LiveSession(new Slot("body"), new LiveSessionOptions {
            Target = target, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false,
            TaskbarProgress = false, StreamHeartbeat = TimeSpan.Zero
        });

        private static void Checkpoint(LiveSession session) {
            session.Start();
            session.Region("body").Set(TextBlock.Plain("Dashboard checkpoint background"));
            session.Delay(TimeSpan.Zero);
        }

        private static InteractiveOptions Options(RecordingTarget target, ScreenCapture? capture = null) => new InteractiveOptions {
            Target = target, Input = target.Input, Mode = InteractionMode.Rich, Capture = capture ?? target.Screens
        };

        [Theory]
        [InlineData("Enter", false)]
        [InlineData("", true)]
        public void PromptRecordsTheCheckpointBackdropAndReleasesItsPauseEvenWhenInputEnds(string keys, bool inputEnds) {
            RecordingTarget target = TestTerminal.Target(80, 24, keys: keys);
            target.SkipDelays = true;
            using (IInputSource input = target.Input!)
            using (LiveSession session = Session(target)) {
                Checkpoint(session);
                CellBuffer backdrop = Assert.IsType<CellBuffer>(session.Snapshot());
                Assert.Equal("Dashboard checkpoint background", backdrop.ToPlainText());
                Assert.Equal(80, backdrop.Width);
                Assert.Equal(1, backdrop.Height);

                var prompt = new ConfirmPrompt("Continue the recorded run?", true);
                if (inputEnds) {
                    Assert.Throws<InputClosedException>(() => prompt.Show(Options(target)));
                } else {
                    Assert.True(prompt.Show(Options(target)));
                }

                CellBuffer frame = Assert.IsType<CellBuffer>(target.Screens.LastFrame);
                Assert.Equal(80, frame.Width);
                Assert.Equal(24, frame.Height);
                Assert.Contains("Dashboard checkpoint background", frame.ToPlainText());
                Assert.Contains("Continue the recorded run?", frame.ToPlainText());
                Assert.False(session.IsPaused);
                Assert.False(ConsoleInput.IsAcquired);
                Assert.Equal(0, Assert.IsType<ScriptedInput>(input).Remaining);
                Assert.Null(session.Failure);
            }
        }

        [Fact]
        public void CancellationReleasesTheRecordingOverlayPauseAndInput() {
            RecordingTarget target = TestTerminal.Target(80, 24, keys: "Enter");
            target.SkipDelays = true;
            using (IInputSource input = target.Input!)
            using (LiveSession session = Session(target))
            using (var cancelled = new CancellationTokenSource()) {
                Checkpoint(session);
                cancelled.Cancel();
                Assert.ThrowsAny<OperationCanceledException>(() => new ConfirmPrompt("Continue?", true).Show(Options(target), cancelled.Token));
                Assert.False(session.IsPaused);
                Assert.False(ConsoleInput.IsAcquired);
                Assert.Equal(1, Assert.IsType<ScriptedInput>(input).Remaining);
            }
        }

        [Fact]
        public void ASeparateExplicitCaptureKeepsItsStandalonePromptSurface() {
            RecordingTarget target = TestTerminal.Target(80, 24, keys: "Enter");
            target.SkipDelays = true;
            var capture = new ScreenCapture(60, 16);
            using (IInputSource input = target.Input!)
            using (LiveSession session = Session(target)) {
                Checkpoint(session);
                Assert.True(new ConfirmPrompt("Separate capture?", true).Show(Options(target, capture)));
                CellBuffer frame = Assert.IsType<CellBuffer>(capture.LastFrame);
                Assert.Equal(60, frame.Width);
                Assert.Equal(16, frame.Height);
                Assert.Contains("Separate capture?", frame.ToPlainText());
                Assert.DoesNotContain("Dashboard checkpoint background", frame.ToPlainText());
                Assert.Single(target.Screens.Frames);
                Assert.False(session.IsPaused);
                Assert.False(ConsoleInput.IsAcquired);
            }
        }

        [Fact]
        public void ARecordingTargetCannotUseAnotherTargetsActiveDashboardAsItsBackdrop() {
            RecordingTarget dashboard = TestTerminal.Target(80, 24);
            dashboard.SkipDelays = true;
            RecordingTarget promptTarget = TestTerminal.Target(80, 24, keys: "Enter");
            promptTarget.SkipDelays = true;
            using (IInputSource input = promptTarget.Input!)
            using (LiveSession session = Session(dashboard)) {
                Checkpoint(session);
                Assert.True(new ConfirmPrompt("Other target?", true).Show(Options(promptTarget)));
                CellBuffer frame = Assert.IsType<CellBuffer>(promptTarget.Screens.LastFrame);
                Assert.Contains("Other target?", frame.ToPlainText());
                Assert.DoesNotContain("Dashboard checkpoint background", frame.ToPlainText());
                Assert.Single(dashboard.Screens.Frames);
                Assert.False(session.IsPaused);
                Assert.False(ConsoleInput.IsAcquired);
            }
        }
    }
}
