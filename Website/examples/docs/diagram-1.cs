using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// A node can point at other nodes with To.
var diagram = new Diagram {
    Nodes = {
        new DiagramNode("Laptop") { To = { "Router" } },
        new DiagramNode("Router") { To = { "Internet" } },
        new DiagramNode("Internet")
    }
};
Terminal.Write(diagram);
