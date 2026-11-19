using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Text;

Grid grid = new Grid("Choose nodes")
{
    OutputMode = GridSelection.Multiple
};
grid.Column("Name").Column("Region").Column("Version");
grid.Row("api-east", "api-east", "eu-west", "2.4.1");
grid.Row("worker-west", "worker-west", "eu-west", "2.4.0");
grid.Row("build-north", "build-north", "eu-north", "2.4.1");
GridResult selected = grid.Show();
foreach (object? node in selected.Items)
{
    Terminal.WriteMarkupLine(Markup.Escape(node?.ToString() ?? string.Empty));
}
