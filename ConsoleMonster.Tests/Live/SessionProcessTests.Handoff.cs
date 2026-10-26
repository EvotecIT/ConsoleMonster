using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed partial class SessionProcessTests {
        [WindowsFact]
        public async Task ActualConPtyHandoffDrainsFinalOutputAndRestoresAfterCancellation() {
            string receipt = Path.Combine(Path.GetTempPath(), "ConsoleMonster-Handoff-" + Guid.NewGuid().ToString("N") + ".txt");
            try {
#if NETFRAMEWORK
                string command = Quote(new Uri(typeof(TestHost.Program).Assembly.CodeBase!).LocalPath);
#else
                string runtime = Path.ChangeExtension(typeof(SessionProcessTests).Assembly.Location, ".runtimeconfig.json");
                string command = "dotnet exec --runtimeconfig " + Quote(runtime) + " " + Quote(typeof(TestHost.Program).Assembly.Location);
#endif
                var result = await Run("host " + command + " handoff-windows " + Quote(receipt));
                string actual = File.Exists(receipt) ? File.ReadAllText(receipt) : "No ConPTY handoff receipt.";
                Assert.True(result.Exit == 0, actual + result.Output + result.Error);
                Assert.StartsWith("CONPTY_HANDOFF_OK\n", actual);
                Assert.Contains("final-output=retained", actual);
                Assert.Contains("cancel=restored", actual);
            } finally { if (File.Exists(receipt)) { File.Delete(receipt); } }
        }
    }
}
