using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Output;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// The pattern for a library that writes console output: it takes an <see cref="ITerminalTarget"/> through its
    /// constructor (defaulting to the ambient <see cref="Terminal.Target"/>), writes only to that target, and never changes
    /// anything global. A script passes nothing; a test passes <c>TestTerminal.Target()</c>; a host passes its own target.
    /// </summary>
    public sealed class HealthReport {
        private readonly ITerminalTarget _target;

        /// <summary>Creates the report.</summary>
        /// <param name="target">Where to write, or null for <see cref="Terminal.Target"/>.</param>
        public HealthReport(ITerminalTarget? target = null) {
            _target = target ?? Terminal.Target;
        }

        /// <summary>Checks the servers under a status line, then prints a table.</summary>
        /// <param name="servers">The servers.</param>
        /// <param name="delay">A pause per server, to see the status line.</param>
        public void Run(string[] servers, TimeSpan delay) {
            Table table = new Table().AddColumn("Server").AddColumn("State").AddColumn("Latency");
            _target.Status("Checking servers", status => {
                for (int i = 0; i < servers.Length; i++) {
                    status.Update(Markup.Interpolate($"Checking [accent]{servers[i]}[/] [muted]{i + 1} of {servers.Length}[/]"));
                    Thread.Sleep(delay);
                    table.AddValues(servers[i], RichText.FromMarkup(i % 4 == 3 ? "[warn]slow[/]" : "[pass]ok[/]"), TimeSpan.FromMilliseconds(12 + i * 7));
                }
            });
            _target.Write(table);
            _target.WriteMarkupLine(Markup.Interpolate($"[muted]{servers.Length} servers checked[/]"));
        }

        /// <summary>Runs the report on the console, then into a log writer, and shows what the log received.</summary>
        public static void Show() {
            string[] servers = { "SRV01", "SRV02", "SRV03", "SRV04" };
            new HealthReport().Run(servers, TimeSpan.FromMilliseconds(400));

            StringWriter log = new StringWriter();
            new HealthReport(new WriterTarget(log, width: 60)).Run(servers, TimeSpan.Zero);
            Terminal.Write(new Layout.Panel(new TextBlock(Markup.Escape(log.ToString().TrimEnd())), "What a WriterTarget(log, width: 60) received"));
        }
    }
}
