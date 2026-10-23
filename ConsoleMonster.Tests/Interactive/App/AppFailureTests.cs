using System.IO;
using System.Threading.Tasks;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.TestHost;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Fatal renderer failures remain visible to application callers after cleanup.</summary>
    [Collection("Live sessions")]
    public sealed class AppFailureTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RendererFailure_PropagatesOriginalExceptionAfterCleanup(bool asynchronous) {
            using (RendererFailureProbe probe = new RendererFailureProbe()) {
                ConsoleAppOptions options = new ConsoleAppOptions {
                    Mouse = false,
                    Interactive = new InteractiveOptions {
                        Input = probe, Profile = probe.Profile, Mode = InteractionMode.Rich, Theme = Theme.Dark
                    },
                    Live = new LiveSessionOptions { Output = probe, CaptureConsole = false, TaskbarProgress = false }
                };
                TextInput query = new TextInput("query");
                IOException actual;
                if (asynchronous) {
                    actual = await Assert.ThrowsAsync<IOException>(() => ConsoleApp.RunAsync(query, options: options));
                } else {
                    actual = Assert.Throws<IOException>(() => ConsoleApp.Run(query, options: options));
                }
                Assert.Same(probe.Failure, actual);
                Assert.Contains(nameof(RendererFailureProbe.RenderFrame), actual.StackTrace);
                Assert.Contains("RenderLoopCore", actual.StackTrace);
                Assert.True(probe.Ended);
                Assert.False(ConsoleInput.IsAcquired);
                Assert.Equal("next", ConsoleApp.Run(new TextInput("query", "next"), options: AppTestHarness.Options("Escape", out _))["query"]);
            }
        }
    }
}
