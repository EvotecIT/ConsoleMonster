using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>The reading direction of a diagram.</summary>
    public enum DiagramDirection { /// <summary>Left to right.</summary>
        Right, /// <summary>Top to bottom.</summary>
        Down, /// <summary>Right to left.</summary>
        Left, /// <summary>Bottom to top.</summary>
        Up }

    /// <summary>The meaning of a node, independent of a rendering engine.</summary>
    public enum DiagramNodeKind {
        /// <summary>A general component.</summary>
        Service,
        /// <summary>A server.</summary>
        Server,
        /// <summary>A person's computer.</summary>
        Workstation,
        /// <summary>A router or gateway.</summary>
        Gateway,
        /// <summary>A cloud resource.</summary>
        Cloud,
        /// <summary>A storage resource.</summary>
        Storage,
        /// <summary>A database.</summary>
        Database,
        /// <summary>A queue.</summary>
        Queue,
        /// <summary>A person using a system.</summary>
        User,
        /// <summary>A person in a hierarchy.</summary>
        Person,
        /// <summary>An application.</summary>
        App,
        /// <summary>A background process.</summary>
        Process,
        /// <summary>A device.</summary>
        Device,
        /// <summary>An external system.</summary>
        External
    }

    /// <summary>The stroke of a connection.</summary>
    public enum DiagramLinkStyle { /// <summary>A continuous stroke.</summary>
        Solid, /// <summary>A dashed stroke.</summary>
        Dashed, /// <summary>A dotted stroke.</summary>
        Dotted }

    /// <summary>One component. Its label is also its identity unless Id is explicitly set.</summary>
    public sealed class DiagramNode {
        private string? _id;
        /// <summary>Creates a component with a literal label.</summary>
        public DiagramNode(string label) { Label = label; }
        /// <summary>The connection and live-update key; defaults to Label.</summary>
        public string Id { get => _id ?? Label; set => _id = value; }
        /// <summary>The name on the first line.</summary>
        public string Label { get; set; }
        /// <summary>The component kind and icon.</summary>
        public DiagramNodeKind Kind { get; set; }
        /// <summary>The second line, retained when a live value is present.</summary>
        public string? Subtitle { get; set; }
        private StatusKind _status = StatusKind.None;
        internal bool HasStatus { get; private set; }
        /// <summary>The component's health. Omit it to show no status marker.</summary>
        public StatusKind Status { get => _status; set { _status = value; HasStatus = true; } }
        /// <summary>The third line, independent of Subtitle.</summary>
        public string? Value { get; set; }
        /// <summary>Destination ids for unlabelled outgoing connections.</summary>
        public List<string> To { get; } = new List<string>();
    }

    /// <summary>A directed connection; undeclared endpoint ids create labelled nodes.</summary>
    public sealed class DiagramLink {
        /// <summary>Connects two node ids.</summary>
        public DiagramLink(string from, string to) { From = from; To = to; }
        /// <summary>The source node id.</summary>
        public string From { get; set; }
        /// <summary>The destination node id.</summary>
        public string To { get; set; }
        /// <summary>The connection's literal label.</summary>
        public string? Label { get; set; }
        private StatusKind _status = StatusKind.None;
        internal bool HasStatus { get; private set; }
        /// <summary>The connection's health. Omit it to show no status marker.</summary>
        public StatusKind Status { get => _status; set { _status = value; HasStatus = true; } }
        /// <summary>The connection's stroke.</summary>
        public DiagramLinkStyle Style { get; set; }
        /// <summary>Whether arrows point in both directions.</summary>
        public bool Both { get; set; }
    }

    /// <summary>A labelled group, optionally containing further groups.</summary>
    public sealed class DiagramGroup {
        /// <summary>Creates a group.</summary>
        public DiagramGroup(string label) { Label = label; }
        /// <summary>The group's literal label.</summary>
        public string Label { get; set; }
        /// <summary>Components directly in this group.</summary>
        public List<DiagramNode> Nodes { get; } = new List<DiagramNode>();
        /// <summary>Nested groups.</summary>
        public List<DiagramGroup> Groups { get; } = new List<DiagramGroup>();
        /// <summary>Whether the topology renderer draws the boundary.</summary>
        public bool Boundary { get; set; } = true;
    }

    /// <summary>The interaction represented by a use-case step.</summary>
    public enum DiagramStepKind { /// <summary>A request.</summary>
        Request, /// <summary>A message.</summary>
        Message, /// <summary>A reply.</summary>
        Reply, /// <summary>A failed interaction.</summary>
        Failed }

    /// <summary>One interaction in a use case.</summary>
    public sealed class DiagramStep {
        /// <summary>Creates a labelled interaction between node ids.</summary>
        public DiagramStep(string from, string to, string label) { From = from; To = to; Label = label; }
        /// <summary>The source node id.</summary>
        public string From { get; set; }
        /// <summary>The destination node id.</summary>
        public string To { get; set; }
        /// <summary>The interaction's literal label.</summary>
        public string Label { get; set; }
        /// <summary>The interaction type.</summary>
        public DiagramStepKind Kind { get; set; }
        /// <summary>Optional payload fields shown with the active step.</summary>
        public Dictionary<string, object?> Payload { get; } = new Dictionary<string, object?>();
    }

    /// <summary>A sequence of interactions that can be stepped or played.</summary>
    public sealed class DiagramUseCase {
        private string? _id;
        /// <summary>Creates a use case.</summary>
        public DiagramUseCase(string label) { Label = label; }
        /// <summary>The selection key; defaults to Label.</summary>
        public string Id { get => _id ?? Label; set => _id = value; }
        /// <summary>The use case's name.</summary>
        public string Label { get; set; }
        /// <summary>The ordered interactions.</summary>
        public List<DiagramStep> Steps { get; } = new List<DiagramStep>();
        /// <summary>The playback interval in milliseconds.</summary>
        public int Delay { get; set; } = 900;
        /// <summary>Whether playback repeats.</summary>
        public bool Loop { get; set; }
        /// <summary>Whether playback starts when the keyboard view opens.</summary>
        public bool AutoPlay { get; set; }
    }
}
