using System;
using System.IO;
using System.Runtime.InteropServices;
using ConsoleMonster.FileSystem;
using Xunit;

namespace ConsoleMonster.Tests.FileSystem {
    public sealed class TrashSecurityTests {
#if NET8_0_OR_GREATER
        [Fact]
        public void XdgTrash_CreatesPrivateDirectoriesBeforeMovingAReadableFile() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return; }
            using TempFolder temp = new TempFolder();
            string source = temp.File("private/readable.txt", "secret");
            File.SetUnixFileMode(Path.GetDirectoryName(source)!, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(source, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            string root = temp.Combine("Trash");
            string target = new XdgTrash(root).MoveToTrash(source)!;
            foreach (string directory in new[] { root, Path.Combine(root, "files"), Path.Combine(root, "info") }) {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(directory));
            }
            Assert.Equal("secret", File.ReadAllText(target));
            Assert.False(File.Exists(source));
        }

        [Theory]
        [InlineData("root", false)]
        [InlineData("files", false)]
        [InlineData("info", false)]
        [InlineData("root", true)]
        [InlineData("files", true)]
        [InlineData("info", true)]
        public void XdgTrash_RejectsPublicOrSymlinkedDirectoriesWithoutMovingTheSource(string component, bool link) {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return; }
            using TempFolder temp = new TempFolder();
            string root = temp.Combine("Trash");
            string bad = component == "root" ? root : Path.Combine(root, component);
            if (component != "root") { Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
            string redirected = temp.Folder("redirected");
            if (link) { Directory.CreateSymbolicLink(bad, redirected); }
            else { Directory.CreateDirectory(bad, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.OtherRead); }
            string source = temp.File("source.txt", "original");
            Assert.Throws<IOException>(() => new XdgTrash(root).MoveToTrash(source));
            Assert.Equal("original", File.ReadAllText(source));
            Assert.Empty(Directory.GetFileSystemEntries(redirected));
        }
#endif
    }
}
