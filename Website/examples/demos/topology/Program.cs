using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

// ConsoleMonster.Graphics draws diagrams with the ChartForgeX layout; without it a diagram prints as a tree.
DiagramRendererProvider.Register();

// The same architecture file ChartForgeX draws as SVG, PNG or an HTML player.
Diagram diagram = Diagram.Load(Path.Combine(AppContext.BaseDirectory, "ad-replication.cfx"));

// Play the first use case; Left and Right step, Tab switches use case, Up and Down focus a node, Esc quits.
diagram.Play();
diagram.Run();
