using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 3: Groups, using object initializers.</summary>
        public static Diagram GroupsInitializer() => new Diagram("Small office") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("Firewall") { Kind = DiagramNodeKind.Gateway, Subtitle = "FW01" }
            },
            Groups = {
                new DiagramGroup("Office LAN") { Nodes = {
                    new DiagramNode("12 PCs") { Kind = DiagramNodeKind.Workstation },
                    new DiagramNode("File server") { Kind = DiagramNodeKind.Storage, Subtitle = "FS01" },
                    new DiagramNode("Printer") { Kind = DiagramNodeKind.Device }
                } },
                new DiagramGroup("DMZ") { Nodes = {
                    new DiagramNode("Web server") { Kind = DiagramNodeKind.Server, Subtitle = "WEB01" }
                } },
                new DiagramGroup("Cloud") { Nodes = {
                    new DiagramNode("Microsoft 365") { Kind = DiagramNodeKind.Cloud },
                    new DiagramNode("Backup") { Kind = DiagramNodeKind.Storage, Subtitle = "Offsite" }
                } }
            },
            Links = {
                new DiagramLink("12 PCs", "Firewall") { Label = "HTTPS" },
                new DiagramLink("File server", "Backup") { Label = "nightly" },
                new DiagramLink("Firewall", "Web server") { Label = "443" },
                new DiagramLink("Firewall", "Microsoft 365") { Label = "443" }
            },
        };
    }
}
