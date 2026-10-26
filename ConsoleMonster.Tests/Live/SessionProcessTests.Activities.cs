using System.Threading.Tasks;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed partial class SessionProcessTests {
        [Fact]
        public async Task StringKeyActivityIdsAndSpinnerPhasesAreIdenticalInIndependentProcesses() {
            var first = await Run("activity-frame");
            var second = await Run("activity-frame");
            Assert.True(first.Exit == 0, first.Error);
            Assert.True(second.Exit == 0, second.Error);
            Assert.Contains("collecting", first.Output);
            Assert.Equal(first.Output, second.Output);
        }
    }
}
