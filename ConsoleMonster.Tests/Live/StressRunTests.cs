using System.Text.RegularExpressions;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class StressRunTests {
        [Fact]
        public void StressRun_VerifiesZeroDropsAndCapturedLineIdentitiesAfterDrain() {
            RecordingTarget target = new RecordingTarget(TerminalProfile.Fixed(120, 40, ColorDepth.None));
            using (Terminal.Use(target)) {
                string result = StressTest.Run(1);
                Assert.Contains("dropped=0 errors=0", result);
                Assert.Contains("missing=0", result);
                Match written = Regex.Match(result, @" captured=(\d+)");
                Match verified = Regex.Match(result, @" verifiedCaptured=(\d+)");
                Assert.True(written.Success);
                Assert.True(verified.Success);
                Assert.Equal(written.Groups[1].Value, verified.Groups[1].Value);
            }
        }
    }
}
