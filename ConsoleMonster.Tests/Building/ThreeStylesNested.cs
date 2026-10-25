using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;
using static ConsoleMonster.Ui;

// A namespace outside ConsoleMonster.Tests, as in an application: there the Ui factories (Text, Table...) are not hidden
// by the test project's own namespaces of the same names.
namespace ThreeStylesSample {
    internal static class NestedStyle {
        // 3. Nested composition: reads like the screen. With `using static ConsoleMonster.Ui`, Table(items) is the typed
        // table (Table.For would name the factory).
        public static IWidget Screen(ConsoleMonster.Tests.Building.ThreeStylesTests.Server[] servers, double[] cpu) {
            return Rows(
                Columns(
                    Panel("Servers", Table(servers)
                        .Column(s => s.Name, c => c.IsKey())
                        .Column(s => s.Role)
                        .Column(s => s.Ram, c => c.Presenter(CellPresenters.Bytes))
                        .Column(s => s.Status)
                        .Totals()),
                    Panel("Summary", Rows(
                        KeyValue(("Servers", 3), ("Failed", 1)),
                        Sparkline(cpu).Label("CPU")))
                ).Gap(1),
                Text("[muted]Updated 10:02[/]")
            ).Gap(1);
        }
    }
}
