using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// The values a live session would keep updating; the layout never moves.
var diagram = new Diagram("Ping monitor · every 5 s") {
    Nodes = {
        new DiagramNode("This PC") {
            Kind = DiagramNodeKind.Workstation,
            To = { "Router" }
        },
        new DiagramNode("Router") {
            Kind = DiagramNodeKind.Gateway,
            Value = "1 ms",
            To = { "DNS", "VPN", "contoso.com" }
        },
        new DiagramNode("DNS") {
            Kind = DiagramNodeKind.Server,
            Subtitle = "1.1.1.1",
            Value = "12 ms"
        },
        new DiagramNode("VPN") {
            Kind = DiagramNodeKind.Server,
            Subtitle = "10.8.0.1",
            Status = StatusKind.Warn,
            Value = "180 ms"
        },
        new DiagramNode("contoso.com") {
            Kind = DiagramNodeKind.Cloud,
            Status = StatusKind.Fail,
            Value = "timeout"
        }
    }
};
Terminal.Write(diagram);
