using System.Globalization;
using ConsoleMonster.Live;

// Forty servers patched eight at a time. Three fail and one hangs past the four-second limit;
// the view lists the failures and the results keep them for the rest of the program.
IEnumerable<string> servers = Enumerable.Range(
    1,
    40
).Select(n => "web" + n.ToString(
        "00",
        CultureInfo.InvariantCulture
    ));
IReadOnlyList<RunResult<string, string>> results = await ConsoleRun.ForEachAsync(
    servers,
    async (server, cancel) => {
    // <more>
    int number = int.Parse(server.Substring(3), CultureInfo.InvariantCulture);
    await Task.Delay(500 + number * 137 % 900, cancel);
    if (number % 13 == 0) {
        throw new InvalidOperationException(server + " did not come back");
    }
    if (number == 22) {
        await Task.Delay(TimeSpan.FromSeconds(10), cancel);
    }
    // </more>
    return server + " patched";
},
    new RunOptions { ThrottleLimit = 8, Timeout = TimeSpan.FromSeconds(4), Title = "Patching" }
);
