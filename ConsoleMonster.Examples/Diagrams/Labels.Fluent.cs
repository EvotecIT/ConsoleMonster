using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 2: Labels, using the fluent API.</summary>
        public static Diagram LabelsFluent() => Ui.Diagram("Home network")
            .Direction(DiagramDirection.Right)
            .Node("Laptop", n => n.Kind(DiagramNodeKind.Workstation).Subtitle("Wi-Fi"))
            .Node("TV", n => n.Kind(DiagramNodeKind.Workstation).Subtitle("Ethernet"))
            .Node("NAS", n => n.Kind(DiagramNodeKind.Storage).Subtitle("Photos, backups"))
            .Node("Router", n => n.Kind(DiagramNodeKind.Gateway).Subtitle("192.168.1.1"))
            .Node("Internet", n => n.Kind(DiagramNodeKind.Cloud).Subtitle("Fibre 1 Gb"))
            .Link("Laptop", "Router", l => l.Label("Wi-Fi"))
            .Link("TV", "Router", l => l.Label("1 Gb"))
            .Link("NAS", "Router", l => l.Label("1 Gb"))
            .Link("Router", "Internet", l => l.Label("WAN"));
    }
}
