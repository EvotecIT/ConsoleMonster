using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ConsoleMonster.Graphics;

// The same architecture file ChartForgeX draws as SVG, PNG or an HTML player.
MarkupParseResult<TopologyChart> result = new MarkupArchitectureParser().Parse(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ad-replication.cfx")));
foreach (MarkupDiagnostic problem in result.Diagnostics) {
    Console.Error.WriteLine(
        $"ad-replication.cfx:{problem.Line}:{problem.Column} {problem.Code} {problem.Message}");
}
TopologyChart chart = result.Document
    ?? throw new InvalidOperationException("The file has no diagram.");

// Play the first use case; Left and Right step, Tab switches use case, Up and Down focus a node, Esc quits.
var view = new TopologyDiagram(chart);
view.Play();
view.Run();
