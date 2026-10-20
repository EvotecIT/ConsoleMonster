using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Forty servers patched eight at a time with <see cref="ConsoleRun.ForEachAsync{TItem, TResult}"/>: the live view shows
    /// the summary bar, one cell per server, the running items and the failures. Three servers fail and one runs past the
    /// four-second limit. The C# twin of the site's <c>Website/examples/demos/parallel.ps1</c>; the data is invented.
    /// </summary>
    public static class ParallelPatch {
        /// <summary>Runs the patch wave and returns the number of failed servers.</summary>
        /// <returns>The failures.</returns>
        public static async Task<int> RunAsync() {
            // <example>
            IEnumerable<string> servers = Enumerable.Range(1, 40).Select(n => "web" + n.ToString("00", CultureInfo.InvariantCulture));
            IReadOnlyList<RunResult<string, string>> results = await ConsoleRun.ForEachAsync(servers, async (server, cancel) => {
                // <more>
                int number = int.Parse(server.Substring(3), CultureInfo.InvariantCulture);
                await Task.Delay(500 + number * 137 % 900, cancel);
                if (number % 13 == 0) {
                    throw new InvalidOperationException(server + " did not come back after the reboot");
                }
                if (number == 22) {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancel);
                }
                // </more>
                return server + " patched";
            }, new RunOptions { ThrottleLimit = 8, Timeout = TimeSpan.FromSeconds(4), Title = "Patching the web farm" });
            // </example>
            return results.Count(r => !r.Succeeded);
        }
    }
}
