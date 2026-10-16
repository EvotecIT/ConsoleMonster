using System;
using ConsoleMonster.Examples.Showcases;

namespace ConsoleMonster.Examples {
    /// <summary>
    /// Entry point for the examples and showcases. Usage: <c>ConsoleMonster.Examples &lt;name&gt;</c> where name is
    /// <c>query</c>, <c>gallery</c>, <c>pipeline</c>, <c>testimox</c>, <c>record</c>, <c>stress</c> or <c>widths</c>. Terminal detection can be
    /// overridden with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE and NO_COLOR.
    /// </summary>
    public static class Program {
        /// <summary>Runs an example by name.</summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Exit code.</returns>
        public static int Main(string[] args) {
            string name = args.Length > 0 ? args[0].ToLowerInvariant() : "query";
            switch (name) {
                case "query":
                    Terminal.Write(QueryResults.Build());
                    return 0;
                case "gallery":
                    Terminal.Write(WidgetGallery.Build());
                    return 0;
                case "pipeline":
                    BuildPipeline.Run();
                    return 0;
                case "record":
                    string target = args.Length > 1 ? args[1] : "testimox-run.jsonl";
                    int count = ParallelRun.Record(target);
                    Console.WriteLine("Wrote " + count + " events to " + System.IO.Path.GetFullPath(target));
                    return 0;
                case "testimox":
                    string? recording = null;
                    double speed = 1.5;
                    for (int i = 1; i < args.Length; i++) {
                        if (args[i] == "--speed" && i + 1 < args.Length) {
                            speed = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                        } else {
                            recording = args[i];
                        }
                    }
                    try {
                        ParallelRun.Replay(recording, speed);
                    } catch (OperationCanceledException) {
                        // Ctrl+C: the session has already restored the terminal and printed the summary.
                        Console.WriteLine("Replay interrupted by Ctrl+C.");
                        return 130;
                    }
                    return 0;
                case "stress":
                    int seconds = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 20;
                    string result = StressTest.Run(seconds);
                    if (args.Length > 2) {
                        System.IO.File.WriteAllText(args[2], result + Environment.NewLine);
                    }
                    return 0;
                case "widths":
                    GraphemeWidths.Run(args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 6);
                    return 0;
                case "tables":
                    int tableWidth = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
                    Capabilities.TerminalProfile tableProfile = tableWidth > 0 ? Terminal.Profile.WithSize(tableWidth, Terminal.Profile.Height) : Terminal.Profile;
                    Terminal.Write(TableShowcases.All(), tableProfile);
                    return 0;
                case "profile":
                    Console.WriteLine(Terminal.Profile.ToString());
                    foreach (string decision in Terminal.Profile.Decisions) {
                        Console.WriteLine("  " + decision);
                    }
                    return 0;
                default:
                    Console.Error.WriteLine("Unknown example '" + name + "'. Use query, gallery, pipeline, testimox, record, stress or widths.");
                    return 2;
            }
        }
    }
}
