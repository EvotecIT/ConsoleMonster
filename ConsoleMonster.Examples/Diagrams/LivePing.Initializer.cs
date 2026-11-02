using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 6: LivePing, using object initializers.</summary>
        public static Diagram LivePingInitializer() => new Diagram("Ping monitor · every 5 s") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("This PC") { Kind = DiagramNodeKind.Workstation },
                new DiagramNode("Router") { Kind = DiagramNodeKind.Gateway, Value = "1 ms" },
                new DiagramNode("DNS") { Kind = DiagramNodeKind.Server, Subtitle = "1.1.1.1", Value = "12 ms" },
                new DiagramNode("VPN") { Kind = DiagramNodeKind.Server, Subtitle = "vpn.contoso.com", Status = StatusKind.Warn, Value = "180 ms" },
                new DiagramNode("contoso.com") { Kind = DiagramNodeKind.Cloud, Status = StatusKind.Fail, Value = "timeout" }
            },
            Links = {
                new DiagramLink("This PC", "Router"),
                new DiagramLink("Router", "DNS"),
                new DiagramLink("Router", "VPN"),
                new DiagramLink("Router", "contoso.com")
            },
        };
    }
}
