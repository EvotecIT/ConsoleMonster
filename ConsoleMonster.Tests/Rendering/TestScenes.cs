using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Deterministic widget compositions used by golden snapshot tests.</summary>
    internal static class TestScenes {
        public static IWidget Report() {
            Table table = new Table { Title = "[bold]Records[/]", Expand = true };
            table.AddColumn(new TableColumn("Name") { Wrap = false });
            table.AddColumn("Type");
            table.AddColumn("TTL");
            table.AddColumn(new TableColumn("Data") { Flexible = true });
            table.AddPlainRow("evotec.xyz", "A", "300", "104.21.48.12");
            table.AddPlainRow("evotec.xyz", "MX", "3600", "10 evotec-xyz.mail.protection.outlook.com");
            table.AddPlainRow("evotec.xyz", "TXT", "3600", "v=spf1 include:spf.protection.outlook.com include:_spf.google.com -all");
            table.AddRow("_dmarc.evotec.xyz", "TXT", "3600", TableCell.ForStatus(StatusKind.Pass, "p=reject"));

            Tree tree = new Tree("root");
            TreeNode child = tree.Add("xyz.", null, "2 servers");
            child.Add("dana.ns.cloudflare.com", StatusKind.Pass, "12 ms");
            child.Add("kirk.ns.cloudflare.com", StatusKind.Fail, "timeout");

            KeyValueList kv = new KeyValueList()
                .Add("Domain", "evotec.xyz")
                .Add("DNSSEC", "not signed", StatusKind.Warn);

            StatusList checks = new StatusList()
                .Add("SPF", StatusKind.Pass, "9 lookups")
                .Add("MTA-STS", StatusKind.Warn, "missing")
                .Add("BIMI", StatusKind.Skip);

            return new Rows(
                new Rule("Report"),
                kv,
                table,
                new Columns(new Panel(tree, "Tree"), new Panel(checks, "Checks") { HeaderRight = "3 items" })) { Gap = 1 };
        }

        public static IWidget Live() {
            Lanes lanes = new Lanes();
            LanesHandle h = lanes.Handle(new ImmediateDispatcher(lanes));
            h.Add("a", "DC01.ad.adatum.com", 10);
            h.Add("b", "DC02.ad.adatum.com", 10);
            h.Add("c", "DC03.ad.adatum.com", 10);
            for (int i = 0; i < 10; i++) {
                h.Advance("a", i == 3 ? StatusKind.Warn : StatusKind.Pass, "LDAP signing");
            }
            h.Complete("a");
            for (int i = 0; i < 5; i++) {
                h.Advance("b", i == 1 ? StatusKind.Fail : StatusKind.Pass, "Replication");
            }

            Phases phases = new Phases().Set("Discover", StatusKind.Pass, null, "2.1s").Set("Evaluate", StatusKind.Running, null, "50%").Set("Report");
            ProgressBar progress = new ProgressBar("Build") { Value = 37, Total = 80, LabelWidth = 6 };
            StackedBar stacked = new StackedBar().Add("Passed", 70, ThemeToken.Pass).Add("Failed", 20, ThemeToken.Fail).Add("Skipped", 10, ThemeToken.Skip);
            BarChart bars = new BarChart().Add("Kerberos", 6).Add("DNS", 3).Add("PKI", 1);
            Sparkline spark = new Sparkline { Title = "rate", Height = 1 };
            Sparkline area = new Sparkline("area") { Height = 3 };
            LineChart chart = new LineChart { Height = 6 };
            Histogram histogram = new Histogram(new double[] { 0, 1, 2, 4, 8 }) { Height = 4 };
            for (int i = 0; i < 60; i++) {
                double v = 10 + (8 * Math.Sin(i / 6.0));
                spark.Add(v);
                area.Add(v);
                chart.Add("a", v);
                chart.Add("b", 20 - v);
                histogram.Add(i % 9);
            }
            LogRegion log = new LogRegion { PreferredHeight = 3 };
            log.Write("Started", "info", TimeSpan.FromSeconds(1));
            log.Write("Slow responder", "warn", TimeSpan.FromSeconds(12));
            log.Write("Check failed", "err", TimeSpan.FromSeconds(31));

            return new Rows(
                new Logo("CM", "tagline"),
                phases,
                progress,
                new Panel(lanes, "Lanes"),
                new Columns(new Panel(stacked, "Stacked"), new Panel(bars, "Bars")),
                new Columns(new Panel(new Rows(spark, area), "Spark"), new Panel(chart, "Chart"), new Panel(histogram, "Histogram")),
                new Panel(log, "Log"));
        }
    }
}
