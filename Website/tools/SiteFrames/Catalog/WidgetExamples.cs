using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace SiteFrames.Catalog {
    /// <summary>
    /// The widget gallery. Each example's code between the <c>// example</c> markers is shown on the site as its C# source,
    /// and the PowerShell script with the same id under <c>Website/examples/widgets/</c> is shown beside it. The tool renders
    /// what the C# wrote at every colour and glyph tier, runs the script on both PowerShell hosts and compares the cells.
    /// </summary>
    internal static class WidgetExamples {
        public static IReadOnlyList<Example> All() => new List<Example> {
            new Example("markup", "Text and markup", "text", "Spectre-style markup: theme roles, colours and decorations, reduced for each tier.", 60, Markup_),
            new Example("rule", "Rule", "text", "A horizontal line with an optional title, the separator inside panels and reports.", 60, Rule_),
            new Example("panel", "Panel", "layout", "A border around any widget, with a title, a caption on the right and a footer.", 60, Panel_),
            new Example("columns", "Rows and columns", "layout", "Stack widgets vertically or side by side; columns share the width by weight.", 60, Columns_),
            new Example("grid", "Grid", "layout", "Equal cells in a fixed number of columns, filled left to right.", 60, Grid_),
            new Example("align", "Padding and align", "layout", "Space around a widget, and placement inside the space it is given.", 60, Align_),
            new Example("stack", "Stack", "layout", "Layers drawn on top of each other: a badge over a chart.", 60, Stack_),
            new Example("keyvalue", "Key/value list", "status", "Aligned facts with optional status marks, as a summary beside a run.", 60, KeyValue_),
            new Example("statuslist", "Status list", "status", "Steps with a status symbol and a detail on the right.", 60, StatusList_),
            new Example("tree", "Tree", "status", "Nested items with guides, status symbols and notes.", 60, Tree_),
            new Example("progress", "Progress", "status", "Labelled bars with a percentage and a count, in line or block shape.", 60, Progress_),
            new Example("phases", "Phases", "status", "A pipeline of stages: done, running and waiting.", 60, Phases_),
            new Example("spinner", "Spinner", "status", "An activity with a detail, or its final state once it has finished.", 60, Spinner_),
            new Example("log", "Log region", "status", "Timed log lines with levels; in a live session it collects Console and PowerShell output.", 60, Log_),
            new Example("bar", "Bar chart", "charts", "Labelled bars scaled to the largest value, with the values on the right.", 60, Bar_),
            new Example("stacked", "Stacked bar", "charts", "Parts of a whole in one bar, with a legend of counts and shares.", 60, Stacked_),
            new Example("sparkline", "Sparkline", "charts", "A compact trend in one or more rows, with the latest value.", 60, Sparkline_),
            new Example("histogram", "Histogram", "charts", "Values counted into buckets with labelled edges.", 60, Histogram_),
            new Example("line", "Line chart", "charts", "Braille line series with an axis and a legend.", 60, Line_),
            new Example("scatter", "Scatter chart", "charts", "The same braille canvas as points instead of lines.", 60, Scatter_),
            new Example("logo", "Logo", "text", "Large pixel lettering with a gradient and a tagline, for the top of a tool.", 60, Logo_)
        };

        private static void Markup_() {
            // <example>
            Terminal.Write(new TextBlock(
                "[bold]Deployed[/] in [accent]48 s[/]: " +
                "[pass]√ 12 passed[/] [warn]▲ 2 warned[/] [fail]✗ 1 failed[/]"));
            Terminal.Write(new TextBlock(
                "[italic]Decorations:[/] [bold]bold[/] [dim]dim[/] [underline]underline[/] " +
                "[strikethrough]struck[/] [invert] invert [/]"));
            // </example>
        }

        private static void Rule_() {
            // <example>
            Terminal.Write(new Rule("[bold]Replication[/]"));
            Terminal.Write(new Rule());
            // </example>
        }

        private static void Panel_() {
            // <example>
            TextBlock body = new TextBlock("Four controllers answered. [warn]DC02[/] lags by 42 minutes.");
            Panel panel = new Panel(body, "[bold]Forest[/]") {
                Caption = "ad.adatum.com",
                Footer = "[muted]checked 14:30[/]"
            };
            Terminal.Write(panel);
            // </example>
        }

        private static void Columns_() {
            // <example>
            Panel results = new Panel(new TextBlock("[pass]√[/] 412 passed\n[warn]▲[/] 51 warnings"), "Results");
            Panel problems = new Panel(new TextBlock("[fail]✗[/] 23 failed\n[muted]-[/] 14 skipped"), "Problems");
            Rule footer = new Rule("[muted]2 domains, 10 controllers[/]");
            Terminal.Write(new Rows(new Columns(results, problems), footer));
            // </example>
        }

        private static void Grid_() {
            // <example>
            Grid grid = new Grid(3,
                new Panel(new TextBlock("[pass]√ healthy[/]"), "DC01"),
                new Panel(new TextBlock("[warn]▲ lagging[/]"), "DC02"),
                new Panel(new TextBlock("[pass]√ healthy[/]"), "DC03"),
                new Panel(new TextBlock("[fail]✗ offline[/]"), "DC04"),
                new Panel(new TextBlock("[pass]√ healthy[/]"), "DC05"));
            Terminal.Write(grid);
            // </example>
        }

        private static void Align_() {
            // <example>
            Panel left = new Panel(new Align(new TextBlock("[bold]centred[/]")), "Align");
            Panel right = new Panel(new Padding(new TextBlock("two cells of padding"), 2, 1, 2, 1), "Padding");
            Terminal.Write(new Columns(left, right));
            // </example>
        }

        private static void Stack_() {
            // <example>
            Sparkline load = new Sparkline { Height = 3, ShowLatest = false };
            load.AddRange(new double[] { 12, 14, 13, 18, 22, 26, 25, 31, 36, 34, 40, 44 });
            load.AddRange(new double[] { 41, 47, 52, 49, 55, 61, 58, 64, 70, 66, 73, 79 });
            Align badge = new Align(new TextBlock("[pass] LIVE [/]"), HorizontalAlignment.Right);
            Stack stack = new Stack(load, badge);
            Terminal.Write(new Panel(stack, "Stack") { Caption = "badge over a chart" });
            // </example>
        }

        private static void KeyValue_() {
            // <example>
            KeyValueList facts = new KeyValueList()
                .Add("Module", "[bold]ConsoleMonster[/]")
                .Add("Version", "0.0.4 [muted]→[/] [bold]2.0.0[/]")
                .Add("Frameworks", "netstandard2.0, net472, net8.0")
                .Add("Signing", "skipped", StatusKind.Skip)
                .Add("Tests", "1 skipped on PowerShell 7", StatusKind.Warn);
            Terminal.Write(facts);
            // </example>
        }

        private static void StatusList_() {
            // <example>
            StatusList steps = new StatusList()
                .Add("Restore NuGet packages", StatusKind.Pass, "1.6 s")
                .Add("Compile ConsoleMonster", StatusKind.Pass, "4.7 s")
                .Add("Run Pester on PowerShell 7", StatusKind.Warn, "1 skipped")
                .Add("Sign module files", StatusKind.Skip, "skipped")
                .Add("Pack ConsoleMonster.2.0.0.nupkg", StatusKind.Running)
                .Add("Publish to PowerShell Gallery", StatusKind.Pending);
            Terminal.Write(steps);
            // </example>
        }

        private static void Tree_() {
            // <example>
            Tree tree = new Tree("[bold]evotec.xyz[/]");
            TreeNode mx = tree.Add("MX", StatusKind.Pass, "2 records");
            mx.Add("evotec-xyz.mail.protection.outlook.com", null, "priority 0");
            TreeNode spf = tree.Add("SPF", StatusKind.Warn, "9 of 10 DNS lookups");
            spf.Add("include:spf.protection.outlook.com");
            spf.Add("include:_spf.salesforce.com");
            tree.Add("DMARC", StatusKind.Fail, "p=none");
            tree.Add("DKIM", StatusKind.Pass, "selector1, selector2");
            Terminal.Write(tree);
            // </example>
        }

        private static void Progress_() {
            // <example>
            Terminal.Write(new Rows(
                new ProgressBar("Build") { Value = 64, Total = 100 },
                new ProgressBar("Tests") { Value = 412, Total = 530 },
                new ProgressBar("Docs") { Value = 100, Total = 100, Status = StatusKind.Pass },
                new ProgressBar("Upload") { Value = 3, Total = 8, Shape = BarShape.Block }));
            // </example>
        }

        private static void Phases_() {
            // <example>
            Phases phases = new Phases()
                .Set("Discover", StatusKind.Pass, null, "2.1s")
                .Set("Collect", StatusKind.Pass, null, "14s")
                .Set("Evaluate", StatusKind.Running, null, "64%")
                .Set("Report", StatusKind.Pending);
            Terminal.Write(phases);
            // </example>
        }

        private static void Spinner_() {
            // <example>
            Terminal.Write(new Rows(
                new Spinner("Resolving DNS records") { Detail = "zone 4 of 9" },
                new Spinner("Schema snapshot") { Status = StatusKind.Pass, Detail = "cached" },
                new Spinner("Certificate chain") { Status = StatusKind.Fail, Detail = "expired 3 days ago" }));
            // </example>
        }

        private static void Log_() {
            // <example>
            LogRegion log = new LogRegion { PreferredHeight = 5 };
            log.Write("Discovered 4 domain controllers in 2 domains", "info", TimeSpan.FromSeconds(2));
            log.Write("DC02: replication partner DC05 last success 3h ago", "warn", TimeSpan.FromSeconds(31));
            log.Write("DC02: USN rollback check failed (event 2095)", "err", TimeSpan.FromSeconds(44));
            log.Write("Collected 186 rule inputs from DC01", "info", TimeSpan.FromSeconds(52));
            log.Write("Using cached schema snapshot", "verbose", TimeSpan.FromSeconds(53));
            Terminal.Write(log);
            // </example>
        }

        private static void Bar_() {
            // <example>
            BarChart failures = new BarChart { BarToken = ThemeToken.Fail }
                .Add("Domain controllers", 14)
                .Add("Group Policy", 9)
                .Add("Kerberos", 6)
                .Add("DNS", 4)
                .Add("PKI", 2);
            Terminal.Write(failures);
            // </example>
        }

        private static void Stacked_() {
            // <example>
            StackedBar results = new StackedBar()
                .Add("Passed", 412, ThemeToken.Pass)
                .Add("Warnings", 51, ThemeToken.Warn)
                .Add("Failed", 23, ThemeToken.Fail)
                .Add("Skipped", 14, ThemeToken.Skip);
            Terminal.Write(results);
            // </example>
        }

        private static void Sparkline_() {
            // <example>
            double[] rate = { 18, 21, 19, 24, 27, 26, 31, 29, 34, 38, 36, 41, 39, 44, 47, 45, 50, 48, 53, 57, 54, 59, 62, 60 };
            Sparkline compact = new Sparkline { Label = "rules/s", Unit = "/s" };
            Sparkline tall = new Sparkline { Height = 3, Unit = "/s" };
            compact.AddRange(rate);
            tall.AddRange(rate);
            Terminal.Write(new Rows(compact, tall) { Gap = 1 });
            // </example>
        }

        private static void Histogram_() {
            // <example>
            Histogram durations = new Histogram(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 }) { Height = 5, Unit = "s" };
            foreach (double seconds in new[] { 0.1, 0.2, 0.3, 0.3, 0.4, 0.6, 0.7, 0.7, 0.8, 0.9, 1.2, 1.4, 1.5, 1.9, 2.4, 3.1, 5.2 }) {
                durations.Add(seconds);
            }
            Terminal.Write(durations);
            // </example>
        }

        private static void Line_() {
            // <example>
            LineChart chart = new LineChart { Height = 7 };
            chart.AddSeries("completed", ThemeToken.Accent);
            chart.AddSeries("failed", ThemeToken.Fail);
            for (int i = 0; i < 48; i++) {
                chart.Add("completed", 20 + (9 * Math.Sin(i / 7.0)) + (i % 3));
                chart.Add("failed", 3 + (2 * Math.Cos(i / 5.0)));
            }
            Terminal.Write(chart);
            // </example>
        }

        private static void Scatter_() {
            // <example>
            LineChart chart = new LineChart { Height = 7, Scatter = true, Unit = "ms" };
            chart.AddSeries("latency", ThemeToken.Accent2);
            for (int i = 0; i < 60; i++) {
                chart.Add("latency", 40 + ((i * 37) % 23) + (i % 7 == 0 ? 30 : 0));
            }
            Terminal.Write(chart);
            // </example>
        }

        private static void Logo_() {
            // <example>
            Terminal.Write(new Logo("Monster", "[muted]zero-dependency console rendering[/]"));
            // </example>
        }
    }

    /// <summary>One gallery entry.</summary>
    internal sealed class Example {
        public Example(string id, string title, string group, string summary, int width, Action code) {
            Id = id;
            Title = title;
            Group = group;
            Summary = summary;
            Width = width;
            Code = code;
        }

        public string Id { get; }

        public string Title { get; }

        public string Group { get; }

        public string Summary { get; }

        public int Width { get; }

        public Action Code { get; }

        /// <summary>The method name holding the example's code, so its source can be found.</summary>
        public string Method => Code.Method.Name;
    }
}
