using System;

namespace ConsoleMonster.Widgets {
    public sealed partial class Diagram {
        /// <summary>Controls the attention summary and returns this diagram.</summary>
        public Diagram Summary(bool show = true) { ShowSummary = show; return this; }

        /// <summary>Adds a component and returns this diagram.</summary>
        public Diagram Node(string label, Action<DiagramNodeBuilder>? configure = null) {
            Nodes.Add(DiagramConstruction.Node(label, configure));
            return this;
        }

        /// <summary>Adds a connection and returns this diagram.</summary>
        public Diagram Link(string from, string to, Action<DiagramLinkBuilder>? configure = null) {
            var link = new DiagramLink(from, to);
            configure?.Invoke(new DiagramLinkBuilder(link));
            Links.Add(link);
            return this;
        }

        /// <summary>Adds a group and returns this diagram.</summary>
        public Diagram Group(string label, Action<DiagramGroupBuilder> configure) {
            var group = new DiagramGroup(label);
            configure(new DiagramGroupBuilder(group));
            Groups.Add(group);
            return this;
        }

        /// <summary>Adds a use case and returns this diagram.</summary>
        public Diagram UseCase(string label, Action<DiagramUseCaseBuilder> configure) {
            var useCase = new DiagramUseCase(label);
            configure(new DiagramUseCaseBuilder(useCase));
            UseCases.Add(useCase);
            return this;
        }
    }
}
