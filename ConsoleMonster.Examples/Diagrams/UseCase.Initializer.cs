using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 7: UseCase, using object initializers.</summary>
        public static Diagram UseCaseInitializer() => new Diagram("Checkout") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("Shopper") { Kind = DiagramNodeKind.User },
                new DiagramNode("Web") { Kind = DiagramNodeKind.Service },
                new DiagramNode("Orders API") { Kind = DiagramNodeKind.Service },
                new DiagramNode("Payments") { Kind = DiagramNodeKind.External, Subtitle = "Stripe" },
                new DiagramNode("SQL") { Kind = DiagramNodeKind.Database }
            },
            Links = {
                new DiagramLink("Shopper", "Web"),
                new DiagramLink("Web", "Orders API"),
                new DiagramLink("Orders API", "Payments"),
                new DiagramLink("Orders API", "SQL")
            },
            UseCases = {
                new DiagramUseCase("Place an order") { Steps = {
                    new DiagramStep("Shopper", "Web", "POST /checkout"),
                    new DiagramStep("Web", "Orders API", "create order") { Payload = { { "sku", "MUG-01" }, { "qty", 2 } } },
                    new DiagramStep("Orders API", "Payments", "charge 24.00 EUR"),
                    new DiagramStep("Payments", "Orders API", "approved") { Kind = DiagramStepKind.Reply },
                    new DiagramStep("Orders API", "SQL", "INSERT order"),
                    new DiagramStep("Orders API", "Web", "201 order 1042") { Kind = DiagramStepKind.Reply },
                    new DiagramStep("Web", "Shopper", "Thank you page") { Kind = DiagramStepKind.Reply }
                } }
            }
        };
    }
}
