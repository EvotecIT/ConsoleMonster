using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    [Collection("Live sessions")]
    public sealed class ScriptedReadingPauseTests {
        [Fact]
        public void SkippedScriptedWaitRecordsTheDisplayedPromptPauseBeforeItsAnswer() {
            var capture = new ScreenCapture(40, 12);
            var input = new ScriptedInput("wait:800 wait:400 y") { Paced = true, SkippedWait = capture.Hold };
            bool answer = new ConfirmPrompt("Continue?").Show(new InteractiveOptions {
                Capture = capture, Input = input, Mode = InteractionMode.Rich, Theme = Theme.Dark,
                Profile = TerminalProfile.Fixed(40, 12, ColorDepth.None)
            });
            Assert.True(answer);
            var hold = Assert.Single(capture.FrameHolds);
            Assert.Contains("Continue?", capture.Frames[hold.Key].ToPlainText());
            Assert.Equal(TimeSpan.FromMilliseconds(1200), hold.Value);
        }
    }
}
