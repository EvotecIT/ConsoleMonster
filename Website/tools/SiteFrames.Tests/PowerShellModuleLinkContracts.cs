using System;
using System.IO;
using System.Threading.Tasks;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class PowerShellModuleLinkContracts {
        [Fact]
        public void ConcurrentCapturesShareLinksToTheSameCheckout() {
            string repository = Path.Combine(Path.GetTempPath(), "cm-module-links-" + Guid.NewGuid().ToString("N"));
            string? links = null;
            var results = new string[8];
            try {
                Directory.CreateDirectory(Path.Combine(repository, "Module"));
                Directory.CreateDirectory(Path.Combine(repository, "ConsoleMonster.PowerShell"));
                File.WriteAllText(Path.Combine(repository, "ConsoleMonster.sln"), string.Empty);
                File.WriteAllText(Path.Combine(repository, "Module", "identity.txt"), "module");
                File.WriteAllText(Path.Combine(repository, "ConsoleMonster.PowerShell", "identity.txt"), "binaries");
                var site = new Site(Path.Combine(repository, "Website"));
                Parallel.For(0, results.Length, index => results[index] = PowerShellRunner.ModuleLinkRoot(site));
                links = results[0];
                Assert.All(results, result => Assert.Equal(links, result));
                Assert.Equal("module", File.ReadAllText(Path.Combine(links, "ConsoleMonster", "identity.txt")));
                Assert.Equal("binaries", File.ReadAllText(Path.Combine(links, "ConsoleMonster.PowerShell", "identity.txt")));
            } finally {
                links ??= Array.Find(results, result => result != null);
                if (links != null) {
                    Directory.Delete(Path.Combine(links, "ConsoleMonster"));
                    Directory.Delete(Path.Combine(links, "ConsoleMonster.PowerShell"));
                    Directory.Delete(links);
                }
                Directory.Delete(repository, recursive: true);
            }
        }
    }
}
