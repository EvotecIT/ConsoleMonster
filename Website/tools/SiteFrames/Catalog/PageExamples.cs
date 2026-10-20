using System;
using System.Collections.Generic;
using ConsoleMonster.Data;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace SiteFrames.Catalog {
    /// <summary>
    /// Examples written for the home, tables and PowerShell pages. As in the gallery, the lines between the example markers
    /// are what the site shows, and scripts under <c>Website/examples/home/</c> are their PowerShell twins.
    /// </summary>
    internal static class PageExamples {
        public static void Controllers() {
            // <example>
            Table table = new Table { Title = "[bold]Domain controllers[/]" };
            table.AddColumn(new TableColumn("Name") { IsKey = true });
            table.AddColumn(new TableColumn("Site") { Priority = 2 });
            table.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status, Priority = 9 });
            table.AddColumn(new TableColumn("Latency") {
                Presenter = new NumberPresenter("ms"),
                Priority = 5,
                ToneRules = { ToneRule.Above(100, ThemeToken.Warn) }
            });
            table.AddColumn(new TableColumn("Last sync") { Presenter = CellPresenters.Duration, Priority = 4 });
            // <more>
            table.AddValues("DC01.ad.adatum.com", "Warsaw", StatusKind.Pass, 12, TimeSpan.FromSeconds(42));
            table.AddValues("DC02.ad.adatum.com", "Warsaw", StatusKind.Warn, 140, TimeSpan.FromMinutes(42));
            table.AddValues("DC03.eu.ad.adatum.com", "Dublin", StatusKind.Pass, 31, TimeSpan.FromSeconds(64));
            table.AddValues("DC04.eu.ad.adatum.com", "Dublin", StatusKind.Fail, 0, TimeSpan.FromHours(4));
            // </more>
            Terminal.Write(table);
            // </example>
        }

        public static void Inspector() {
            // <example>
            Dictionary<string, object?> zone = new Dictionary<string, object?> {
                ["Name"] = "evotec.xyz",
                ["Serial"] = 2351234567L,
                ["Signed"] = false,
                ["Soa"] = new Dictionary<string, object?> {
                    ["Primary"] = "dana.ns.cloudflare.com",
                    ["Refresh"] = TimeSpan.FromSeconds(10000),
                    ["Expire"] = TimeSpan.FromSeconds(604800)
                },
                ["NameServers"] = new List<Dictionary<string, object?>> {
                    new Dictionary<string, object?> { ["Host"] = "dana.ns.cloudflare.com", ["Address"] = "108.162.192.103", ["LatencyMs"] = 12 },
                    new Dictionary<string, object?> { ["Host"] = "kirk.ns.cloudflare.com", ["Address"] = "172.64.33.149", ["LatencyMs"] = 14 }
                },
                ["Tags"] = new[] { "production", "cloudflare", "doh" }
            };
            Terminal.Write(new InspectorView(zone, new InspectorOptions { MaxDepth = 3 }, "evotec.xyz zone"));
            // </example>
        }
    }
}
