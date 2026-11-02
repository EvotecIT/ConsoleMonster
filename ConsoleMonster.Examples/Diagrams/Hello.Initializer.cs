using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 1: Hello, using object initializers.</summary>
        public static Diagram HelloInitializer() => new Diagram("") {
            Direction = DiagramDirection.Right,
            Nodes = {
                new DiagramNode("Laptop") { Kind = DiagramNodeKind.Workstation },
                new DiagramNode("Router") { Kind = DiagramNodeKind.Gateway },
                new DiagramNode("Internet") { Kind = DiagramNodeKind.Cloud }
            },
            Links = {
                new DiagramLink("Laptop", "Router"),
                new DiagramLink("Router", "Internet")
            },
        };
    }
}
