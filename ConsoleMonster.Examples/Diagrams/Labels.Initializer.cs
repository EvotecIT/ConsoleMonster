using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 2: Labels, using object initializers.</summary>
        public static Diagram LabelsInitializer() => new Diagram("Home network") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("Laptop") { Kind = DiagramNodeKind.Workstation, Subtitle = "Wi-Fi" },
                new DiagramNode("TV") { Kind = DiagramNodeKind.Workstation, Subtitle = "Ethernet" },
                new DiagramNode("NAS") { Kind = DiagramNodeKind.Storage, Subtitle = "Photos, backups" },
                new DiagramNode("Router") { Kind = DiagramNodeKind.Gateway, Subtitle = "192.168.1.1" },
                new DiagramNode("Internet") { Kind = DiagramNodeKind.Cloud, Subtitle = "Fibre 1 Gb" }
            },
            Links = {
                new DiagramLink("Laptop", "Router") { Label = "Wi-Fi" },
                new DiagramLink("TV", "Router") { Label = "1 Gb" },
                new DiagramLink("NAS", "Router") { Label = "1 Gb" },
                new DiagramLink("Router", "Internet") { Label = "WAN" }
            },
        };
    }
}
