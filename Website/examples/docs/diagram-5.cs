using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// Rows you already have; nodes are created from the links.
Link[] links = {
    new("Browser", "CDN"),
    new("CDN", "Web"),
    new("Web", "Orders API"),
    new("Orders API", "SQL"),
    new("Web", "Redis"),
    new("Orders API", "Queue"),
    new("Queue", "Worker")
};
Diagram diagram = Diagram.FromLinks(links, link => link.From, link => link.To);
diagram.Title = "Shop · production";
Terminal.Write(diagram);

record Link(string From, string To);
