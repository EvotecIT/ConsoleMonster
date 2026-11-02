using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

// Status uses the same words as every widget; Value says what is wrong.
var diagram = new Diagram("Small office · Monday 08:10") {
    Groups = {
        new DiagramGroup("Office LAN") {
            Nodes = {
                new DiagramNode("12 PCs") {
                    Kind = DiagramNodeKind.Workstation,
                    To = { "Firewall" }
                },
                new DiagramNode("File server") {
                    Kind = DiagramNodeKind.Storage,
                    Status = StatusKind.Warn,
                    Value = "disk 91%"
                },
                new DiagramNode("Printer") {
                    Kind = DiagramNodeKind.Device,
                    Status = StatusKind.Fail,
                    Value = "offline"
                }
            }
        },
        new DiagramGroup("Cloud") {
            Nodes = {
                new DiagramNode("Microsoft 365") { Kind = DiagramNodeKind.Cloud },
                new DiagramNode("Backup") {
                    Kind = DiagramNodeKind.Storage,
                    Status = StatusKind.Fail,
                    Value = "last success 2 days ago"
                }
            }
        }
    },
    Nodes = {
        new DiagramNode("Firewall") {
            Kind = DiagramNodeKind.Gateway,
            To = { "Microsoft 365" }
        }
    },
    Links = {
        new DiagramLink("File server", "Backup") {
            Label = "failed 2 nights",
            Status = StatusKind.Fail
        }
    }
};
Terminal.Write(diagram);
