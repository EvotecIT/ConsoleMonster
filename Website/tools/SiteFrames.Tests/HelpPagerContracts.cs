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
    /// <summary>Protects native help paging through the shared default-output broker on both PowerShell runtimes.</summary>
    public sealed class HelpPagerContracts {
        [NativeEditorTheory]
        [InlineData(120, "dark")]
        [InlineData(120, "light")]
        [InlineData(60, "dark")]
        [InlineData(60, "light")]
        public void DirectHelp_VisitsNextAndPreviousPageThenQuitsAndReleasesInput(int width, string theme) {
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
                var pages = new List<string>();
                bool sawCommand = false, sawFooter = false;
                foreach (char character in vt) {
                    screen.Feed(character.ToString());
                    if (!char.IsDigit(character) && character != 't') { continue; }
                    string text = screen.Transcript().ToPlainText();
                    sawCommand |= text.Contains("Get-ConsoleCapability", StringComparison.Ordinal);
                    sawFooter |= text.Contains("q quit", StringComparison.Ordinal);
                    MatchCollection matches = Regex.Matches(text, @"Page ([0-9]+) of ([0-9]+)");
                    if (matches.Count == 0) { continue; }
                    string page = matches[matches.Count - 1].Groups[1].Value;
                    if (pages.Count == 0 || pages[pages.Count - 1] != page) { pages.Add(page); }
                }
                Assert.True(sawCommand, host.Label + " did not show the actual command's help.");
                Assert.True(sawFooter, host.Label + " did not show the interactive pager footer.");
                Assert.Equal(new[] { "1", "2", "1" }, pages);
            }
        }

        private static string Repository([CallerFilePath] string source = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
