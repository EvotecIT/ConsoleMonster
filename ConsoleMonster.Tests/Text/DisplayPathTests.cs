using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>Paths from recorded runs and imported traces look the same on every host.</summary>
    public sealed class DisplayPathTests {
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
