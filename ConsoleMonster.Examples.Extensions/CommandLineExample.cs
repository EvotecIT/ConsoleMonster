using System.CommandLine;
using System.Threading;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Extensions {
    /// <summary>
    /// A command-line tool that parses with System.CommandLine and writes its output with ConsoleMonster: a live status
    /// list while it works, then a table. Run it as <c>cli --servers 4</c>; <c>cli --help</c> prints System.CommandLine's help.
    /// </summary>
    public static class CommandLineExample {
        /// <summary>Parses the arguments and runs the command.</summary>
        /// <param name="args">The arguments after <c>cli</c>.</param>
        /// <returns>The exit code.</returns>
        public static int Run(string[] args) {
            // <example>
            Option<int> servers = new Option<int>("--servers") {
                Description = "How many servers to check.",
                DefaultValueFactory = _ => 3
            };
            RootCommand root = new RootCommand("Checks a few servers and prints what it found.");
            root.Options.Add(servers);
            root.SetAction(parseResult => {
                int count = parseResult.GetValue(servers);
                string[] names = new string[count];
                for (int i = 0; i < count; i++) {
                    names[i] = "SRV" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                }

                // While it works: a live status list, drawn in place below the prompt.
                StatusList checks = new StatusList("checks");
                foreach (string name in names) {
                    checks.Set(name, name, StatusKind.Pending);
                }
                using (LiveSession session = new LiveSession(checks, new LiveSessionOptions { Mode = LiveMode.Inline })) {
                    session.Run(s => {
                        StatusListHandle handle = checks.Handle(s);
                        foreach (string name in names) {
                            handle.Set(name, StatusKind.Running);
                            Thread.Sleep(250);
                            handle.Set(name, StatusKind.Pass);
                        }
                    });
                }

                // The result: a table that fits the terminal's width.
                Table table = new Table();
                table.AddColumn(new TableColumn("Server") { IsKey = true });
                table.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status });
                table.AddColumn(new TableColumn("Latency") { Presenter = new NumberPresenter("ms") });
                for (int i = 0; i < names.Length; i++) {
                    table.AddValues(names[i], StatusKind.Pass, 12 + i * 7);
                }
                Terminal.Write(table);
                return 0;
            });
            return root.Parse(args).Invoke();
            // </example>
        }
    }
}
