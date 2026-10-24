using System;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Emoji, flags and CJK in table columns: printed once (column anchoring keeps the following columns in place even
    /// where the terminal splits a ZWJ family), then live with the width probe result in the header.
    /// </summary>
    public static class GraphemeWidths {
        private const string Family = "\U0001F468\u200D\U0001F469\u200D\U0001F467";
        private const string Poland = "\U0001F1F5\U0001F1F1";
        private const string Japan = "\U0001F1EF\U0001F1F5";

        /// <summary>Builds the table.</summary>
        /// <returns>The table widget.</returns>
        public static Table BuildTable() {
            Table table = new Table { Border = TableBorder.Rounded, Title = "Owners" };
            table.AddColumn("Owner").AddColumn("Site").AddColumn("Status").AddColumn("Mailboxes", HorizontalAlignment.Right).AddColumn("Note");
            table.AddRow(new TableCell(Family + " K\u0142ys family"), new TableCell(Poland + " Warsaw"), TableCell.ForStatus(StatusKind.Pass), new TableCell("42"), new TableCell("ZWJ family"));
            table.AddRow(new TableCell("\u5F20\u4F1F (Zhang Wei)"), new TableCell(Japan + " Tokyo"), TableCell.ForStatus(StatusKind.Warn), new TableCell("7"), new TableCell("CJK name"));
            table.AddRow(new TableCell("\U0001F44D\U0001F3FD Ada"), new TableCell("London"), TableCell.ForStatus(StatusKind.Fail), new TableCell("1234"), new TableCell("skin tone"));
            table.AddRow(new TableCell("#\uFE0F\u20E3 Ops"), new TableCell("\u2764\uFE0F Krak\u00F3w"), TableCell.ForStatus(StatusKind.Pass), new TableCell("9"), new TableCell("keycap, VS16"));
            return table;
        }

        /// <summary>Prints the table once, then shows it live for a few seconds with the probe result.</summary>
        /// <param name="seconds">How long the live part runs.</param>
        public static void Run(int seconds) {
            // Detecting the terminal first applies explicit CONSOLEMONSTER_*_WIDTH settings to measurement.
            string terminal = Terminal.Profile.ToString();
            Terminal.Write(new Rows(
                new TextBlock("[bold]Printed once[/] [muted](measured with " + DisplayWidth.Profile + ")[/]"),
                BuildTable()));
            KeyValueList facts = new KeyValueList("facts")
                .Add("Terminal", terminal)
                .Add("Width probe", "running" + "\u2026");
            Spinner spinner = new Spinner("Live table with anchored columns");
            Rows root = new Rows(facts, spinner, BuildTable()) { Gap = 1 };
            using (LiveSession session = new LiveSession(root, new LiveSessionOptions { PrintFinalState = true })) {
                session.Start();
                session.Post(new LiveEvent("facts", "set", "Width probe", text: session.WidthDecision ?? "not run (" + DisplayWidth.Profile + ")"));
                session.Post(new LiveEvent("facts", "set", "Measuring with", text: DisplayWidth.Profile.ToString()));
                Thread.Sleep(TimeSpan.FromSeconds(Math.Max(1, seconds)));
                session.Post(new LiveEvent("spinner", "state", text: "columns held", status: "pass"));
                Thread.Sleep(300);
            }
        }
    }
}
