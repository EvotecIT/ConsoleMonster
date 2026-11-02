using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 8: OrgChart, using object initializers.</summary>
        public static Diagram OrgChartInitializer() => new Diagram("IT team") {
            Direction = DiagramDirection.Down,
            Nodes = {
                new DiagramNode("Anna") { Kind = DiagramNodeKind.Person, Subtitle = "CIO" },
                new DiagramNode("Marek") { Kind = DiagramNodeKind.Person, Subtitle = "Operations" },
                new DiagramNode("Julia") { Kind = DiagramNodeKind.Person, Subtitle = "Development" },
                new DiagramNode("Tom") { Kind = DiagramNodeKind.Person, Subtitle = "Service desk" },
                new DiagramNode("Ola") { Kind = DiagramNodeKind.Person, Subtitle = "Network" },
                new DiagramNode("Ben") { Kind = DiagramNodeKind.Person, Subtitle = "Apps" }
            },
            Links = {
                new DiagramLink("Anna", "Marek"),
                new DiagramLink("Anna", "Julia"),
                new DiagramLink("Marek", "Tom"),
                new DiagramLink("Marek", "Ola"),
                new DiagramLink("Julia", "Ben")
            },
        };
    }
}
