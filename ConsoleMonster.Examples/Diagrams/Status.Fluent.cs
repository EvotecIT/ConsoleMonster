using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 4: Status, using the fluent API.</summary>
        public static Diagram StatusFluent() => Ui.Diagram("Small office · Monday 08:10")
            .Direction(DiagramDirection.Right)
            .Group("Office LAN", g => g
                .Node("12 PCs", n => n.Kind(DiagramNodeKind.Workstation))
                .Node("File server", n => n.Kind(DiagramNodeKind.Storage).Subtitle("disk 91%").Status(StatusKind.Warn))
                .Node("Printer", n => n.Kind(DiagramNodeKind.Device).Subtitle("offline").Status(StatusKind.Fail)))
            .Group("Cloud", g => g
                .Node("Microsoft 365", n => n.Kind(DiagramNodeKind.Cloud))
                .Node("Backup", n => n.Kind(DiagramNodeKind.Storage).Subtitle("Offsite").Status(StatusKind.Fail)))
            .Node("Firewall", n => n.Kind(DiagramNodeKind.Gateway))
            .Link("12 PCs", "Firewall", l => l.Label("HTTPS"))
            .Link("File server", "Backup", l => l.Label("failed 2 nights").Status(StatusKind.Fail))
            .Link("Firewall", "Microsoft 365", l => l.Label("443"));
    }
}
