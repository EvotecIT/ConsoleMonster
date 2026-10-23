using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>Paths from recorded runs and imported traces look the same on every host.</summary>
    public sealed class DisplayPathTests {
        [Theory]
        [InlineData(@"C:\src\Operations\Deployment\Scripts", 28, "…", @"C:\src\…\Deployment\Scripts")]
        [InlineData("/src/Operations/Deployment/Scripts", 26, "…", "/src/…/Deployment/Scripts")]
        [InlineData("src/Operations/Deployment/Scripts", 25, "…", "src/…/Deployment/Scripts")]
        [InlineData(@"~\src\Operations\Deployment\Scripts", 26, "…", @"~\src\…\Deployment\Scripts")]
        [InlineData("~/src/Operations/Deployment/Scripts", 28, "...", "~/src/.../Deployment/Scripts")]
        [InlineData(@"\\server\share\Operations\Deployment\Scripts", 37, "…", @"\\server\share\…\Deployment\Scripts")]
        [InlineData("oversized-parent/Scripts", 8, "…", "Scripts")]
        [InlineData("src/Operations/abcdefghijklmnopqrst", 10, "…", "abcde…qrst")]
        [InlineData("/src/開発/Operations/Deployment/Scripts", 26, "…", "/src/…/Deployment/Scripts")]
        [InlineData("~/src/Scripts/", 40, "…", "~/src/Scripts/")]
        [InlineData("/", 1, "…", "/")]
        [InlineData("relative", 0, "…", "")]
        public void FitKeepsWholeSegmentsUntilTheFinalSegmentExceedsTheBudget(string path, int width, string marker, string expected) {
            string result = DisplayPath.Fit(path, width, TerminalWidthProfile.Standard, marker);
            Assert.Equal(expected, result);
            Assert.True(DisplayWidth.Measure(result) <= width);
        }

        [Theory]
        [InlineData(@"C:\repo\tests\Deploy.Tests.ps1", "Deploy.Tests.ps1")]
        [InlineData("/home/user/repo/tests/Deploy.Tests.ps1", "Deploy.Tests.ps1")]
        [InlineData(@"\\server\share\x.ps1", "x.ps1")]
        [InlineData("plain.ps1", "plain.ps1")]
        [InlineData(@"C:\repo\folder\", "")]
        [InlineData("", "")]
        public void FileName_SplitsOnBothSeparatorsOnEveryHost(string path, string expected) {
            Assert.Equal(expected, DisplayPath.FileName(path));
        }

        [Theory]
        [InlineData(@"C:\repo\deploy.ps1", false, true)]
        [InlineData("c:/repo/deploy.ps1", false, true)]
        [InlineData(@"\\server\share\x.ps1", false, true)]
        [InlineData("/home/user/x.ps1", false, false)]
        [InlineData("/home/user/x.ps1", true, true)]
        [InlineData(@"C:\repo\deploy.ps1", true, false)]
        [InlineData("relative/x.ps1", false, false)]
        [InlineData("relative/x.ps1", true, false)]
        public void IsForeign_RecognisesPathsFromTheOtherKindOfSystem(string path, bool hostIsWindows, bool expected) {
            Assert.Equal(expected, DisplayPath.IsForeign(path, hostIsWindows));
        }

        [Theory]
        [InlineData(@"C:\repo\deploy.ps1", @"C:\repo", "deploy.ps1")]
        [InlineData(@"C:\repo\src\Inventory.cs", @"c:/REPO", @"src\Inventory.cs")]
        [InlineData(@"D:\other\tools\build.ps1", @"C:\repo", @"tools\build.ps1")]
        [InlineData(@"C:\deploy.ps1", null, "deploy.ps1")]
        [InlineData("/srv/app/run.sh", "/srv", "app/run.sh")]
        [InlineData("/var/log/app/run.log", null, "app/run.log")]
        public void Short_IsRelativeUnderTheBaseOtherwiseFolderAndFile(string path, string? basePath, string expected) {
            Assert.Equal(expected, DisplayPath.Short(path, basePath));
        }
    }
}
