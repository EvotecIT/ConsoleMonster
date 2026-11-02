using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// A use case walks a request through the diagram; this shows its third step.
var diagram = new Diagram("Checkout") {
    Nodes = {
        new DiagramNode("Shopper") { Kind = DiagramNodeKind.User, To = { "Web" } },
        new DiagramNode("Web") { Kind = DiagramNodeKind.Service, To = { "Orders API" } },
        new DiagramNode("Orders API") {
            Kind = DiagramNodeKind.Service,
            To = { "Payments", "SQL" }
        },
        new DiagramNode("Payments") {
            Kind = DiagramNodeKind.External,
            Subtitle = "Stripe"
        },
        new DiagramNode("SQL") { Kind = DiagramNodeKind.Database }
    },
    UseCases = {
        new DiagramUseCase("Place an order") {
            Steps = {
                new DiagramStep("Shopper", "Web", "POST /checkout"),
                new DiagramStep("Web", "Orders API", "create order"),
                new DiagramStep("Orders API", "Payments", "charge 24.00 EUR"),
                new DiagramStep("Payments", "Orders API", "approved") {
                    Kind = DiagramStepKind.Reply
                },
                new DiagramStep("Orders API", "SQL", "INSERT order"),
                new DiagramStep("Orders API", "Web", "201 order 1042") {
                    Kind = DiagramStepKind.Reply
                },
                new DiagramStep("Web", "Shopper", "Thank you page") {
                    Kind = DiagramStepKind.Reply
                }
            }
        }
    }
};
diagram.Step(2);
Terminal.Write(diagram);
