using System;
using System.Diagnostics;
using System.IO;
using ConsoleMonster.Apps.Commander;
using ConsoleMonster.Tests.FileSystem;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleMonster.Tests.Apps.Commander {
    /// <summary>Timing runs alone, so other tests' load doesn't decide whether a large folder opens without a pause.</summary>
    [CollectionDefinition("Commander timing", DisableParallelization = true)]
    public sealed class CommanderTimingCollection { }

    [Collection("Commander timing")]
    public sealed class CommanderPanelTimingTests {
        private readonly ITestOutputHelper _output;

        public CommanderPanelTimingTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void TenThousandEntries_ListSortMarkAndSearchQuickly() {
            using TempFolder temp = new TempFolder();
            for (int i = 0; i < 500; i++) { Directory.CreateDirectory(temp.Combine("folder-" + i.ToString("0000"))); }
            for (int i = 0; i < 9500; i++) { File.WriteAllBytes(temp.Combine("file-" + i.ToString("00000") + (i % 3 == 0 ? ".cs" : ".txt")), new byte[i % 7]); }

            Stopwatch watch = Stopwatch.StartNew();
            using CommanderPanel panel = new CommanderPanel(new PanelState { Folder = temp.Path, ComputeFolderSizes = false });
            TimeSpan listing = watch.Elapsed;
            Assert.Equal(10001, panel.Entries.Count);

            watch.Restart();
            panel.Sort(PanelSortColumn.Size, true);
            panel.Sort(PanelSortColumn.Modified, false);
            panel.Sort(PanelSortColumn.Name, false);
            TimeSpan sorting = watch.Elapsed;

            watch.Restart();
            int marked = panel.MarkPattern("*.cs");
            panel.InvertMarks();
            long size = panel.MarkedSize;
            TimeSpan marking = watch.Elapsed;
            Assert.Equal(3167, marked);
            Assert.Equal(9500 - 3167, panel.MarkedCount);
            Assert.True(size > 0);

            watch.Restart();
            Assert.True(panel.Search("file-09"));
            Assert.Equal("file-09000.cs", panel.Current!.Name);
            for (int i = 0; i < 100; i++) { panel.SearchNext(); }
            panel.Refresh();
            TimeSpan searchAndRefresh = watch.Elapsed;

            _output.WriteLine("10,001 rows: list " + listing.TotalMilliseconds.ToString("0") + " ms, three sorts " + sorting.TotalMilliseconds.ToString("0") + " ms, mark/invert/size " + marking.TotalMilliseconds.ToString("0") + " ms, search x101 + refresh " + searchAndRefresh.TotalMilliseconds.ToString("0") + " ms");
            // Generous ceilings for shared CI machines; a local run is far below them.
            Assert.True(listing < TimeSpan.FromSeconds(3), "Listing took " + listing);
            Assert.True(sorting < TimeSpan.FromSeconds(1), "Sorting took " + sorting);
            Assert.True(marking < TimeSpan.FromSeconds(1), "Marking took " + marking);
            Assert.True(searchAndRefresh < TimeSpan.FromSeconds(3), "Search and refresh took " + searchAndRefresh);
        }
    }
}
