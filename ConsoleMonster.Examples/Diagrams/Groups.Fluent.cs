using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 3: Groups, using the fluent API.</summary>
        public static Diagram GroupsFluent() => Ui.Diagram("Small office")
            .Direction(DiagramDirection.Right)
            .Group("Office LAN", g => g
                .Node("12 PCs", n => n.Kind(DiagramNodeKind.Workstation))
                .Node("File server", n => n.Kind(DiagramNodeKind.Storage).Subtitle("FS01"))
                .Node("Printer", n => n.Kind(DiagramNodeKind.Device)))
            .Group("DMZ", g => g
                .Node("Web server", n => n.Kind(DiagramNodeKind.Server).Subtitle("WEB01")))
            .Group("Cloud", g => g
                .Node("Microsoft 365", n => n.Kind(DiagramNodeKind.Cloud))
                .Node("Backup", n => n.Kind(DiagramNodeKind.Storage).Subtitle("Offsite")))
            .Node("Firewall", n => n.Kind(DiagramNodeKind.Gateway).Subtitle("FW01"))
            .Link("12 PCs", "Firewall", l => l.Label("HTTPS"))
            .Link("File server", "Backup", l => l.Label("nightly"))
            .Link("Firewall", "Web server", l => l.Label("443"))
            .Link("Firewall", "Microsoft 365", l => l.Label("443"));
    }
}
