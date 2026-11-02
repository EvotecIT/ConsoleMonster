using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// Links that form a tree become a hierarchy, laid out top to bottom.
var diagram = new Diagram("IT team") {
    Direction = DiagramDirection.Down,
    Nodes = {
        new DiagramNode("Anna") {
            Kind = DiagramNodeKind.Person,
            Subtitle = "CIO",
            To = { "Marek", "Julia" }
        },
        new DiagramNode("Marek") {
            Kind = DiagramNodeKind.Person,
            Subtitle = "Operations",
            To = { "Tom", "Ola" }
        },
        new DiagramNode("Julia") {
            Kind = DiagramNodeKind.Person,
            Subtitle = "Development",
            To = { "Ben" }
        },
        new DiagramNode("Tom") { Kind = DiagramNodeKind.Person, Subtitle = "Service desk" },
        new DiagramNode("Ola") { Kind = DiagramNodeKind.Person, Subtitle = "Network" },
        new DiagramNode("Ben") { Kind = DiagramNodeKind.Person, Subtitle = "Apps" }
    }
};
Terminal.Write(diagram);
