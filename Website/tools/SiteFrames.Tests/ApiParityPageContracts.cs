using System;
using System.IO;
using System.Linq;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The site supplies its own page title; the standalone Markdown keeps its document heading.</summary>
    public sealed class ApiParityPageContracts {
        [Fact]
        public void WebsiteBodyOmitsStandaloneTitleAndBothOutputsUseLf() {
            DirectoryInfo? repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ConsoleMonster.sln"))) {
                repository = repository.Parent;
            }
            Assert.NotNull(repository);
            string temporary = Path.Combine(Path.GetTempPath(), "cm-api-parity-" + Guid.NewGuid().ToString("N"));
            try {
                Directory.CreateDirectory(Path.Combine(temporary, "Tests", "Api"));
                File.WriteAllText(Path.Combine(temporary, "ConsoleMonster.sln"), string.Empty);
                File.Copy(Path.Combine(repository!.FullName, "Tests", "Api", "round2-parity.json"),
                    Path.Combine(temporary, "Tests", "Api", "round2-parity.json"));

                ApiParityPage.Render(new Site(Path.Combine(temporary, "Website")));

                string document = File.ReadAllText(Path.Combine(temporary, "Docs", "Api-Parity.md"));
                string page = File.ReadAllText(Path.Combine(temporary, "Website", "content", "docs", "api-parity.md"));
                Assert.Equal(1, document.Split('\n').Count(line => line.StartsWith("# ", StringComparison.Ordinal)));
                Assert.DoesNotContain(page.Split('\n'), line => line.StartsWith("# ", StringComparison.Ordinal));
                Assert.Contains(page.Split('\n'), line => line.StartsWith("## ", StringComparison.Ordinal));
                Assert.DoesNotContain('\r', document);
                Assert.DoesNotContain('\r', page);
            } finally {
                if (Directory.Exists(temporary)) { Directory.Delete(temporary, recursive: true); }
            }
        }
    }
}
