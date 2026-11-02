using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 5: WebApp, using object initializers.</summary>
        public static Diagram WebAppInitializer() => new Diagram("Shop · production") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("Browser") { Kind = DiagramNodeKind.User },
                new DiagramNode("CDN") { Kind = DiagramNodeKind.Gateway, Subtitle = "Cloudflare" }
            },
            Groups = {
                new DiagramGroup("App service") { Nodes = {
                    new DiagramNode("Web") { Kind = DiagramNodeKind.Service, Subtitle = "3 instances" },
                    new DiagramNode("Orders API") { Kind = DiagramNodeKind.Service, Subtitle = "2 instances" },
                    new DiagramNode("Worker") { Kind = DiagramNodeKind.Process, Subtitle = "emails, invoices" }
                } },
                new DiagramGroup("Data") { Nodes = {
                    new DiagramNode("SQL") { Kind = DiagramNodeKind.Database, Subtitle = "orders" },
                    new DiagramNode("Redis") { Kind = DiagramNodeKind.Database, Subtitle = "sessions" },
                    new DiagramNode("Queue") { Kind = DiagramNodeKind.Queue, Subtitle = "orders.created" }
                } }
            },
            Links = {
                new DiagramLink("Browser", "CDN"),
                new DiagramLink("CDN", "Web"),
                new DiagramLink("Web", "Orders API"),
                new DiagramLink("Orders API", "SQL"),
                new DiagramLink("Web", "Redis"),
                new DiagramLink("Orders API", "Queue"),
                new DiagramLink("Queue", "Worker")
            },
        };
    }
}
