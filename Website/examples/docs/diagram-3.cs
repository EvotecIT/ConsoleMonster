using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// A group is a block around its nodes.
var diagram = new Diagram("Small office") {
    Groups = {
        new DiagramGroup("Office LAN") {
            Nodes = {
                new DiagramNode("12 PCs") {
                    Kind = DiagramNodeKind.Workstation,
                    To = { "Firewall" }
                },
                new DiagramNode("File server") {
                    Kind = DiagramNodeKind.Storage,
                    Subtitle = "FS01"
                },
                new DiagramNode("Printer") { Kind = DiagramNodeKind.Device }
            }
        },
        new DiagramGroup("DMZ") {
            Nodes = {
                new DiagramNode("Web server") {
                    Kind = DiagramNodeKind.Server,
                    Subtitle = "WEB01"
                }
            }
        },
        new DiagramGroup("Cloud") {
            Nodes = {
                new DiagramNode("Microsoft 365") { Kind = DiagramNodeKind.Cloud },
                new DiagramNode("Backup") {
                    Kind = DiagramNodeKind.Storage,
                    Subtitle = "Offsite"
                }
            }
        }
    },
    Nodes = {
        new DiagramNode("Firewall") {
            Kind = DiagramNodeKind.Gateway,
            To = { "Web server", "Microsoft 365" }
        }
    },
    Links = {
        new DiagramLink("File server", "Backup") { Label = "nightly" }
    }
};
Terminal.Write(diagram);
