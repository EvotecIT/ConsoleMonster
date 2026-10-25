using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class DependencyFreeGraphicsTests {
        [Fact]
        public void ShippedGraphicsSourcesCannotLaunchAnExternalEncoder() {
            string graphics = Path.Combine(SourceRoot(), "ConsoleMonster.Graphics");
            Assert.True(Directory.Exists(graphics));
            var sources = Directory.EnumerateFiles(graphics, "*", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .Where(path => Path.GetExtension(path) == ".cs" || Path.GetExtension(path) == ".csproj");
            foreach (string source in sources) {
                string content = File.ReadAllText(source);
                Assert.False(content.IndexOf("ffmpeg", StringComparison.OrdinalIgnoreCase) >= 0, source);
                Assert.DoesNotContain("ProcessStartInfo", content);
                Assert.DoesNotContain("new Process", content);
                Assert.DoesNotContain("Process.Start", content);
            }
        }
        private static string SourceRoot([CallerFilePath] string sourcePath = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", ".."));
    }
}
