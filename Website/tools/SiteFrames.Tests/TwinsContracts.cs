using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using SiteFrames;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Static figure parity uses the requested capture and rejects stale sources or different output.</summary>
    public sealed class TwinsContracts {
        [Theory]
        [InlineData(null)]
        [InlineData("light-60")]
        public void CheckRequiresBothHostsAndMatchingSourceProfileAndCells(string? variant) {
            string root = Path.Combine(Path.GetTempPath(), "siteframes-twins-" + Guid.NewGuid().ToString("N"));
            try {
                string website = Path.Combine(root, "Website");
                string scripts = Path.Combine(website, "examples", "docs");
                string captures = Path.Combine(website, "captures", "docs");
                Directory.CreateDirectory(scripts);
                Directory.CreateDirectory(captures);
                File.WriteAllText(Path.Combine(root, "ConsoleMonster.sln"), string.Empty);
                string script = Path.Combine(scripts, "chart.ps1");
                File.WriteAllText(script, "Write-ConsoleText good\n");
                Site site = new Site(website);
                Dictionary<string, string?> environment = PowerShellRunner.Environment(variant == null ? "dark" : "light");
                JsonObject hosts = new JsonObject { ["ps5"] = "5.1", ["ps7"] = "7.5" };
                foreach (PowerShellHost host in PowerShellHost.All) {
                    string path = Path.Combine(captures, "chart" + (variant == null ? string.Empty : "-" + variant) + "." + host.Id + ".ansi");
                    File.WriteAllText(path, "good\r\n");
                    Stamps.Write(site, path, Stamps.Of(script, environment, host.Id, 8.ToString(CultureInfo.InvariantCulture)));
                }
                CellBuffer frame = new CellBuffer(8, 1);
                for (int x = 0; x < 4; x++) { frame.Set(x, 0, "good"[x].ToString(), Style.Plain); }
                List<string> problems = new List<string>();
                JsonObject result = Twins.Check(site, "docs", "chart", 8, frame, hosts, problems, variant, environment);
                Assert.Empty(problems);
                Assert.True(result["ps5"]!["identical"]!.GetValue<bool>());
                Assert.True(result["ps7"]!["identical"]!.GetValue<bool>());

                environment["CONSOLEMONSTER_COLOR"] = "16";
                Twins.Check(site, "docs", "chart", 8, frame, hosts, problems, variant, environment);
                Assert.Equal(2, problems.Count);
                Assert.All(problems, p => Assert.Contains("another version", p, StringComparison.Ordinal));

                problems.Clear();
                environment["CONSOLEMONSTER_COLOR"] = "truecolor";
                File.AppendAllText(script, "# changed shown source\n");
                Twins.Check(site, "docs", "chart", 8, frame, hosts, problems, variant, environment);
                Assert.Equal(2, problems.Count);
                Assert.All(problems, p => Assert.Contains("another version", p, StringComparison.Ordinal));

                problems.Clear();
                File.WriteAllText(script, "Write-ConsoleText good\n");
                frame.Set(0, 0, "b", Style.Plain);
                Twins.Check(site, "docs", "chart", 8, frame, hosts, problems, variant, environment);
                Assert.Equal(2, problems.Count);
                Assert.All(problems, p => Assert.Contains("row 1 differs", p, StringComparison.Ordinal));

                problems.Clear();
                File.Delete(Path.Combine(captures, "chart" + (variant == null ? string.Empty : "-" + variant) + ".ps5.ansi"));
                Twins.Check(site, "docs", "chart", 8, frame, hosts, problems, variant, environment);
                Assert.Contains(problems, p => p.Contains("has no Windows PowerShell 5.1 capture", StringComparison.Ordinal));
            } finally {
                if (Directory.Exists(root)) { Directory.Delete(root, true); }
            }
        }
    }
}
