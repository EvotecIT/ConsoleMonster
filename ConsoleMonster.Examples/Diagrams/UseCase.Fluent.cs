using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 7: UseCase, using the fluent API.</summary>
        public static Diagram UseCaseFluent() => Ui.Diagram("Checkout")
            .Direction(DiagramDirection.Right)
            .Node("Shopper", n => n.Kind(DiagramNodeKind.User))
            .Node("Web", n => n.Kind(DiagramNodeKind.Service))
            .Node("Orders API", n => n.Kind(DiagramNodeKind.Service))
            .Node("Payments", n => n.Kind(DiagramNodeKind.External).Subtitle("Stripe"))
            .Node("SQL", n => n.Kind(DiagramNodeKind.Database))
            .Link("Shopper", "Web")
            .Link("Web", "Orders API")
            .Link("Orders API", "Payments")
            .Link("Orders API", "SQL")
            .UseCase("Place an order", u => u
                .Step("Shopper", "Web", "POST /checkout")
                .Step("Web", "Orders API", "create order", s => s.Payload(new System.Collections.Generic.Dictionary<string, object?> { { "sku", "MUG-01" }, { "qty", 2 } }))
                .Step("Orders API", "Payments", "charge 24.00 EUR")
                .Reply("Payments", "Orders API", "approved")
                .Step("Orders API", "SQL", "INSERT order")
                .Reply("Orders API", "Web", "201 order 1042")
                .Reply("Web", "Shopper", "Thank you page"))
            .Build;
    }
}
