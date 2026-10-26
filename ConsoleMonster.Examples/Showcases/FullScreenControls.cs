using System;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Dates;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>A fictional operations workspace exercising retained grids, tabs, date ranges, forms and background work.</summary>
    public static class FullScreenControls {
        /// <summary>Runs the workspace. The example changes no files or services.</summary>
        public static void Show() {
            Grid Listing(string title, int count) {
                var listing = Ui.Grid().Title(title);
                listing.AddColumn(new TableColumn("Name") { IsKey = true, Priority = 10 });
                listing.AddColumn(new TableColumn("Size") { Priority = 5 });
                listing.AddColumn(new TableColumn("Modified") { Priority = 1 });
                for (int i = 0; i < count; i++) {
                    int number = i;
                    listing.AddRow(new GridRow(i, "service-" + i.ToString("D5") + (i % 3 == 0 ? ".log" : ".json"),
                        new DataGridCell(async cancellation => { await Task.Delay(100, cancellation); return (object?)(number * 1024L); }), new DateTime(2026, 10, 26)) { Id = i });
                }
                return listing;
            }
            using DataGrid left = Ui.DataGrid().Key("Left").Grid(Listing("Primary · 10,000 rows", 10000)).Height(16).Build();
            using DataGrid right = Ui.DataGrid().Key("Right").Grid(Listing("Replica", 12)).Height(16).Build();
            var regions = Ui.LayoutRegions().Add("left", left).Add("right", right);
            var dates = Ui.DatePicker().Key("Window").Selection(new DateSelectionOptions { Today = new DateTime(2026, 10, 8) }).Build();
            dates.Range = true; dates.Value = new DateRange(new DateTime(2026, 10, 8), new DateTime(2026, 10, 10));
            var log = new LogViewport("Output") { Height = 16, ShowElapsed = false };
            log.Write("This workspace uses fictional services. No files or services are changed.");
            var tabs = Ui.Tabs().Add("Services", regions).Add("Calendar", dates).Add("Output", log);
            var dialogs = new AppDialogs();
            var work = new AppCommand("check", "Check replicas", async (context, cancellation) => {
                for (int i = 1; i <= 20; i++) {
                    cancellation.ThrowIfCancellationRequested();
                    context.Report(new AppProgress("Checking replica " + i, i, 20, "Fictional check · Cancel stops remaining work"));
                    await Task.Delay(150, cancellation);
                }
                context.Summarize(new AppOperationSummary("20 replicas checked. All fictional checks passed."));
            }) { Shortcut = new KeyChord(ConsoleKey.F5), ProgressDialog = true };
            string[] labels = { "Help", "Options", "Output", "Swap", "Check", "Panel", "Calendar", "Mark", "Services", "Quit" };
            var options = new ConsoleAppOptions { Dialogs = dialogs, MinimumWidth = 40, MinimumHeight = 12, Selection = _ => left.Focused ? left.Selection : right.Selection };
            options.Keys.NextFocus = null; options.Keys.Actions = null;
            options.Keys.CancelAction = new KeyChord(ConsoleKey.Escape);
            options.Command.Add(work);
            options.Footer = new FooterBar {
                FixedSlots = true,
                HintsSource = () => labels.Select((label, i) => new KeyHint("F" + (i + 1), i == 5 ? (left.Focused ? "Right" : "Left") : label, i != 7 || tabs.SelectedIndex == 0))
            };
            options.HandleInput = (state, input) => {
                int slot = 0;
                if (input.Kind == InputEventKind.Key && input.Key >= ConsoleKey.F1 && input.Key <= ConsoleKey.F10) { slot = input.Key - ConsoleKey.F1 + 1; }
                if ((input.Modifiers & KeyModifiers.Alt) != 0 && input.Char >= '0' && input.Char <= '9') { slot = input.Char == '0' ? 10 : input.Char - '0'; }
                if (input.Is(ConsoleKey.Tab)) { if (left.Focused) { right.Focus(); } else { left.Focus(); } return true; }
                if (slot == 10) { state.RequestExit(); return true; }
                if (slot == 2) {
                    var destination = new TextInput("Destination", "replica");
                    var timestamps = new Toggle("Preserve", true) { Label = "Preserve timestamps" };
                    dialogs.Show(new FormDialog<string>("Operation options", Ui.Rows().Add(destination).Add(timestamps), values => values.Get<string>("Destination")) { InitialFocus = "Destination" }, result => {
                        if (result.Accepted) { log.Write("Destination: " + result.Value); }
                    });
                    return true;
                }
                if (slot == 3) { tabs.SelectedIndex = 2; return true; }
                if (slot == 4) { regions.Swap("left", "right"); return true; }
                if (slot == 6) { tabs.SelectedIndex = 0; if (left.Focused) { right.Focus(); } else { left.Focus(); } return true; }
                if (slot == 7) { tabs.SelectedIndex = 1; return true; }
                if (slot == 8 && tabs.SelectedIndex == 0) { (left.Focused ? left : right).MarkPattern("*.log"); return true; }
                if (slot == 9) { tabs.SelectedIndex = 0; return true; }
                if (slot == 1) { log.Write("Tab switches panel · Space marks · typing searches · Ctrl+PgUp/PgDn changes tab · F2 form · F5 background check"); tabs.SelectedIndex = 2; return true; }
                return false;
            };
            ConsoleApp.Run(Ui.Rows().Add(Ui.HeaderBar().Title("Operations workspace"), LayoutSize.Fixed(1)).Add(tabs, LayoutSize.Fraction()), options: options);
        }
    }
}
