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
        public void Find_DriftedBlocksAndMissingImages_AreReported(string readme) {
            Assert.Single(Readme.Find(_site, readme));
        }

        [Fact]
        public void Sync_WritesBlocksFromTheirSources() {
            File.WriteAllText(Path.Combine(_root, "README.md"), "# Title\n\n<!-- example: Website/examples/rule.ps1 -->\n```powershell\nold\n```\n");
            Readme.Sync(_site);
            Assert.Equal("# Title\n\n" + FromFile, File.ReadAllText(Path.Combine(_root, "README.md")));
        }
    }
}
