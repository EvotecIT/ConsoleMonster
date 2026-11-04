using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    internal static class DiagramConstruction {
        internal static DiagramNode Node(string label, Action<DiagramNodeBuilder>? configure) { var node = new DiagramNode(label); configure?.Invoke(new DiagramNodeBuilder(node)); return node; }
    }
    /// <summary>Configures a component without hiding its model properties.</summary>
    public sealed class DiagramNodeBuilder {
        private readonly DiagramNode _node;
        internal DiagramNodeBuilder(DiagramNode node) { _node = node; }
        /// <summary>Sets the connection identity.</summary>
        public DiagramNodeBuilder Id(string id) { _node.Id = id; return this; }
        /// <summary>Sets the displayed name.</summary>
        public DiagramNodeBuilder Label(string label) { _node.Label = label; return this; }
        /// <summary>Sets the icon's meaning.</summary>
        public DiagramNodeBuilder Kind(DiagramNodeKind kind) { _node.Kind = kind; return this; }
        /// <summary>Sets the second line.</summary>
        public DiagramNodeBuilder Subtitle(string? subtitle) { _node.Subtitle = subtitle; return this; }
        /// <summary>Sets the health.</summary>
        public DiagramNodeBuilder Status(StatusKind status) { _node.Status = status; return this; }
        /// <summary>Sets the third line.</summary>
        public DiagramNodeBuilder Value(string? value) { _node.Value = value; return this; }
        /// <summary>Adds outgoing destinations.</summary>
        public DiagramNodeBuilder To(params string[] destinations) { _node.To.AddRange(destinations); return this; }
    }
    /// <summary>Configures a connection.</summary>
    public sealed class DiagramLinkBuilder {
        private readonly DiagramLink _link;
        internal DiagramLinkBuilder(DiagramLink link) { _link = link; }
        /// <summary>Sets the source.</summary>
        public DiagramLinkBuilder From(string from) { _link.From = from; return this; }
        /// <summary>Sets the destination.</summary>
        public DiagramLinkBuilder To(string to) { _link.To = to; return this; }
        /// <summary>Sets the literal label.</summary>
        public DiagramLinkBuilder Label(string label) { _link.Label = label; return this; }
        /// <summary>Sets the health.</summary>
        public DiagramLinkBuilder Status(StatusKind status) { _link.Status = status; return this; }
        /// <summary>Sets the stroke.</summary>
        public DiagramLinkBuilder Style(DiagramLinkStyle style) { _link.Style = style; return this; }
        /// <summary>Sets arrows at both ends.</summary>
        public DiagramLinkBuilder Both(bool both = true) { _link.Both = both; return this; }
    }
    /// <summary>Configures a group and nested contents.</summary>
    public sealed class DiagramGroupBuilder {
        private readonly DiagramGroup _group;
        internal DiagramGroupBuilder(DiagramGroup group) { _group = group; }
        /// <summary>Sets the group label.</summary>
        public DiagramGroupBuilder Label(string label) { _group.Label = label; return this; }
        /// <summary>Controls the boundary.</summary>
        public DiagramGroupBuilder Boundary(bool boundary = true) { _group.Boundary = boundary; return this; }
        /// <summary>Adds a component.</summary>
        public DiagramGroupBuilder Node(string label, Action<DiagramNodeBuilder>? configure = null) { _group.Nodes.Add(DiagramConstruction.Node(label, configure)); return this; }
        /// <summary>Adds a nested group.</summary>
        public DiagramGroupBuilder Group(string label, Action<DiagramGroupBuilder> configure) { var group = new DiagramGroup(label); configure(new DiagramGroupBuilder(group)); _group.Groups.Add(group); return this; }
    }
    /// <summary>Configures a playable use case.</summary>
    public sealed class DiagramUseCaseBuilder {
        private readonly DiagramUseCase _useCase;
        internal DiagramUseCaseBuilder(DiagramUseCase useCase) { _useCase = useCase; }
        /// <summary>Sets the selection identity.</summary>
        public DiagramUseCaseBuilder Id(string id) { _useCase.Id = id; return this; }
        /// <summary>Sets the name.</summary>
        public DiagramUseCaseBuilder Label(string label) { _useCase.Label = label; return this; }
        /// <summary>Sets the playback interval in milliseconds.</summary>
        public DiagramUseCaseBuilder Delay(int milliseconds) { _useCase.Delay = milliseconds; return this; }
        /// <summary>Controls looping.</summary>
        public DiagramUseCaseBuilder Loop(bool loop = true) { _useCase.Loop = loop; return this; }
        /// <summary>Controls autoplay when the keyboard app opens.</summary>
        public DiagramUseCaseBuilder AutoPlay(bool autoplay = true) { _useCase.AutoPlay = autoplay; return this; }
        /// <summary>Adds a request, with optional payload fields.</summary>
        public DiagramUseCaseBuilder Step(string from, string to, string label, Action<DiagramStepBuilder>? configure = null) => Add(from, to, label, DiagramStepKind.Request, configure);
        /// <summary>Adds a message.</summary>
        public DiagramUseCaseBuilder Message(string from, string to, string label, Action<DiagramStepBuilder>? configure = null) => Add(from, to, label, DiagramStepKind.Message, configure);
        /// <summary>Adds a reply.</summary>
        public DiagramUseCaseBuilder Reply(string from, string to, string label, Action<DiagramStepBuilder>? configure = null) => Add(from, to, label, DiagramStepKind.Reply, configure);
        /// <summary>Adds a failed interaction.</summary>
        public DiagramUseCaseBuilder Failed(string from, string to, string label, Action<DiagramStepBuilder>? configure = null) => Add(from, to, label, DiagramStepKind.Failed, configure);
        private DiagramUseCaseBuilder Add(string from, string to, string label, DiagramStepKind kind, Action<DiagramStepBuilder>? configure) { var step = new DiagramStep(from, to, label) { Kind = kind }; configure?.Invoke(new DiagramStepBuilder(step)); _useCase.Steps.Add(step); return this; }
    }
    /// <summary>Configures a use-case interaction.</summary>
    public sealed class DiagramStepBuilder {
        private readonly DiagramStep _step;
        internal DiagramStepBuilder(DiagramStep step) { _step = step; }
        /// <summary>Sets the source.</summary>
        public DiagramStepBuilder From(string from) { _step.From = from; return this; }
        /// <summary>Sets the destination.</summary>
        public DiagramStepBuilder To(string to) { _step.To = to; return this; }
        /// <summary>Sets the label.</summary>
        public DiagramStepBuilder Label(string label) { _step.Label = label; return this; }
        /// <summary>Sets the interaction type.</summary>
        public DiagramStepBuilder Kind(DiagramStepKind kind) { _step.Kind = kind; return this; }
        /// <summary>Adds payload fields shown at this step.</summary>
        public DiagramStepBuilder Payload(IDictionary<string, object?> payload) { foreach (var entry in payload) _step.Payload[entry.Key] = entry.Value; return this; }
    }
}
