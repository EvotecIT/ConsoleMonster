using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleMonster.Sessions;
using SiteFrames;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Protects native help in the document viewer through the shared default-output broker on both PowerShell runtimes.</summary>
    public sealed class HelpPagerContracts {
        [NativeEditorTheory]
        [InlineData(120, "dark")]
        [InlineData(120, "light")]
        [InlineData(60, "dark")]
        [InlineData(60, "light")]
        public void DirectHelp_PagesDownAndBackThenQuitsAndReleasesInput(int width, string theme) {
            string root = Repository();
            var site = new Site(Path.Combine(root, "Website"));
            foreach (PowerShellHost host in PowerShellHost.Available) {
                var environment = PowerShellRunner.Environment(theme);
                environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
                string example = Path.Combine(root, "Website", "examples", "help-formatting", "help.ps1");
                string driver = Path.Combine(site.Tool, "Capture", "Assets", "HelpFormatting.ps1");
                // The driver's remaining-key and input-lease assertions run inside the actual host.
                var chunks = PowerShellRunner.Run(site, host, driver, width, 18, environment,
                    arguments: "-ExamplePath '" + example.Replace("'", "''") + "' -Formatted -Paged", interactive: true);
                string vt = Encoding.UTF8.GetString(PowerShellRunner.Bytes(chunks));
                var screen = new VtScreen(width, 18);
                var tops = new List<string>();
                bool sawCommand = false, sawTabs = false;
                foreach (char character in vt) {
                    screen.Feed(character.ToString());
                    if (!char.IsDigit(character)) { continue; }
                    string header = screen.Snapshot().GetRowText(0);
                    string text = screen.Snapshot().ToPlainText();
                    sawCommand |= header.Contains("Get-ConsoleCapability", StringComparison.Ordinal);
                    sawTabs |= text.Contains("Overview", StringComparison.Ordinal) && text.Contains("Parameters", StringComparison.Ordinal);
                    // The viewer's header ends with the visible rows: "1–9 / 40".
                    Match range = Regex.Match(header, @"([0-9]+)[–-][0-9]+ / [0-9]+\s*$");
                    if (!range.Success) { continue; }
                    string top = range.Groups[1].Value;
                    if (tops.Count == 0 || tops[tops.Count - 1] != top) { tops.Add(top); }
                }
                Assert.True(sawCommand, host.Label + " did not show the actual command's help in the viewer.");
                Assert.True(sawTabs, host.Label + " did not show the help's tabs.");
                // ConPTY may send a range in pieces, so the order matters rather than the count: the top, a later page, the top again.
                Assert.Equal("1", tops[0]);
                int down = tops.FindIndex(1, top => top != "1");
                Assert.True(down > 0 && tops.FindIndex(down, top => top == "1") > down, host.Label + " pages: " + string.Join(",", tops));
            }
        }
        private static string Repository([CallerFilePath] string source = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
