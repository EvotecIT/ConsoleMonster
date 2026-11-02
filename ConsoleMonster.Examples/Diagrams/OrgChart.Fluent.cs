using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 8: OrgChart, using the fluent API.</summary>
        public static Diagram OrgChartFluent() => Ui.Diagram("IT team")
            .Direction(DiagramDirection.Down)
            .Node("Anna", n => n.Kind(DiagramNodeKind.Person).Subtitle("CIO"))
            .Node("Marek", n => n.Kind(DiagramNodeKind.Person).Subtitle("Operations"))
            .Node("Julia", n => n.Kind(DiagramNodeKind.Person).Subtitle("Development"))
            .Node("Tom", n => n.Kind(DiagramNodeKind.Person).Subtitle("Service desk"))
            .Node("Ola", n => n.Kind(DiagramNodeKind.Person).Subtitle("Network"))
            .Node("Ben", n => n.Kind(DiagramNodeKind.Person).Subtitle("Apps"))
            .Link("Anna", "Marek")
            .Link("Anna", "Julia")
            .Link("Marek", "Tom")
            .Link("Marek", "Ola")
            .Link("Julia", "Ben")
            .Build;
    }
}
