using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.FileSystem;
using Xunit;

namespace ConsoleMonster.Tests.FileSystem {
    public sealed class FileOperationTests {
        private static readonly DateTime Old = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        private static readonly DateTime New = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Utc);

        [Fact]
        public async Task Copy_CopiesATreeIntoTheFolderAndKeepsTimestamps() {
            using TempFolder temp = new TempFolder();
            string source = temp.Folder("src/project");
            temp.File("src/project/readme.md", "hello", Old);
            temp.File("src/project/lib/core.cs", new string('x', 5000), Old);
            temp.Folder("src/project/empty");
            string destination = temp.Folder("dst");

            FileOperationResult result = await FileOperation.Copy(new[] { source }, destination).RunAsync();

            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            Assert.Equal("hello", File.ReadAllText(Path.Combine(destination, "project", "readme.md")));
            Assert.Equal(5000, new FileInfo(Path.Combine(destination, "project", "lib", "core.cs")).Length);
            Assert.True(Directory.Exists(Path.Combine(destination, "project", "empty")));
            Assert.Equal(Old, File.GetLastWriteTimeUtc(Path.Combine(destination, "project", "readme.md")));
            Assert.Equal(5005, result.BytesTransferred);
            Assert.True(Directory.Exists(source));
            Assert.Empty(Directory.EnumerateFiles(destination, "*.partial", SearchOption.AllDirectories));
        }

        [Fact]
        public async Task Copy_WithoutPreservedTimestamps_StampsNow() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("a.txt", "a", Old);
            string destination = temp.Folder("dst");
            await FileOperation.Copy(new[] { file }, destination, new FileOperationOptions { PreserveTimestamps = false }).RunAsync();
            Assert.True(File.GetLastWriteTimeUtc(Path.Combine(destination, "a.txt")) > Old.AddYears(1));
        }

        [Fact]
        public async Task Copy_OneItemToANewName_CopiesUnderThatName_RelativeToTheSourceFolder() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("report.csv", "1,2");
            FileOperation operation = FileOperation.Copy(new[] { file }, "backup.csv");
            Assert.Equal(temp.Combine("backup.csv"), operation.Targets[0]);
            Assert.True((await operation.RunAsync()).Succeeded);
            Assert.Equal("1,2", File.ReadAllText(temp.Combine("backup.csv")));
            Assert.True(File.Exists(file));
        }

        [Fact]
        public async Task Copy_SeveralItemsToAMissingFolder_CreatesIt() {
            using TempFolder temp = new TempFolder();
            string a = temp.File("a.txt"), b = temp.File("b.txt");
            FileOperationResult result = await FileOperation.Copy(new[] { a, b }, temp.Combine("new", "place")).RunAsync();
            Assert.True(result.Succeeded);
            Assert.True(File.Exists(temp.Combine("new", "place", "a.txt")));
            Assert.True(File.Exists(temp.Combine("new", "place", "b.txt")));
        }

        [Theory]
        [InlineData(FileConflictPolicy.Skip, "old", 1)]
        [InlineData(FileConflictPolicy.Overwrite, "new", 0)]
        [InlineData(FileConflictPolicy.Ask, "old", 1)]
        public async Task Conflicts_FollowThePolicy(FileConflictPolicy policy, string expected, int skipped) {
            using TempFolder temp = new TempFolder();
            string file = temp.File("src/a.txt", "new", New);
            temp.File("dst/a.txt", "old", Old);
            FileOperationResult result = await FileOperation.Copy(new[] { file }, temp.Combine("dst"), new FileOperationOptions { ConflictPolicy = policy }).RunAsync();
            Assert.Equal(expected, File.ReadAllText(temp.Combine("dst", "a.txt")));
            Assert.Equal(skipped, result.Skipped);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task OverwriteOlder_ReplacesOnlyOlderFiles() {
            using TempFolder temp = new TempFolder();
            string newer = temp.File("src/newer.txt", "fresh", New);
            string older = temp.File("src/older.txt", "stale", Old);
            temp.File("dst/newer.txt", "kept?", Old);
            temp.File("dst/older.txt", "kept", New);
            FileOperationResult result = await FileOperation.Copy(new[] { newer, older }, temp.Combine("dst"), new FileOperationOptions { ConflictPolicy = FileConflictPolicy.OverwriteOlder }).RunAsync();
            Assert.Equal("fresh", File.ReadAllText(temp.Combine("dst", "newer.txt")));
            Assert.Equal("kept", File.ReadAllText(temp.Combine("dst", "older.txt")));
            Assert.Equal(1, result.Completed);
            Assert.Equal(1, result.Skipped);
        }

        [Fact]
        public async Task RenamePolicy_KeepsBothFiles() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("src/a.txt", "new");
            temp.File("dst/a.txt", "old");
            temp.File("dst/a (2).txt", "older");
            FileOperationResult result = await FileOperation.Copy(new[] { file }, temp.Combine("dst"), new FileOperationOptions { ConflictPolicy = FileConflictPolicy.Rename }).RunAsync();
            Assert.True(result.Succeeded);
            Assert.Equal("old", File.ReadAllText(temp.Combine("dst", "a.txt")));
            Assert.Equal("new", File.ReadAllText(temp.Combine("dst", "a (3).txt")));
        }

        [Fact]
        public async Task Ask_PassesBothSidesAndOverwriteAllStopsAsking() {
            using TempFolder temp = new TempFolder();
            List<string> sources = new List<string>();
            for (int i = 0; i < 3; i++) {
                sources.Add(temp.File("src/f" + i + ".txt", "new" + i, New));
                temp.File("dst/f" + i + ".txt", "o", Old);
            }
            List<FileConflict> asked = new List<FileConflict>();
            FileOperationOptions options = new FileOperationOptions {
                ResolveConflict = (conflict, token) => { asked.Add(conflict); return Task.FromResult(FileConflictResolution.OverwriteAll); }
            };
            FileOperationResult result = await FileOperation.Copy(sources, temp.Combine("dst"), options).RunAsync();
            FileConflict first = Assert.Single(asked);
            Assert.Equal(4, first.SourceSize);
            Assert.Equal(1, first.TargetSize);
            Assert.Equal(New, first.SourceModified);
            Assert.Equal(Old, first.TargetModified);
            Assert.True(first.SourceIsNewer);
            Assert.False(first.IsSameFile);
            Assert.Equal(3, result.Completed);
            for (int i = 0; i < 3; i++) { Assert.Equal("new" + i, File.ReadAllText(temp.Combine("dst", "f" + i + ".txt"))); }
        }

        [Fact]
        public async Task Ask_SkipAllAndCustomRenameAndCancel() {
            using TempFolder temp = new TempFolder();
            string a = temp.File("src/a.txt", "A"), b = temp.File("src/b.txt", "B"), c = temp.File("src/c.txt", "C");
            temp.File("dst/a.txt", "a"); temp.File("dst/b.txt", "b"); temp.File("dst/c.txt", "c");
            Queue<FileConflictResolution> answers = new Queue<FileConflictResolution>(new[] { FileConflictResolution.Rename, FileConflictResolution.SkipAll });
            int calls = 0;
            FileOperationOptions options = new FileOperationOptions {
                ResolveConflict = (conflict, token) => {
                    calls++;
                    if (calls == 1) { conflict.RenameTarget = "renamed.txt"; }
                    return Task.FromResult(answers.Dequeue());
                }
            };
            FileOperationResult result = await FileOperation.Copy(new[] { a, b, c }, temp.Combine("dst"), options).RunAsync();
            Assert.Equal(2, calls);
            Assert.Equal("A", File.ReadAllText(temp.Combine("dst", "renamed.txt")));
            Assert.Equal("b", File.ReadAllText(temp.Combine("dst", "b.txt")));
            Assert.Equal("c", File.ReadAllText(temp.Combine("dst", "c.txt")));
            Assert.Equal(2, result.Skipped);

            FileOperationResult canceled = await FileOperation.Copy(new[] { a, b, c }, temp.Combine("dst"), new FileOperationOptions {
                ResolveConflict = (conflict, token) => Task.FromResult(FileConflictResolution.Cancel)
            }).RunAsync();
            Assert.True(canceled.Canceled);
            Assert.Equal(3, canceled.NotProcessed);
            Assert.False(canceled.Succeeded);
        }

        [Fact]
        public async Task CopyIntoTheSameFolder_IsTheSameFile_AndOnlyRenameCopies() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("a.txt", "x");
            FileConflict? seen = null;
            FileOperationResult overwrite = await FileOperation.Copy(new[] { file }, temp.Path + Path.DirectorySeparatorChar, new FileOperationOptions {
                ResolveConflict = (conflict, token) => { seen = conflict; return Task.FromResult(FileConflictResolution.Overwrite); }
            }).RunAsync();
            Assert.True(seen!.IsSameFile);
            Assert.Equal(FileOperationErrorKind.AlreadyExists, Assert.Single(overwrite.Errors).Kind);
            Assert.Equal("x", File.ReadAllText(file));
            FileOperationResult renamed = await FileOperation.Copy(new[] { file }, temp.Path + Path.DirectorySeparatorChar, new FileOperationOptions { ConflictPolicy = FileConflictPolicy.Rename }).RunAsync();
            Assert.True(renamed.Succeeded);
            Assert.True(File.Exists(temp.Combine("a (2).txt")));
        }

        [Fact]
        public async Task AFolderIsNeverCopiedOrMovedIntoItself() {
            using TempFolder temp = new TempFolder();
            string folder = temp.Folder("tree");
            temp.File("tree/a.txt");
            FileOperationResult copy = await FileOperation.Copy(new[] { folder }, temp.Combine("tree", "inside")).RunAsync();
            Assert.Equal(FileOperationErrorKind.IntoItself, Assert.Single(copy.Errors).Kind);
            FileOperationResult move = await FileOperation.Move(new[] { folder }, temp.Combine("tree") + Path.DirectorySeparatorChar).RunAsync();
            Assert.Equal(FileOperationErrorKind.IntoItself, Assert.Single(move.Errors).Kind);
            Assert.True(File.Exists(temp.Combine("tree", "a.txt")));
        }

        [Fact]
        public async Task FileInTheWayOfAFolder_IsAnErrorAndTheRestContinues() {
            using TempFolder temp = new TempFolder();
            string folder = temp.Folder("src/docs");
            string other = temp.File("src/z.txt", "z");
            temp.File("dst/docs", "a file named docs");
            FileOperationResult result = await FileOperation.Copy(new[] { folder, other }, temp.Combine("dst")).RunAsync();
            Assert.Equal(FileOperationErrorKind.AlreadyExists, Assert.Single(result.Errors).Kind);
            Assert.True(File.Exists(temp.Combine("dst", "z.txt")));
        }

        [Fact]
        public async Task Move_RenamesWithinAVolumeAndMergesIntoAnExistingFolder() {
            using TempFolder temp = new TempFolder();
            string tree = temp.Folder("src/tree");
            temp.File("src/tree/a.txt", "a");
            temp.File("src/tree/sub/b.txt", "b");
            temp.File("dst/tree/existing.txt", "e");
            FileOperationResult result = await FileOperation.Move(new[] { tree }, temp.Combine("dst")).RunAsync();
            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            Assert.False(Directory.Exists(tree));
            Assert.Equal("a", File.ReadAllText(temp.Combine("dst", "tree", "a.txt")));
            Assert.Equal("b", File.ReadAllText(temp.Combine("dst", "tree", "sub", "b.txt")));
            Assert.True(File.Exists(temp.Combine("dst", "tree", "existing.txt")));
        }

        [Fact]
        public async Task Move_ToAPlainName_RenamesInPlace_IncludingCaseOnly() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("draft.md", "d");
            Assert.True((await FileOperation.Move(new[] { file }, "final.md").RunAsync()).Succeeded);
            Assert.True(File.Exists(temp.Combine("final.md")));
            Assert.False(File.Exists(file));
            Assert.True((await FileOperation.Move(new[] { temp.Combine("final.md") }, "Final.md").RunAsync()).Succeeded);
            Assert.Contains("Final.md", Directory.GetFiles(temp.Path).Select(Path.GetFileName));
        }

        [Fact]
        public async Task Move_KeepsAFolderWhosePartsWereSkipped() {
            using TempFolder temp = new TempFolder();
            string tree = temp.Folder("src/tree");
            temp.File("src/tree/a.txt", "new");
            temp.File("src/tree/b.txt", "b");
            temp.File("dst/tree/a.txt", "old");
            FileOperationResult result = await FileOperation.Move(new[] { tree }, temp.Combine("dst"), new FileOperationOptions { ConflictPolicy = FileConflictPolicy.Skip }).RunAsync();
            Assert.True(result.Succeeded);
            Assert.True(File.Exists(temp.Combine("src", "tree", "a.txt")));
            Assert.False(File.Exists(temp.Combine("src", "tree", "b.txt")));
            Assert.Equal("old", File.ReadAllText(temp.Combine("dst", "tree", "a.txt")));
        }

        [Fact]
        public async Task Cancel_StopsAfterTheCurrentFileAndLeavesNoPartialFiles() {
            using TempFolder temp = new TempFolder();
            List<string> files = new List<string>();
            for (int i = 0; i < 6; i++) { files.Add(temp.File("src/f" + i + ".bin", new string((char)('a' + i), 300000))); }
            using CancellationTokenSource cancel = new CancellationTokenSource();
            FileOperationOptions options = new FileOperationOptions { BufferSize = 4096, ProgressInterval = TimeSpan.Zero };
            options.Progress = new Inline(progress => {
                if (progress.Phase == FileOperationPhase.Running && progress.CurrentItemBytesDone > 0) { cancel.Cancel(); }
            });
            FileOperationResult result = await FileOperation.Copy(files, temp.Combine("dst"), options).RunAsync(cancel.Token);
            Assert.True(result.Canceled);
            Assert.Equal(1, result.Completed);
            Assert.Equal(5, result.NotProcessed);
            Assert.Equal(300000, new FileInfo(temp.Combine("dst", "f0.bin")).Length);
            Assert.Single(Directory.GetFiles(temp.Combine("dst")));
        }

        [Fact]
        public async Task CancelBeforeStart_ReportsEverythingNotProcessed() {
            using TempFolder temp = new TempFolder();
            string a = temp.File("a.txt");
            using CancellationTokenSource cancel = new CancellationTokenSource();
            cancel.Cancel();
            FileOperationResult result = await FileOperation.Copy(new[] { a }, temp.Combine("dst")).RunAsync(cancel.Token);
            Assert.True(result.Canceled);
            Assert.Equal(0, result.Completed);
            Assert.False(Directory.Exists(temp.Combine("dst")));
        }

        [Fact]
        public async Task Progress_ReportsPhasesTotalsRateAndCompletion() {
            using TempFolder temp = new TempFolder();
            string tree = temp.Folder("src");
            temp.File("src/a.bin", new string('a', 70000));
            temp.File("src/b/c.bin", new string('c', 30000));
            List<FileOperationProgress> reports = new List<FileOperationProgress>();
            FileOperationOptions options = new FileOperationOptions { BufferSize = 4096, ProgressInterval = TimeSpan.Zero, Progress = new Inline(reports.Add) };
            FileOperationResult result = await FileOperation.Copy(new[] { tree }, temp.Combine("dst"), options).RunAsync();
            Assert.True(result.Succeeded);
            Assert.Equal(FileOperationPhase.Scanning, reports[0].Phase);
            Assert.Contains(reports, report => report.Phase == FileOperationPhase.Running && report.CurrentItemBytes == 70000);
            FileOperationProgress last = reports[reports.Count - 1];
            Assert.Equal(FileOperationPhase.Completed, last.Phase);
            Assert.Equal(100000, last.BytesTotal);
            Assert.Equal(100000, last.BytesDone);
            Assert.Equal(last.ItemsTotal, last.ItemsDone);
            Assert.Equal(4, last.ItemsTotal);
            Assert.Equal(100, last.Percent);
            Assert.Equal(TimeSpan.Zero, last.Remaining);
            Assert.True(reports.Select(report => report.BytesDone).SequenceEqual(reports.Select(report => report.BytesDone).OrderBy(value => value)));
        }

        [Fact]
        public async Task Move_ProgressReachesTheTotalForRenames() {
            using TempFolder temp = new TempFolder();
            string a = temp.File("src/a.txt", "a"), b = temp.Folder("src/b");
            temp.File("src/b/c.txt", "c");
            FileOperationProgress? last = null;
            FileOperationOptions options = new FileOperationOptions { ProgressInterval = TimeSpan.Zero, Progress = new Inline(progress => last = progress) };
            Assert.True((await FileOperation.Move(new[] { a, b }, temp.Combine("dst"), options).RunAsync()).Succeeded);
            Assert.Equal(last!.ItemsTotal, last.ItemsDone);
            Assert.Equal(2, last.ItemsTotal);
        }

        [Fact]
        public async Task AccessDenied_ContinuesWithTheNextItem_AndRetrySucceedsOnceFixed() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || IsRoot()) { return; }
#if NET8_0_OR_GREATER
            using TempFolder temp = new TempFolder();
            string locked = temp.Folder("src/locked");
            temp.File("src/locked/secret.txt", "s");
            string open = temp.File("src/open.txt", "o");
            File.SetUnixFileMode(locked, UnixFileMode.None);
            try {
                FileOperationResult result = await FileOperation.Copy(new[] { locked, open }, temp.Combine("dst")).RunAsync();
                FileOperationError error = Assert.Single(result.Errors);
                Assert.Equal(FileOperationErrorKind.AccessDenied, error.Kind);
                Assert.Equal(locked, error.Source);
                Assert.True(File.Exists(temp.Combine("dst", "open.txt")));
                Assert.False(result.Succeeded);
                FileOperation retry = result.RetryFailed()!;
                Assert.Equal(new[] { locked }, retry.Sources);
                File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                FileOperationResult second = await retry.RunAsync();
                Assert.True(second.Succeeded, string.Join("; ", second.Errors));
                Assert.Null(second.RetryFailed());
                Assert.Equal("s", File.ReadAllText(temp.Combine("dst", "locked", "secret.txt")));
            } finally {
                File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
#else
            await Task.CompletedTask;
#endif
        }

        [Fact]
        public async Task FileInUse_IsReportedAndTheRestContinues() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            using TempFolder temp = new TempFolder();
            string busy = temp.File("src/busy.txt", "b");
            string free = temp.File("src/free.txt", "f");
            using (new FileStream(busy, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                FileOperationResult result = await FileOperation.Copy(new[] { busy, free }, temp.Combine("dst")).RunAsync();
                Assert.Equal(FileOperationErrorKind.InUse, Assert.Single(result.Errors).Kind);
                Assert.True(File.Exists(temp.Combine("dst", "free.txt")));
            }
        }

        [Fact]
        public async Task PathTooLong_IsReportedAndTheRestContinues() {
            using TempFolder temp = new TempFolder();
            string file = temp.File("src/a.txt", "a");
            FileOperationResult result = await FileOperation.Copy(new[] { file }, temp.Combine("dst", new string('n', 300) + ".txt")).RunAsync();
            FileOperationError error = Assert.Single(result.Errors);
            Assert.Equal(FileOperationErrorKind.PathTooLong, error.Kind);
            Assert.Empty(Directory.GetFiles(temp.Combine("dst")));
        }

        [Fact]
        public async Task MissingSource_IsNotFound() {
            using TempFolder temp = new TempFolder();
            FileOperationResult result = await FileOperation.Copy(new[] { temp.Combine("ghost.txt") }, temp.Combine("dst")).RunAsync();
            Assert.Equal(FileOperationErrorKind.NotFound, Assert.Single(result.Errors).Kind);
        }

        [Fact]
        public async Task DeletePermanently_RemovesTreesAndReadOnlyFiles() {
            using TempFolder temp = new TempFolder();
            string tree = temp.Folder("tree");
            temp.File("tree/a/b/c.txt");
            string readOnly = temp.File("tree/locked.txt");
            File.SetAttributes(readOnly, FileAttributes.ReadOnly);
            string single = temp.File("single.txt");
            FileOperationResult result = await FileOperation.DeletePermanently(new[] { tree, single }).RunAsync();
            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            Assert.False(Directory.Exists(tree));
            Assert.False(File.Exists(single));
            Assert.Equal(6, result.Completed);
        }

        [Fact]
        public async Task DeletePermanently_RemovesALinkButNotWhatItPointsTo() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
#if NET8_0_OR_GREATER
            using TempFolder temp = new TempFolder();
            string outside = temp.Folder("outside");
            string kept = temp.File("outside/keep.txt", "k");
            string tree = temp.Folder("tree");
            Directory.CreateSymbolicLink(temp.Combine("tree", "link"), outside);
            FileOperationResult result = await FileOperation.DeletePermanently(new[] { tree }).RunAsync();
            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            Assert.False(Directory.Exists(tree));
            Assert.True(File.Exists(kept));
#else
            await Task.CompletedTask;
#endif
        }

        [Fact]
        public async Task Copy_RecreatesLinksInsteadOfFollowingThem() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
#if NET8_0_OR_GREATER
            using TempFolder temp = new TempFolder();
            string outside = temp.Folder("outside");
            temp.File("outside/big.txt", "content");
            string tree = temp.Folder("tree");
            Directory.CreateSymbolicLink(temp.Combine("tree", "link"), outside);
            FileOperationResult result = await FileOperation.Copy(new[] { tree }, temp.Combine("dst") + Path.DirectorySeparatorChar).RunAsync();
            Assert.True(result.Succeeded, string.Join("; ", result.Errors));
            DirectoryInfo copied = new DirectoryInfo(temp.Combine("dst", "tree", "link"));
            Assert.Equal(outside, copied.LinkTarget);
            Assert.False(File.Exists(temp.Combine("dst", "tree", "link", "big.txt")) && !copied.Attributes.HasFlag(FileAttributes.ReparsePoint));
#else
            await Task.CompletedTask;
#endif
        }

        [Fact]
        public async Task Delete_UsesTheTrash_AndNeverDeletesWhenThereIsNone() {
            using TempFolder temp = new TempFolder();
            string a = temp.File("a.txt"), b = temp.Folder("b");
            RecordingTrash trash = new RecordingTrash();
            FileOperationResult result = await FileOperation.Delete(new[] { a, b }, new FileOperationOptions { Trash = trash }).RunAsync();
            Assert.True(result.Succeeded);
            Assert.Equal(new[] { a, b }, trash.Trashed);

            string c = temp.File("c.txt");
            FileOperationResult none = await FileOperation.Delete(new[] { c }, new FileOperationOptions { Trash = FileTrash.Unavailable }).RunAsync();
            Assert.Equal(FileOperationErrorKind.TrashUnavailable, Assert.Single(none.Errors).Kind);
            Assert.True(File.Exists(c));

            FileOperationResult failing = await FileOperation.Delete(new[] { c }, new FileOperationOptions { Trash = new RecordingTrash { Failure = new IOException("Trash full") } }).RunAsync();
            Assert.Equal(FileOperationErrorKind.TrashUnavailable, Assert.Single(failing.Errors).Kind);
            Assert.True(File.Exists(c));
        }

        [Fact]
        public async Task CreateFolder_CreatesParentsAndRefusesExistingNames() {
            using TempFolder temp = new TempFolder();
            Assert.True((await FileOperation.CreateFolder(temp.Combine("a", "b", "c")).RunAsync()).Succeeded);
            Assert.True(Directory.Exists(temp.Combine("a", "b", "c")));
            FileOperationResult again = await FileOperation.CreateFolder(temp.Combine("a")).RunAsync();
            Assert.Equal(FileOperationErrorKind.AlreadyExists, Assert.Single(again.Errors).Kind);
        }

        [Fact]
        public void Planning_ValidatesInputAndTouchesNothing() {
            using TempFolder temp = new TempFolder();
            Assert.Throws<ArgumentException>(() => FileOperation.Copy(Array.Empty<string>(), temp.Path));
            Assert.Throws<ArgumentException>(() => FileOperation.Move(new[] { "a" }, " "));
            Assert.Throws<ArgumentException>(() => FileOperation.Delete(Array.Empty<string>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileOperationOptions { BufferSize = 10 });
            FileOperation.Copy(new[] { temp.Combine("a.txt") }, temp.Combine("dst"));
            Assert.False(Directory.Exists(temp.Combine("dst")));
        }

        [Fact]
        public async Task AsAnAppCommand_RunsOnTheActionRunnerAndLogsTheSummary() {
            using TempFolder temp = new TempFolder();
            string a = temp.File("a.txt", "a");
            FileOperationResult? seen = null;
            ConsoleMonster.Interactive.AppCommand command = FileOperation.Copy(new[] { a, temp.Combine("ghost.txt") }, temp.Combine("dst")).ToAppCommand("copy", "Copy", result => seen = result);
            using ConsoleMonster.Interactive.AppActionSession session = new ConsoleMonster.Interactive.AppActionSession(default);
            Assert.True(session.Start(command, Array.Empty<object?>()));
            Assert.True(SpinWait.SpinUntil(() => !session.IsBusy, TimeSpan.FromSeconds(10)));
            Assert.NotNull(seen);
            Assert.Contains(session.Output, entry => entry.Level == "err" && entry.Text.Contains("ghost.txt"));
            Assert.Contains(session.Output, entry => entry.Text.StartsWith("1 done, 1 failed", StringComparison.Ordinal));
            Assert.True(File.Exists(temp.Combine("dst", "a.txt")));
            await Task.CompletedTask;
        }

        private static bool IsRoot() => string.Equals(Environment.UserName, "root", StringComparison.Ordinal);

        internal sealed class Inline : IProgress<FileOperationProgress> {
            private readonly Action<FileOperationProgress> _report;
            internal Inline(Action<FileOperationProgress> report) { _report = report; }
            public void Report(FileOperationProgress value) => _report(value);
        }

        private sealed class RecordingTrash : ITrash {
            internal List<string> Trashed { get; } = new List<string>();
            internal Exception? Failure { get; set; }
            public string Name => "Test trash";
            public bool IsAvailable => true;
            public string? MoveToTrash(string path) {
                if (Failure != null) { throw Failure; }
                Trashed.Add(path);
                return null;
            }
        }
    }
}
