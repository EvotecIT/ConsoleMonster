using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed partial class SessionProcessTests {
        [WindowsFact]
        public Task NativeModes_FreshAndCachedProfilesRestoreAfterNormalCancellationAndSourceFailure() => NativeModeContract("repeated");

        [WindowsFact]
        public Task NativeModes_BorrowedOutputsAndWriterFailuresRestoreWithoutDisposingBorrowedWriters() => NativeModeContract("outputs");

        [WindowsFact]
        public Task NativeModes_NonConsoleWritersNeverChangeTheNativeConsoleMode() => NativeModeContract("recording");

        private static async Task NativeModeContract(string scenario) {
            string receipt = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            try {
#if NETFRAMEWORK
                string assembly = new Uri(typeof(TestHost.Program).Assembly.CodeBase!).LocalPath;
                string command = Quote(assembly);
#else
                string assembly = typeof(TestHost.Program).Assembly.Location;
                string runtime = Path.ChangeExtension(typeof(SessionProcessTests).Assembly.Location, ".runtimeconfig.json");
                string command = "dotnet exec --runtimeconfig " + Quote(runtime) + " " + Quote(assembly);
#endif
                var result = await Run("host " + command + " native-modes " + Quote(receipt) + " " + scenario);
                string actual = File.Exists(receipt) ? File.ReadAllText(receipt) : "The native mode child produced no receipt.";
                Assert.True(result.Exit == 0, actual + result.Output + result.Error);
                Assert.StartsWith("NATIVE_MODES_OK\n" + scenario + "\n", actual);
                Assert.Contains("mode=exactly-restored", actual);
            } finally { if (File.Exists(receipt)) { File.Delete(receipt); } }
        }
    }
}
