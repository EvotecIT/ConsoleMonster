using System;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class TailNavigationContracts {
        [Theory]
        [InlineData(60, false)]
        [InlineData(120, false)]
        [InlineData(60, true)]
        [InlineData(120, true)]
        public void NavigationWaitsForEachActualRenderedState(int width, bool light) {
            var theme = light ? Theme.Light : Theme.Dark;
            var frames = TailsPage.ReviewStates(width, theme);
            var profile = TerminalProfile.Fixed(width, 12);
            var target = new RecordingTarget(profile) { Theme = theme };
            using var input = new TailsPage.NavigationInput(target, width);
            using var output = new VtOutput(target.Writer!, profile, ownsWriter: false);
            output.BeginLive();
            try {
                Assert.Null(input.Read(TimeSpan.Zero, CancellationToken.None));
                output.RenderFrame(frames["source-days"]);
                Assert.True(input.Read(TimeSpan.Zero, CancellationToken.None)!.Is(ConsoleKey.PageUp));

                // Re-reading the existing frame cannot advance the script before the next state is drawn.
                Assert.Null(input.Read(TimeSpan.Zero, CancellationToken.None));
                output.RenderFrame(frames["paused-new-lines"]);
                Assert.True(input.Read(TimeSpan.Zero, CancellationToken.None)!.Is(ConsoleKey.End));
                Assert.Null(input.Read(TimeSpan.Zero, CancellationToken.None));
                output.RenderFrame(frames["source-days"]);
                Assert.True(input.Read(TimeSpan.Zero, CancellationToken.None)!.IsChar('q'));
            } finally {
                output.EndLive();
            }
        }
    }
}
