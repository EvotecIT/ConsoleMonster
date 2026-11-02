using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 5: WebApp, using the fluent API.</summary>
        public static Diagram WebAppFluent() => Ui.Diagram("Shop · production")
            .Direction(DiagramDirection.Right)
            .Group("App service", g => g
                .Node("Web", n => n.Kind(DiagramNodeKind.Service).Subtitle("3 instances"))
                .Node("Orders API", n => n.Kind(DiagramNodeKind.Service).Subtitle("2 instances"))
                .Node("Worker", n => n.Kind(DiagramNodeKind.Process).Subtitle("emails, invoices")))
            .Group("Data", g => g
                .Node("SQL", n => n.Kind(DiagramNodeKind.Database).Subtitle("orders"))
                .Node("Redis", n => n.Kind(DiagramNodeKind.Database).Subtitle("sessions"))
                .Node("Queue", n => n.Kind(DiagramNodeKind.Queue).Subtitle("orders.created")))
            .Node("Browser", n => n.Kind(DiagramNodeKind.User))
            .Node("CDN", n => n.Kind(DiagramNodeKind.Gateway).Subtitle("Cloudflare"))
            .Link("Browser", "CDN")
            .Link("CDN", "Web")
            .Link("Web", "Orders API")
            .Link("Orders API", "SQL")
            .Link("Web", "Redis")
            .Link("Orders API", "Queue")
            .Link("Queue", "Worker")
            .Build;
    }
}
