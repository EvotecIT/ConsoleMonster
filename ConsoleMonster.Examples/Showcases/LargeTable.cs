using System;
using System.Diagnostics;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Measures print-once output of a large table: time, managed bytes allocated and peak working set while a table of
    /// many rows is written to a writer that discards the text (so only rendering is measured).
    /// </summary>
    public static class LargeTable {
        /// <summary>Builds a table of invented rows.</summary>
        /// <param name="rows">Row count.</param>
        /// <returns>The table.</returns>
        public static Table Build(int rows) {
            Table table = new Table { Title = "Inventory" };
            table.AddColumn("Name");
            table.AddColumn("Site");
            table.AddColumn(new TableColumn("Size") { Alignment = Layout.HorizontalAlignment.Right });
            table.AddColumn("Modified");
            table.AddColumn("Owner");
            table.AddColumn("Path");
            string[] sites = { "Warsaw", "Berlin", "Dublin", "Lab" };
            DateTime start = new DateTime(2026, 1, 1);
            for (int i = 0; i < rows; i++) {
                table.AddPlainRow("file-" + i.ToString("00000", CultureInfo.InvariantCulture) + ".log", sites[i % 4], ((i * 7919) % 100000).ToString("N0", CultureInfo.InvariantCulture) + " KB",
                    start.AddMinutes(i * 13).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), "svc-" + (i % 17).ToString(CultureInfo.InvariantCulture), @"D:\Data\Logs\" + sites[i % 4] + @"\" + (i % 100).ToString(CultureInfo.InvariantCulture));
            }
            return table;
        }

        /// <summary>Writes the table and reports the cost.</summary>
        /// <param name="rows">Row count.</param>
        /// <returns>A one-line report.</returns>
        public static string Measure(int rows) {
            Table table = Build(rows);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Process process = Process.GetCurrentProcess();
            process.Refresh();
            long workingBefore = process.WorkingSet64;
            long allocatedBefore = Allocated();
            Stopwatch watch = Stopwatch.StartNew();
            TerminalProfile profile = TerminalProfile.Fixed(140, 40, ColorDepth.TrueColor, GlyphTier.Full);
            Terminal.Write(table, profile, new DiscardWriter());
            watch.Stop();
            process.Refresh();
            long peak = process.PeakWorkingSet64;
            return string.Format(CultureInfo.InvariantCulture, "rows={0} time={1:0.00}s allocated={2:0}MB peakWorkingSetAboveStart={3:0}MB", rows, watch.Elapsed.TotalSeconds, (Allocated() - allocatedBefore) / 1048576.0, (peak - workingBefore) / 1048576.0);
        }

        private static long Allocated() {
#if NET5_0_OR_GREATER
            return GC.GetTotalAllocatedBytes(true);
#else
            return GC.GetTotalMemory(false);
#endif
        }

        private sealed class DiscardWriter : ITerminalWriter {
            public bool IsConsole => false;

            public void Write(string text) {
            }

            public void Flush() {
            }

            public void Dispose() {
            }
        }
    }
}
