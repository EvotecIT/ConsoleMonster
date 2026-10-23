using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Text;

Grid grid = new Grid("Choose nodes") { Selection = GridSelection.Multiple };
grid.AddColumn("Name").AddColumn("Region").AddColumn("Version");
grid.AddRow("api-east", "api-east", "eu-west", "2.4.1");
grid.AddRow("worker-west", "worker-west", "eu-west", "2.4.0");
grid.AddRow("build-north", "build-north", "eu-north", "2.4.1");
GridResult selected = grid.Show();
foreach (object? node in selected.Items) {
    Terminal.WriteMarkupLine(Markup.Escape(node?.ToString() ?? string.Empty));
}
