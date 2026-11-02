using System;
using ConsoleMonster.Widgets;

namespace ConsoleMonster {
    public static partial class Ui {
        /// <summary>Starts a diagram chain using the same model as object initializers and PowerShell.</summary>
        public static DiagramBuilder Diagram(string title = "") => new DiagramBuilder(new Diagram(title));
    }
}

namespace ConsoleMonster.Layout {
    public sealed partial class ContentBuilder {
        /// <summary>Adds a diagram configured through its dependency-free model.</summary>
        public ContentBuilder Diagram(Action<Widgets.Diagram> configure) {
            var diagram = new Widgets.Diagram();
            configure(diagram);
            return Add(diagram);
        }
        /// <summary>Adds an existing diagram.</summary>
        public ContentBuilder Diagram(Widgets.Diagram diagram) => Add(diagram);
    }
}
