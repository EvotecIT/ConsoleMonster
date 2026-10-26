using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

namespace ThreeStylesSample {
    internal static class NestedStyle {
        public static IWidget Screen(ConsoleMonster.Tests.Building.ThreeStylesTests.Server[] servers, double[] cpu) =>
            Ui.Rows().Gap(1).Content(rows => {
                rows.Columns(columns => columns.Gap(1).Content(panels => {
                    panels.Panel(panel => panel.Title("Servers").Content(content => content.Add(Ui.Table(servers)
                        .Column(s => s.Name, c => c.Key()).Column(s => s.Role)
                        .Column(s => s.Ram, c => c.Presenter(CellPresenters.Bytes)).Column(s => s.Status).Totals())));
                    panels.Panel(panel => panel.Title("Summary").Content(content => {
                        content.KeyValue(list => list.AddValue("Servers", 3).AddValue("Failed", 1));
                        content.Chart(chart => chart.Type(ChartType.Sparkline).Label("CPU").Values(cpu));
                    }));
                }));
                rows.Markup("[muted]Updated 10:02[/]");
            });
    }
}
