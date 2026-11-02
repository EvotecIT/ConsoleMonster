using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 6: LivePing, using the fluent API.</summary>
        public static Diagram LivePingFluent() => Ui.Diagram("Ping monitor · every 5 s")
            .Direction(DiagramDirection.Right)
            .Node("This PC", n => n.Kind(DiagramNodeKind.Workstation))
            .Node("Router", n => n.Kind(DiagramNodeKind.Gateway).Value("1 ms"))
            .Node("DNS", n => n.Kind(DiagramNodeKind.Server).Subtitle("1.1.1.1").Value("12 ms"))
            .Node("VPN", n => n.Kind(DiagramNodeKind.Server).Subtitle("vpn.contoso.com").Status(StatusKind.Warn).Value("180 ms"))
            .Node("contoso.com", n => n.Kind(DiagramNodeKind.Cloud).Status(StatusKind.Fail).Value("timeout"))
            .Link("This PC", "Router")
            .Link("Router", "DNS")
            .Link("Router", "VPN")
            .Link("Router", "contoso.com")
            .Build;
    }
}
