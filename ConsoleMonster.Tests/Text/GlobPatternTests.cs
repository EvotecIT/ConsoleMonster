using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class GlobPatternTests {
        [Theory]
        [InlineData("*", "anything.txt", true)]
        [InlineData("*.cs", "Program.cs", true)]
        [InlineData("*.cs", "Program.CS", true)]
        [InlineData("*.cs", "Program.csproj", false)]
        [InlineData("report-??.csv", "report-07.csv", true)]
        [InlineData("report-??.csv", "report-7.csv", false)]
        [InlineData("[a-c]*", "beta", true)]
        [InlineData("[a-c]*", "delta", false)]
        [InlineData("[!a-c]*", "delta", true)]
        [InlineData("[abc", "[abc", true)]
        [InlineData("a*b*c", "aXXbYYc", true)]
        [InlineData("a*b*c", "aXXbYY", false)]
        [InlineData("*.cs;*.md", "README.md", true)]
        [InlineData("*.cs, *.md", "build.ps1", false)]
        [InlineData("*.log;!debug*", "app.log", true)]
        [InlineData("*.log;!debug*", "debug.log", false)]
        [InlineData("!*.tmp", "keep.txt", true)]
        [InlineData("!*.tmp", "scratch.tmp", false)]
        [InlineData("\\*", "*", true)]
        [InlineData("\\*", "x", false)]
        [InlineData("", "x", false)]
        public void Matches(string pattern, string name, bool expected) => Assert.Equal(expected, GlobPattern.IsMatch(pattern, name));

        [Fact]
        public void CaseSensitiveMatching_IsOptIn() {
            Assert.False(new GlobPattern("*.CS", caseSensitive: true).IsMatch("a.cs"));
            Assert.True(new GlobPattern("[A-C]*", caseSensitive: false).IsMatch("beta"));
            Assert.False(new GlobPattern("[A-C]*", caseSensitive: true).IsMatch("beta"));
        }

        [Fact]
        public void PathologicalPatterns_StayLinear() {
            string text = new string('a', 5000);
            Assert.False(GlobPattern.IsMatch("*a*a*a*a*a*a*a*a*b", text));
            Assert.False(new GlobPattern("*").IsMatch(null));
        }
    }
}
