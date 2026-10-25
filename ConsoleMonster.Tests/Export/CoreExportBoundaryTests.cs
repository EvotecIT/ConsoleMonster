using System;
using System.IO;
using ConsoleMonster.Export;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Export {
    public sealed class CoreExportBoundaryTests {
        [Theory]
        [InlineData("report.md")]
        [InlineData("report.MARKDOWN")]
        public void MarkdownFileRequestsNameTheRequiredPackageWithoutWriting(string name) {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + name);
            var error = Assert.Throws<NotSupportedException>(() => ConsoleExporter.Save(new TextBlock("report"), path));
            Assert.Contains("ConsoleMonster.Markdown", error.Message);
            Assert.False(File.Exists(path));
        }
    }
}
