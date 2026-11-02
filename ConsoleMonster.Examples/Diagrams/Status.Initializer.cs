using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 4: Status, using object initializers.</summary>
        public static Diagram StatusInitializer() => new Diagram("Small office · Monday 08:10") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("Firewall") { Kind = DiagramNodeKind.Gateway }
            },
            Groups = {
                new DiagramGroup("Office LAN") { Nodes = {
                    new DiagramNode("12 PCs") { Kind = DiagramNodeKind.Workstation },
                    new DiagramNode("File server") { Kind = DiagramNodeKind.Storage, Subtitle = "disk 91%", Status = StatusKind.Warn },
                    new DiagramNode("Printer") { Kind = DiagramNodeKind.Device, Subtitle = "offline", Status = StatusKind.Fail }
                } },
                new DiagramGroup("Cloud") { Nodes = {
                    new DiagramNode("Microsoft 365") { Kind = DiagramNodeKind.Cloud },
                    new DiagramNode("Backup") { Kind = DiagramNodeKind.Storage, Subtitle = "Offsite", Status = StatusKind.Fail }
                } }
            },
            Links = {
                new DiagramLink("12 PCs", "Firewall") { Label = "HTTPS" },
                new DiagramLink("File server", "Backup") { Label = "failed 2 nights", Status = StatusKind.Fail },
                new DiagramLink("Firewall", "Microsoft 365") { Label = "443" }
            },
        };
    }
}
