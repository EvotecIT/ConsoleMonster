using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    public static partial class DiagramLadder {
        /// <summary>Level 1: Hello, using the fluent API.</summary>
        public static Diagram HelloFluent() => Ui.Diagram("")
            .Direction(DiagramDirection.Right)
            .Node("Laptop", n => n.Kind(DiagramNodeKind.Workstation))
            .Node("Router", n => n.Kind(DiagramNodeKind.Gateway))
            .Node("Internet", n => n.Kind(DiagramNodeKind.Cloud))
            .Link("Laptop", "Router")
            .Link("Router", "Internet")
            .Build;
    }
}
