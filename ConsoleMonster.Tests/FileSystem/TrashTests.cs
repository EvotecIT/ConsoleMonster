using System;
using System.IO;
using System.Runtime.InteropServices;
using ConsoleMonster.FileSystem;
using Xunit;

namespace ConsoleMonster.Tests.FileSystem {
    public sealed class TrashTests {
        [Fact]
        public void XdgTrash_MovesTheItemAndWritesARestorableRecord() {
            using TempFolder temp = new TempFolder();
            string trashRoot = temp.Combine("Trash");
            XdgTrash trash = new XdgTrash(trashRoot);
            string file = temp.File("work/my report #1.txt", "r");
            string? location = trash.MoveToTrash(file);
            Assert.False(File.Exists(file));
            Assert.Equal(Path.Combine(trashRoot, "files", "my report #1.txt"), location);
            Assert.Equal("r", File.ReadAllText(location!));
            string[] info = File.ReadAllLines(Path.Combine(trashRoot, "info", "my report #1.txt.trashinfo"));
            Assert.Equal("[Trash Info]", info[0]);
            Assert.StartsWith("Path=", info[1], StringComparison.Ordinal);
            Assert.Contains("my%20report%20%231.txt", info[1]);
            Assert.Matches(@"^DeletionDate=\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$", info[2]);
        }

        [Fact]
        public void XdgTrash_KeepsBothItemsWhenNamesCollide_AndTrashesFolders() {
            using TempFolder temp = new TempFolder();
            XdgTrash trash = new XdgTrash(temp.Combine("Trash"));
            string first = trash.MoveToTrash(temp.File("a/notes.md", "1"))!;
            string second = trash.MoveToTrash(temp.File("b/notes.md", "2"))!;
            Assert.NotEqual(first, second);
            Assert.Equal("notes.2.md", Path.GetFileName(second));
            Assert.True(File.Exists(temp.Combine("Trash", "info", "notes.2.md.trashinfo")));
            string folder = temp.Folder("c/project");
            temp.File("c/project/x.txt");
            string trashed = trash.MoveToTrash(folder)!;
            Assert.False(Directory.Exists(folder));
            Assert.True(File.Exists(Path.Combine(trashed, "x.txt")));
        }

        [Fact]
        public void XdgTrash_RefusesMissingItemsAndTheTrashItself() {
            using TempFolder temp = new TempFolder();
            XdgTrash trash = new XdgTrash(temp.Combine("Trash"));
            Assert.Throws<FileNotFoundException>(() => trash.MoveToTrash(temp.Combine("ghost")));
            Directory.CreateDirectory(temp.Combine("Trash", "files"));
            Assert.Throws<IOException>(() => trash.MoveToTrash(temp.Combine("Trash", "files")));
        }

        [Fact]
        public void XdgTrash_DefaultHomeFollowsXdgDataHome() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return; }
            using TempFolder temp = new TempFolder();
            string? previous = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            try {
                Environment.SetEnvironmentVariable("XDG_DATA_HOME", temp.Combine("data"));
                Assert.Equal(temp.Combine("data", "Trash"), new XdgTrash().HomeTrash);
            } finally { Environment.SetEnvironmentVariable("XDG_DATA_HOME", previous); }
        }

        [Fact]
        public void CurrentPlatformTrash_TakesATempFile() {
            ITrash trash = FileTrash.Current;
            Assert.True(trash.IsAvailable, trash.Name + " should be available on this machine.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Assert.Equal("Recycle Bin", trash.Name); } else { Assert.Equal("Trash", trash.Name); }
            using TempFolder temp = new TempFolder();
            string file = temp.File("ConsoleMonster-trash-test-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt", "temporary");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                // Keep the user's own trash out of it: the trash record goes to a temp XDG home.
                trash = new XdgTrash(temp.Combine("xdg", "Trash"));
            }
            string? location = trash.MoveToTrash(file);
            Assert.False(File.Exists(file));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                Assert.NotNull(location);
                Assert.Equal("temporary", File.ReadAllText(location!));
                File.Delete(location!);
            }
        }

        [Fact]
        public void Unavailable_NeverDeletes() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("a.txt");
            Assert.False(FileTrash.Unavailable.IsAvailable);
            Assert.Throws<IOException>(() => FileTrash.Unavailable.MoveToTrash(file));
            Assert.True(File.Exists(file));
        }
    }
}
