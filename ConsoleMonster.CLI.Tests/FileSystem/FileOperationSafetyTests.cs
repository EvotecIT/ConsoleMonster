using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.FileSystem;
using Xunit;
#if NET8_0_OR_GREATER
using System.Security.AccessControl;
using System.Security.Principal;
#endif

namespace ConsoleMonster.Tests.FileSystem {
    public sealed class FileOperationSafetyTests {
#if NET8_0_OR_GREATER
        [Fact]
        public async Task Copy_RejectsDestinationInsideTheSourceThroughADirectoryAlias() {
            using TempFolder temp = new TempFolder();
            string source = temp.Folder("tree");
            temp.File("tree/keep.txt", "original");
            string alias = temp.Combine("alias");
            LinkDirectory(alias, source);
            using CancellationTokenSource stop = new CancellationTokenSource();
            // Keep a broken recursive copy bounded while still observing its first attempted write.
            var options = new FileOperationOptions { Progress = new CallbackProgress(_ => {
                if (Directory.Exists(temp.Combine("tree", "nested"))) { stop.Cancel(); }
            }) };
            FileOperationResult result = await FileOperation.Copy(new[] { source }, temp.Combine("alias", "nested"), options).RunAsync(stop.Token);
            Assert.Equal(FileOperationErrorKind.IntoItself, Assert.Single(result.Errors).Kind);
            Assert.False(Directory.Exists(temp.Combine("tree", "nested")));
            Assert.Equal("original", File.ReadAllText(temp.Combine("tree", "keep.txt")));
        }

        [Fact]
        public async Task Copy_AValidLongBasenameDoesNotOverflowItsStagingName() {
            using TempFolder temp = new TempFolder();
            string name = new string('a', 236) + ".txt";
            string source = temp.File("source/" + name, "complete");
            string destination = temp.Folder("destination");
            FileOperationResult result = await FileOperation.Copy(new[] { source }, destination).RunAsync();
            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            Assert.Equal("complete", File.ReadAllText(Path.Combine(destination, name)));
            Assert.Single(Directory.GetFiles(destination));
        }

        [WindowsFact]
        public async Task Copy_LinkCreationFailurePreservesTheExistingDestination() {
            using TempFolder temp = new TempFolder();
            string source = temp.Combine("source-link");
            LinkDirectory(source, temp.Folder("linked-folder"));
            string destination = temp.Folder("destination");
            string target = temp.File("destination/source-link", "previous destination");
            var directory = new DirectoryInfo(destination);
            DirectorySecurity original = directory.GetAccessControl();
            DirectorySecurity denied = directory.GetAccessControl();
            denied.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!,
                FileSystemRights.CreateDirectories | FileSystemRights.CreateFiles, AccessControlType.Deny));
            try {
                directory.SetAccessControl(denied);
                FileOperationResult result = await FileOperation.Copy(new[] { source }, destination,
                    new FileOperationOptions { ConflictPolicy = FileConflictPolicy.Overwrite }).RunAsync();
                Assert.False(result.Succeeded);
                Assert.True(File.Exists(target), "A failed replacement removed the previous destination.");
                Assert.Equal("previous destination", File.ReadAllText(target));
                Assert.True(Directory.Exists(source));
            } finally { directory.SetAccessControl(original); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Retry_AFailedFolderRemovalFinishesTheRequestedRoot(bool move) {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            using TempFolder temp = new TempFolder();
            string source = temp.Folder("source");
            string locked = temp.File("source/locked.txt", "keep");
            string destination = temp.Folder("destination");
            temp.Folder("destination/source"); // Force a merge, so Move reaches the locked child.
            FileOperation operation = move ? FileOperation.Move(new[] { source }, destination) : FileOperation.DeletePermanently(new[] { source });
            FileOperationResult failed;
            using (FileStream held = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { failed = await operation.RunAsync(); }
            Assert.Single(failed.Errors);
            FileOperation retry = failed.RetryFailed()!;
            Assert.Equal(new[] { source }, retry.Sources);
            FileOperationResult completed = await retry.RunAsync();
            Assert.True(completed.Succeeded, string.Join("; ", completed.Errors));
            Assert.False(Directory.Exists(source));
            if (move) { Assert.Equal("keep", File.ReadAllText(temp.Combine("destination/source/locked.txt"))); }
        }

        [Fact]
        public async Task Copy_PrivateUnixDirectoryKeepsItsModeBeforeAndAfterWriting() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            using TempFolder temp = new TempFolder();
            string source = temp.Folder("private");
            temp.File("private/readable.txt", "protected by parent");
            UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
            File.SetUnixFileMode(source, mode);
            string target = temp.Combine("copy");
            bool observed = false;
            var options = new FileOperationOptions { ProgressInterval = TimeSpan.Zero, Progress = new CallbackProgress(progress => {
                if (progress.CurrentItemBytesDone > 0) { observed = true; Assert.Equal(mode, File.GetUnixFileMode(target)); }
            }) };
            FileOperationResult result = await FileOperation.Copy(new[] { source }, target, options).RunAsync();
            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            Assert.True(observed);
            Assert.Equal(mode, File.GetUnixFileMode(target));
            Assert.Equal("protected by parent", File.ReadAllText(Path.Combine(target, "readable.txt")));
        }

        [Fact]
        public async Task Move_DistinctCaseSensitiveEntriesUsesTheConflictPolicy() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return; }
            using TempFolder temp = new TempFolder();
            string source = temp.File("draft.txt", "source");
            string target = temp.File("Draft.txt", "target");
            Assert.Equal(2, Directory.GetFiles(temp.Path).Length);
            FileOperationResult result = await FileOperation.Move(new[] { source }, target, new FileOperationOptions { ConflictPolicy = FileConflictPolicy.Skip }).RunAsync();
            Assert.True(result.Succeeded);
            Assert.Equal(1, result.Skipped);
            Assert.Equal("source", File.ReadAllText(source));
            Assert.Equal("target", File.ReadAllText(target));
            Assert.Equal(2, Directory.GetFiles(temp.Path).Length);
        }

        private static void LinkDirectory(string link, string target) {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Directory.CreateSymbolicLink(link, target); return; }
            // Junction creation does not require symbolic-link elevation; every path belongs to this fixture.
            using Process process = Process.Start(new ProcessStartInfo("cmd.exe", "/d /c mklink /J \"" + link + "\" \"" + target + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            })!;
            if (!process.WaitForExit(5000)) { process.Kill(true); process.WaitForExit(); throw new TimeoutException("Fixture junction creation timed out."); }
            Assert.Equal(0, process.ExitCode);
        }

        private sealed class WindowsFactAttribute : FactAttribute {
            public WindowsFactAttribute() { if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Skip = "Requires a task-owned Windows directory ACL and junction."; } }
        }

        private sealed class CallbackProgress(Action<FileOperationProgress> callback) : IProgress<FileOperationProgress> {
            public void Report(FileOperationProgress value) => callback(value);
        }
#endif
    }
}
