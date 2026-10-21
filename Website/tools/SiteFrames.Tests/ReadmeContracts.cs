using System;
using System.IO;
using SiteFrames;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The README's code blocks are the site's example sources; any drift is a problem the render reports.</summary>
    public sealed class ReadmeContracts : IDisposable {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "siteframes-readme-" + Guid.NewGuid().ToString("N"));
        private readonly Site _site;

        public ReadmeContracts() {
            Directory.CreateDirectory(Path.Combine(_root, "Website", "examples"));
            Directory.CreateDirectory(Path.Combine(_root, "Website", "content"));
            File.WriteAllText(Path.Combine(_root, "ConsoleMonster.sln"), string.Empty);
            File.WriteAllText(Path.Combine(_root, "Website", "examples", "rule.ps1"), "Import-Module ConsoleMonster\n\n#region more\n$title = 'Hello'\n#endregion\nWrite-ConsoleRule $title\n");
            File.WriteAllText(Path.Combine(_root, "Website", "examples", "long.ps1"), "Import-Module ConsoleMonster\n\nWrite-ConsoleRule '" + new string('x', 80) + "'\n");
            File.WriteAllText(Path.Combine(_root, "Website", "content", "page.md"), "Text\n\n```powershell\nInstall-Module ConsoleMonster\n```\n\n```bash\ndotnet run\n```\n");
            _site = new Site(Path.Combine(_root, "Website"));
        }

        public void Dispose() => Directory.Delete(_root, true);

        private const string FromFile = "<!-- example: Website/examples/rule.ps1 -->\n```powershell\nImport-Module ConsoleMonster\n\n$title = 'Hello'\nWrite-ConsoleRule $title\n```\n";

        [Fact]
        public void Find_BlocksMatchingTheirSources_HaveNoProblems() {
            string readme = FromFile + "<!-- example: Website/content/page.md#2 -->\n```bash\ndotnet run\n```\n";
            Assert.Empty(Readme.Find(_site, readme));
        }

        [Theory]
        [InlineData("<!-- example: Website/examples/rule.ps1 -->\n```powershell\nImport-Module ConsoleMonster\nWrite-ConsoleRule 'Hello'\n```\n")]
        [InlineData("<!-- example: Website/examples/rule.ps1 -->\n```csharp\nImport-Module ConsoleMonster\n\n$title = 'Hello'\nWrite-ConsoleRule $title\n```\n")]
        [InlineData("<!-- example: Website/content/page.md#1 -->\n```powershell\nInstall-Module ConsoleMonster -Force\n```\n")]
        [InlineData("<!-- example: Website/content/page.md#3 -->\n```bash\ndotnet run\n```\n")]
        [InlineData("<!-- example: Website/examples/missing.ps1 -->\n```powershell\nx\n```\n")]
        [InlineData("<img src=\"https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/none-dark.png\">")]
        [InlineData("Text\n\n```powershell\nGet-Thing\n```\n")]
        [InlineData("<!-- example: Website/examples/rule.ps1 -->\n\n```powershell\nImport-Module ConsoleMonster\n```\n")]
        [InlineData("<!-- example: Website/content/page.md#x -->\n```powershell\nInstall-Module ConsoleMonster\n```\n")]
        public void Find_DriftedBlocksAndMissingImages_AreReported(string readme) {
            Assert.NotEmpty(Readme.Find(_site, readme));
        }

        [Theory]
        [InlineData("<!-- example: Website/examples/long.ps1 -->\n```powershell\nImport-Module ConsoleMonster\n\nWrite-ConsoleRule 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'\n```\n", "characters")]
        [InlineData("<a href=\"https://consolemonster.dev/\"><picture>\n  <source media=\"(prefers-color-scheme: dark)\" srcset=\"https://x/d.png\">\n  <img alt=\"Hero\" src=\"https://x/l.png\">\n</picture></a>\n", "share a line")]
        [InlineData("<picture class=\"x\">\n  <img alt=\"Hero\" src=\"https://x/l.png\">\n</picture>\n", "share a line")]
        public void Find_LongLinesAndInlinePictures_AreReported(string readme, string message) {
            Assert.Contains(Readme.Find(_site, readme), problem => problem.Contains(message, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("<span class=\"x\"> </span>&nbsp;", true)]
        [InlineData("<span class=\"x\">Ctrl+C stop</span>", false)]
        public void Blank_RowsWithoutVisibleText_AreBlank(string row, bool blank) {
            Assert.Equal(blank, Readme.Blank(new[] { row }, 0));
            Assert.True(Readme.Blank(new[] { row }, 1));
        }

        [Fact]
        public void PackageReadme_TurnsPicturesIntoMarkdownImagesAndDropsMarkers() {
            string readme = "<picture>\n  <source media=\"(prefers-color-scheme: dark)\" srcset=\"https://x/hero-dark.png\">\n  <img alt=\"Hero\" src=\"https://x/hero-light.png\">\n</picture>\n\n<picture>\n  <source media=\"(prefers-color-scheme: dark)\" srcset=\"https://x/t-dark.png\">\n  <img alt=\"Tree\" width=\"532\" src=\"https://x/t-light.png\">\n</picture>\n\n" + FromFile;
            string package = Readme.PackageReadme(readme);
            Assert.StartsWith("![Hero](https://x/hero-light.png)\n\n![Tree](https://x/t-light.png)\n\n```powershell\n", package, StringComparison.Ordinal);
            Assert.DoesNotContain("<", package.Replace("<!--", string.Empty), StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_WritesBlocksFromTheirSources() {
            File.WriteAllText(Path.Combine(_root, "README.md"), "# Title\n\n<!-- example: Website/examples/rule.ps1 -->\n```powershell\nold\n```\n");
            Readme.Sync(_site);
            Assert.Equal("# Title\n\n" + FromFile, File.ReadAllText(Path.Combine(_root, "README.md")));
        }
    }
}
