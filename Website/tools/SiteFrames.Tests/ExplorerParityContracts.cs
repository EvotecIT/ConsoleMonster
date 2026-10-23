using System;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster.Styling;
using SiteFrames;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The published PowerShell explorer example must match its current C# screens and profile.</summary>
    [Collection("Interactive process defaults")]
    public sealed class ExplorerParityContracts {
        [Theory]
        [InlineData(120)]
        [InlineData(60)]
        public void ShownExplorerRejectsStaleSourceProfileMissingCaptureAndDifferentScreens(int width) {
            DirectoryInfo? repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ConsoleMonster.sln"))) { repository = repository.Parent; }
            Assert.NotNull(repository);
            string root = Path.Combine(Path.GetTempPath(), "siteframes-explorer-" + Guid.NewGuid().ToString("N"));
            try {
                string website = Path.Combine(root, "Website");
                string examples = Path.Combine(website, "examples", "docs");
                string captures = Path.Combine(website, "captures", "interactive");
                Directory.CreateDirectory(examples);
                Directory.CreateDirectory(captures);
                File.WriteAllText(Path.Combine(root, "ConsoleMonster.sln"), string.Empty);
                foreach (string extension in new[] { ".cs", ".ps1" }) {
                    File.Copy(Path.Combine(repository!.FullName, "Website", "examples", "docs", "object-explorer" + extension), Path.Combine(examples, "object-explorer" + extension));
                }
                string id = "object-explorer-dark-" + width;
                string capture = Path.Combine(captures, id + ".ps7.txt");
                foreach (string host in new[] { "ps5", "ps7" }) {
                    File.Copy(Path.Combine(repository!.FullName, "Website", "captures", "interactive", id + "." + host + ".txt"), Path.Combine(captures, id + "." + host + ".txt"));
                }
                string originalCapture = File.ReadAllText(capture);
                string script = Path.Combine(examples, "object-explorer.ps1");
                string originalScript = File.ReadAllText(script);
                Site site = new Site(website);
                var story = new InteractivePage.Story(id, "Object explorer", string.Empty, "F10", width, 20, false, false) {
                    Area = "docs", ExampleId = "object-explorer", Theme = Theme.Dark
                };
                List<string> problems = new List<string>();
                Assert.True(InteractivePage.CheckExample(site, story, problems), string.Join("\n", problems));
                Assert.Empty(problems);

                File.AppendAllText(script, "\n# changed displayed source\n");
                Assert.False(InteractivePage.CheckExample(site, story, problems));
                Assert.Equal(2, problems.Count);
                Assert.All(problems, problem => Assert.Contains("another version", problem, StringComparison.Ordinal));
                File.WriteAllText(script, originalScript);

                problems.Clear();
                story.Theme = Theme.Light;
                Assert.False(InteractivePage.CheckExample(site, story, problems));
                Assert.Equal(2, problems.Count);
                Assert.All(problems, problem => Assert.Contains("another version", problem, StringComparison.Ordinal));
                story.Theme = Theme.Dark;

                problems.Clear();
                File.WriteAllText(capture, originalCapture.Replace("web-01", "wrong!", StringComparison.Ordinal));
                Assert.False(InteractivePage.CheckExample(site, story, problems));
                Assert.Contains("differs between PowerShell and C#", Assert.Single(problems), StringComparison.Ordinal);

                problems.Clear();
                File.Delete(capture);
                Assert.False(InteractivePage.CheckExample(site, story, problems));
                Assert.Contains("no PowerShell capture", Assert.Single(problems), StringComparison.Ordinal);
            } finally {
                if (Directory.Exists(root)) { Directory.Delete(root, true); }
            }
        }
    }
}
