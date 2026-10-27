using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ConsoleMonster.Apps.Commander;
using ConsoleMonster.Tests.FileSystem;
using Xunit;

namespace ConsoleMonster.Tests.Apps.Commander {
    public sealed class CommanderPanelTests {
        private static TempFolder Sample() {
            TempFolder temp = new TempFolder();
            temp.Folder("zeta");
            temp.Folder("Alpha");
            temp.File("Alpha/inner.bin", new string('i', 400));
            temp.File("zeta/a.bin", new string('a', 100));
            temp.File("zeta/deep/b.bin", new string('b', 50));
            temp.File("b.txt", new string('b', 30), new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            temp.File("A.md", new string('a', 10), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            temp.File("c.cs", new string('c', 20), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Hide(temp.File(".hidden", "h"));
            return temp;
        }

        private static readonly bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // A leading dot hides an item on macOS and Linux; Windows needs the attribute.
        private static void Hide(string path) {
            if (Windows) { File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden); }
        }

        private static CommanderPanel Open(string folder) => new CommanderPanel(new PanelState { Folder = folder, ComputeFolderSizes = false });

        private static string[] Names(CommanderPanel panel) => panel.Entries.Select(entry => entry.Name).ToArray();

        [Fact]
        public void Listing_PutsParentThenFoldersFirst_SortedByName_WithHiddenFilesHidden() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            Assert.Equal(new[] { "..", "Alpha", "zeta", "A.md", "b.txt", "c.cs" }, Names(panel));
            Assert.True(panel.Entries[0].IsParent);
            Assert.Equal(PanelEntry.ParentId, panel.Entries[0].Id);
            Assert.Equal(Path.Combine(temp.Path, "c.cs"), panel.Entries.Last().Id);
            Assert.Null(panel.Error);
            panel.SetShowHidden(true);
            Assert.Contains(".hidden", Names(panel));
        }

        [Fact]
        public void Hidden_FollowsThePlatformRule() {
            using TempFolder temp = new TempFolder();
            temp.File(".dotfile");
            string flagged = temp.File("flagged.txt");
            File.SetAttributes(flagged, FileAttributes.Hidden);
            string system = temp.File("system.txt");
            if (Windows) { File.SetAttributes(system, FileAttributes.System); }
            temp.File("plain.txt");
            using CommanderPanel panel = Open(temp.Path);
            // macOS keeps the hidden flag that .NET sets; Linux has no such flag, so only the dot counts there.
            bool flagKept = (File.GetAttributes(flagged) & FileAttributes.Hidden) != 0;
            Assert.Equal(Windows || RuntimeInformation.IsOSPlatform(OSPlatform.OSX), flagKept);
            string[] expected = Windows ? new[] { "..", ".dotfile", "plain.txt" } : flagKept ? new[] { "..", "plain.txt", "system.txt" } : new[] { "..", "flagged.txt", "plain.txt", "system.txt" };
            Assert.Equal(expected, Names(panel));
            panel.SetShowHidden(true);
            Assert.Equal(new[] { "..", ".dotfile", "flagged.txt", "plain.txt", "system.txt" }, Names(panel));
            Assert.Equal(new[] { ".dotfile", "flagged.txt", "system.txt" }.Where(name => Windows ? name != ".dotfile" : name == ".dotfile" || name == "flagged.txt" && flagKept),
                panel.Entries.Where(entry => entry.IsHidden).Select(entry => entry.Name));
            Assert.False(panel.Entries[0].IsHidden);
        }

        [Fact]
        public void Sorting_ByEveryColumnAndDirection_KeepsFoldersFirstAndTheCursorRow() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            Assert.True(panel.MoveTo(Path.Combine(temp.Path, "b.txt")));
            panel.Sort(PanelSortColumn.Size, false);
            Assert.Equal(new[] { "A.md", "c.cs", "b.txt" }, Names(panel).Skip(3));
            Assert.Equal("b.txt", panel.Current!.Name);
            panel.ToggleSort(PanelSortColumn.Size);
            Assert.True(panel.SortDescending);
            Assert.Equal(new[] { "b.txt", "c.cs", "A.md" }, Names(panel).Skip(3));
            Assert.Equal("..", Names(panel)[0]);
            panel.Sort(PanelSortColumn.Modified, true);
            Assert.Equal(new[] { "A.md", "c.cs", "b.txt" }, Names(panel).Skip(3));
            panel.Sort(PanelSortColumn.Extension, false);
            Assert.Equal(new[] { "c.cs", "A.md", "b.txt" }, Names(panel).Skip(3));
            Assert.True(panel.Entries.Skip(1).Take(2).All(entry => entry.IsFolder));
            Assert.Equal("b.txt", panel.Current!.Name);
        }

        [Fact]
        public void Marks_CountSizePatternsInvertAndTargets() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            Assert.Equal("..", panel.Current!.Name);
            Assert.Empty(panel.Targets);
            panel.ToggleMark();
            Assert.Equal(0, panel.MarkedCount);
            Assert.Equal(1, panel.CursorIndex);

            Assert.Equal(2, panel.MarkPattern("*.md;*.cs"));
            Assert.Equal(2, panel.MarkedCount);
            Assert.Equal(30, panel.MarkedSize);
            Assert.True(panel.MarkedSizeIsComplete);
            Assert.Equal(new[] { "A.md", "c.cs" }, panel.Targets.Select(entry => entry.Name));
            Assert.Equal(1, panel.UnmarkPattern("c*"));
            panel.InvertMarks();
            Assert.Equal(new[] { "b.txt", "c.cs" }, panel.MarkedEntries.Select(entry => entry.Name));
            Assert.Equal(0, panel.MarkPattern("zeta"));
            Assert.Equal(1, panel.MarkPattern("zeta", includeFolders: true));
            Assert.False(panel.MarkedSizeIsComplete);
            panel.ClearMarks();
            panel.MoveTo(Path.Combine(temp.Path, "c.cs"));
            Assert.Equal("c.cs", Assert.Single(panel.Targets).Name);
        }

        [Fact]
        public void Refresh_KeepsTheCursorRowAndSurvivingMarks() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            panel.MarkPattern("*.cs;*.md");
            panel.MoveTo(Path.Combine(temp.Path, "c.cs"));
            PanelEntry before = panel.Current!;
            temp.File("0-first.txt");
            File.Delete(Path.Combine(temp.Path, "A.md"));
            long revision = panel.Revision;
            Assert.True(panel.Refresh());
            Assert.True(panel.Revision > revision);
            Assert.Equal(before.Id, panel.Current!.Id);
            Assert.Equal(new[] { "c.cs" }, panel.MarkedEntries.Select(entry => entry.Name));
            Assert.Contains("0-first.txt", Names(panel));
        }

        [Fact]
        public void Navigation_OpensFoldersAndReturnsToTheFolderLeft() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            panel.MoveTo(Path.Combine(temp.Path, "zeta"));
            panel.MarkPattern("*.cs");
            Assert.True(panel.OpenCurrent());
            Assert.Equal(Path.Combine(temp.Path, "zeta"), panel.Folder);
            Assert.Equal(0, panel.MarkedCount);
            Assert.Equal(new[] { "..", "deep", "a.bin" }, Names(panel));
            Assert.True(panel.GoToParent());
            Assert.Equal("zeta", panel.Current!.Name);
            panel.MoveTo(Path.Combine(temp.Path, "c.cs"));
            Assert.False(panel.OpenCurrent());

            Assert.False(panel.Navigate(temp.Combine("missing")));
            Assert.NotNull(panel.Error);
            Assert.Equal(temp.Path, panel.Folder);
            Assert.True(panel.Navigate("zeta"));
            Assert.Null(panel.Error);
        }

        [Fact]
        public void ADeletedFolder_RefreshesToItsNearestParent_AndRememberedFoldersFallBack() {
            using TempFolder temp = Sample();
            string gone = temp.Folder("gone/deeper");
            using CommanderPanel panel = Open(gone);
            Directory.Delete(temp.Combine("gone"), true);
            Assert.True(panel.Refresh());
            Assert.Equal(temp.Path, panel.Folder);
            using CommanderPanel remembered = new CommanderPanel(new PanelState { Folder = temp.Combine("never", "there"), SortColumn = PanelSortColumn.Size, SortDescending = true });
            Assert.Equal(temp.Path, remembered.Folder);
            Assert.Equal(PanelSortColumn.Size, remembered.SortColumn);
            Assert.True(remembered.SortDescending);
        }

        [Fact]
        public void QuickSearch_JumpsByPrefix_RefusesMisses_AndCycles() {
            using TempFolder temp = Sample();
            temp.File("cat.txt");
            using CommanderPanel panel = Open(temp.Path);
            Assert.True(panel.AppendSearch('c'));
            Assert.Equal("c.cs", panel.Current!.Name);
            Assert.True(panel.SearchNext());
            Assert.Equal("cat.txt", panel.Current!.Name);
            Assert.True(panel.SearchNext());
            Assert.Equal("c.cs", panel.Current!.Name);
            Assert.True(panel.AppendSearch('A'));
            Assert.Equal("cat.txt", panel.Current!.Name);
            Assert.False(panel.AppendSearch('x'));
            Assert.Equal("cA", panel.QuickSearch);
            Assert.True(panel.MatchesSearch(panel.Current!));
            panel.BackspaceSearch();
            Assert.Equal("c", panel.QuickSearch);
            panel.ClearSearch();
            Assert.Equal(string.Empty, panel.QuickSearch);
            Assert.Equal("cat.txt", panel.Current!.Name);
        }

        [Fact]
        public async Task FolderSizes_ArePendingThenComputedInTheBackground() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            PanelEntry zeta = panel.Find(Path.Combine(temp.Path, "zeta"))!;
            Assert.Equal(EntrySizeState.Pending, zeta.SizeState);
            Assert.Null(zeta.Size);
            Assert.Equal(EntrySizeState.Known, panel.Find(Path.Combine(temp.Path, "c.cs"))!.SizeState);
            panel.SetMark(zeta, true);
            Assert.Equal(0, panel.MarkedSize);
            long revision = panel.Revision;
            await panel.StartFolderSizes();
            Assert.Equal(EntrySizeState.Known, zeta.SizeState);
            Assert.Equal(150, zeta.Size);
            Assert.Equal(150, panel.MarkedSize);
            Assert.True(panel.MarkedSizeIsComplete);
            Assert.True(panel.Revision > revision);
            Assert.Null(panel.Entries[0].Size);
            panel.Sort(PanelSortColumn.Size, true);
            Assert.Equal("Alpha", panel.Entries[1].Name);
        }

        [Fact]
        public async Task FolderSizes_StartAutomaticallyAndANewListingCancelsThem() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = new CommanderPanel(temp.Path);
            await panel.FolderSizes;
            Assert.All(panel.Entries.Where(entry => entry.IsFolder && !entry.IsParent), entry => Assert.Equal(EntrySizeState.Known, entry.SizeState));
            Assert.True(panel.Navigate("zeta"));
            await panel.FolderSizes;
            Assert.Equal(50, panel.Find(Path.Combine(temp.Path, "zeta", "deep"))!.Size);
        }

        [Fact]
        public async Task FolderSizes_RestartIncludesThePreviouslyComputingRow() {
            using TempFolder temp = Sample();
            using CommanderPanel panel = Open(temp.Path);
            PanelEntry folder = panel.Find(Path.Combine(temp.Path, "zeta"))!;
            // A cancelled scan resets its Computing row asynchronously. Hold that previous task
            // so restart must wait for its actual completion before taking the pending snapshot.
            folder.SetSizeState(EntrySizeState.Computing);
            var previous = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            typeof(CommanderPanel).GetProperty(nameof(CommanderPanel.FolderSizes))!.SetValue(panel, previous.Task);
            Task restarted = panel.StartFolderSizes();
            folder.SetSizeState(EntrySizeState.Pending);
            previous.SetResult(true);
            await restarted;
            Assert.Equal(EntrySizeState.Known, folder.SizeState);
            Assert.Equal(150, folder.Size);
        }

        [Fact]
        public void State_RoundTripsFoldersAndSortOnly() {
            using TempFolder temp = Sample();
            using CommanderModel model = new CommanderModel(temp.Path, temp.Combine("zeta"));
            model.Left.ComputeFolderSizes = false;
            model.Left.Sort(PanelSortColumn.Modified, true);
            model.Left.MarkPattern("*.cs");
            model.Right.SetShowHidden(true);
            model.SwitchPanels();
            string file = temp.Combine("state", "state.txt");
            model.State.Save(file);
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("c.cs", text);
            CommanderState loaded = CommanderState.Load(file);
            Assert.Equal(temp.Path, loaded.Left.Folder);
            Assert.Equal(PanelSortColumn.Modified, loaded.Left.SortColumn);
            Assert.True(loaded.Left.SortDescending);
            Assert.Equal(temp.Combine("zeta"), loaded.Right.Folder);
            Assert.True(loaded.Right.ShowHidden);
            Assert.Equal(CommanderSide.Right, loaded.ActivePanel);
            model.State.Save(file);

            File.WriteAllText(file, "garbage\nleft.sort=Nonsense\nleft.sort=7\nactive=Right\nright.folder=/x\n");
            CommanderState tolerant = CommanderState.Load(file);
            Assert.Equal(PanelSortColumn.Name, tolerant.Left.SortColumn);
            Assert.Equal(CommanderSide.Right, tolerant.ActivePanel);
            Assert.Equal("/x", tolerant.Right.Folder);
            Assert.Null(CommanderState.Load(temp.Combine("none.txt")).Left.Folder);
            Assert.EndsWith(Path.Combine("ConsoleMonster", "Commander", "state.txt"), CommanderState.DefaultPath);
        }
    }
}
