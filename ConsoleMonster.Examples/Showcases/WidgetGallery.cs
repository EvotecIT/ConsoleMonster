using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using static ConsoleMonster.Ui;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Every v1 widget printed once, in one composition, for tier and theme comparisons. The widgets are configured with
    /// object initializers and the composition is written with the nested <see cref="Ui"/> factories, so it reads like the
    /// screen.
    /// </summary>
    public static class WidgetGallery {
        /// <summary>Builds the gallery.</summary>
        /// <returns>The widget tree.</returns>
        public static IWidget Build() {
            Logo logo = new Logo("ConsoleMonster", "[muted]zero-dependency console rendering for .NET and PowerShell[/]");

            ProgressBar build = new ProgressBar("Build", "p1") { Value = 64, Total = 100, LabelWidth = 8 };
            ProgressBar tests = new ProgressBar("Tests", "p2") { Value = 412, Total = 530, LabelWidth = 8 };
            ProgressBar docs = new ProgressBar("Docs", "p3") { Value = 100, Total = 100, LabelWidth = 8, Status = StatusKind.Pass };

            Lanes lanes = new Lanes();
            LanesHandle lh = lanes.Handle(new ImmediateDispatcher(lanes));
            lh.Add("dc1", "DC01.ad.adatum.com", 40);
            lh.Add("dc2", "DC02.ad.adatum.com", 40);
            lh.Add("dc3", "DC03.eu.ad.adatum.com", 40);
            lh.Add("dc4", "DC04.eu.ad.adatum.com", 40);
            for (int i = 0; i < 40; i++) {
                lh.Advance("dc1", i % 9 == 0 ? StatusKind.Warn : StatusKind.Pass, "LDAP signing required");
            }
            lh.Complete("dc1");
            for (int i = 0; i < 26; i++) {
                lh.Advance("dc2", i % 11 == 3 ? StatusKind.Fail : StatusKind.Pass, "Replication latency per partner");
            }
            for (int i = 0; i < 9; i++) {
                lh.Advance("dc3", StatusKind.Pass, "Kerberos AES-only encryption");
            }

            Phases phases = new Phases()
                .Set("Discover", StatusKind.Pass, null, "2.1s")
                .Set("Collect", StatusKind.Pass, null, "14s")
                .Set("Evaluate", StatusKind.Running, null, "64%")
                .Set("Report", StatusKind.Pending);

            StackedBar results = new StackedBar()
                .Add("Passed", 412, ThemeToken.Pass)
                .Add("Warnings", 51, ThemeToken.Warn)
                .Add("Failed", 23, ThemeToken.Fail)
                .Add("Skipped", 14, ThemeToken.Skip);

            Random random = new Random(42);
            Sparkline spark = new Sparkline("s1") { Label = "rules/s", Unit = "/s" };
            Sparkline area = new Sparkline("s2") { Height = 3, Unit = "/s" };
            LineChart chart = new LineChart { Height = 7 };
            chart.AddSeries("completed", ThemeToken.Accent);
            chart.AddSeries("failed", ThemeToken.Fail);
            Histogram histogram = new Histogram(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8, 16 }) { Height = 5, Unit = "s" };
            for (int i = 0; i < 120; i++) {
                double value = 18 + (8 * Math.Sin(i / 9.0)) + (random.NextDouble() * 6);
                spark.Add(value);
                area.Add(value);
                chart.Add("completed", value);
                chart.Add("failed", Math.Max(0, (value / 6) + (random.NextDouble() * 2) - 2));
                histogram.Add(Math.Exp((random.NextDouble() * 4) - 2.2));
            }

            BarChart categories = new BarChart("categories") { BarToken = ThemeToken.Fail }
                .Add("Domain controllers", 14)
                .Add("Group Policy", 9)
                .Add("Kerberos", 6)
                .Add("DNS", 4)
                .Add("PKI", 2);

            LogRegion log = new LogRegion { PreferredHeight = 5 };
            log.Write("Discovered 4 domain controllers in 2 domains", "info", TimeSpan.FromSeconds(2));
            log.Write("DC02: replication partner DC05 last success 3h ago", "warn", TimeSpan.FromSeconds(31));
            log.Write("DC02: USN rollback check failed (event 2095)", "err", TimeSpan.FromSeconds(44));
            log.Write("Collected 186 rule inputs from DC01", "info", TimeSpan.FromSeconds(52));
            log.Write("Using cached schema snapshot", "verbose", TimeSpan.FromSeconds(53));

            Spinner resolving = new Spinner("Resolving DNS records", "spin1") { Detail = "zone 4 of 9" };
            Spinner synced = new Spinner("Schema snapshot", "spin2") { Status = StatusKind.Pass, Detail = "cached" };
            Sparkline badgeBase = new Sparkline("s3") { Height = 2 };
            for (int i = 0; i < 40; i++) {
                badgeBase.Add(10 + (6 * Math.Sin(i / 4.0)));
            }
            Stack overlay = new Stack(badgeBase, new Align(new TextBlock("[pass] LIVE [/]"), HorizontalAlignment.Right));
            return Rows(
                logo,
                Panel("Progress", Rows(build, tests, docs)),
                Panel("Lanes", lanes).WithCaption("2 running"),
                Panel("Phases", phases),
                Columns(
                    Panel("Stacked bar", results),
                    Panel("Bar chart", categories).WithCaption("failures")),
                Columns(
                    Panel("Sparkline", Rows(spark, area).WithGap(1)),
                    Panel("Line chart", chart).WithCaption("braille"),
                    Panel("Histogram", histogram)),
                Columns(
                    Panel("Spinner", Rows(resolving, synced)),
                    Panel("Align", new Align(new TextBlock("[muted]centred with Align[/]"))),
                    Panel("Stack", overlay).WithCaption("badge over a chart")),
                Panel("Log region", log));
        }
    }
}
