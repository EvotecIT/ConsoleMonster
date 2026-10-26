using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A native cell strip of nodes and labelled arrows, with one optional side branch.</summary>
    public sealed partial class Flow : LiveWidget {
        private readonly List<FlowNode> _nodes = new List<FlowNode>();
        private readonly List<FlowEdge> _edges = new List<FlowEdge>();

        /// <summary>Creates an empty flow with a live widget identifier.</summary>
        /// <param name="widgetId">The live event target.</param>
        public Flow(string widgetId = "flow") : base(widgetId) { }

        /// <summary>Nodes in strip order.</summary>
        public IReadOnlyList<FlowNode> Nodes => _nodes;
        /// <summary>Explicit connections; omitted adjacent connections use an unlabelled arrow.</summary>
        public IReadOnlyList<FlowEdge> Edges => _edges;
        /// <summary>The single optional branch.</summary>
        public FlowBranch? Branch {
            get => _branch;
            set {
                if (value != null && string.IsNullOrWhiteSpace(value.Label)) { throw new ArgumentException("A branch needs a label.", nameof(value)); }
                if (value != null && (Index(value.From) < 0 || value.To != null && Index(value.To) <= Index(value.From))) {
                    throw new ArgumentException("A branch needs an existing source and an optional later destination.", nameof(value));
                }
                _branch = value;
            }
        }
        private FlowBranch? _branch;

        /// <summary>Adds a node or replaces the node with the same key in place.</summary>
        /// <param name="node">The node.</param><returns>This flow.</returns>
        public Flow Add(FlowNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));
            RequireKey(node.Key, nameof(node));
            if (string.IsNullOrWhiteSpace(node.Label)) throw new ArgumentException("A node needs a label.", nameof(node));
            int index = Index(node.Key);
            if (index < 0) _nodes.Add(node); else _nodes[index] = node;
            return this;
        }

        /// <summary>Adds or replaces an edge after its adjacent nodes have been added.</summary>
        /// <param name="edge">The edge.</param><returns>This flow.</returns>
        public Flow Add(FlowEdge edge) {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            RequireKey(edge.Key, nameof(edge));
            int from = Index(edge.From), to = Index(edge.To);
            if (from < 0 || to != from + 1) throw new ArgumentException("An edge must join existing adjacent nodes in strip order.", nameof(edge));
            int existing = _edges.FindIndex(e => e.Key == edge.Key);
            if (_edges.Exists(e => e.From == edge.From && e.Key != edge.Key)) throw new ArgumentException("Only one edge may join adjacent nodes.", nameof(edge));
            if (existing < 0) _edges.Add(edge); else _edges[existing] = edge;
            return this;
        }

        /// <summary>Adds a node, like Add(new FlowNode(...)) and a New-ConsoleFlow node row.</summary>
        /// <param name="key">The stable node key.</param><param name="label">The literal label.</param><param name="status">The node status, or null.</param><returns>This flow.</returns>
        public Flow Node(string key, string label, Styling.StatusKind? status = null) => Add(new FlowNode(key, label, status));

        /// <summary>Adds an edge between adjacent nodes; its key defaults to <c>from-&gt;to</c>, as for a New-ConsoleFlow edge row.</summary>
        /// <param name="from">The left node key.</param><param name="to">The right node key.</param><param name="label">The literal edge label, or null.</param>
        /// <param name="status">The edge status, or null.</param><param name="cut">Whether the connection is severed.</param><param name="key">The stable edge key, or null for <c>from-&gt;to</c>.</param><returns>This flow.</returns>
        public Flow Edge(string from, string to, string? label = null, Styling.StatusKind? status = null, bool cut = false, string? key = null) => Add(new FlowEdge(key ?? from + "->" + to, from, to, label, status, cut));

        /// <summary>Returns a handle that posts state changes through the live dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param><returns>The handle.</returns>
        public FlowHandle Handle(ILiveDispatcher dispatcher) => new FlowHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (!StatusKinds.TryParse(liveEvent.Status, out StatusKind status)) return;
            if (liveEvent.Operation == "node") {
                int index = Index(liveEvent.Key ?? string.Empty);
                if (index >= 0) _nodes[index] = _nodes[index] with { Status = status };
            } else if (liveEvent.Operation == "edge" || liveEvent.Operation == "cut" || liveEvent.Operation == "connect") {
                int index = _edges.FindIndex(e => e.Key == liveEvent.Key);
                if (index >= 0) _edges[index] = _edges[index] with {
                    Status = status,
                    Cut = liveEvent.Operation == "cut" || (liveEvent.Operation == "edge" && _edges[index].Cut)
                };
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            Plan plan = Layout(constraints.MaxWidth, context);
            return new Size(plan.Width, constraints.IsHeightUnbounded ? plan.Height : Math.Min(plan.Height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            Plan plan = Layout(region.Width, context);
            foreach (Action<Region> draw in plan.Draw) draw(region);
        }

        private int Index(string key) => _nodes.FindIndex(n => n.Key == key);
        private FlowEdge Edge(int index) => _edges.Find(e => e.From == _nodes[index].Key) ?? new FlowEdge(string.Empty, _nodes[index].Key, _nodes[index + 1].Key);
        private static void RequireKey(string key, string parameter) {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A nonempty key is required.", parameter);
        }
    }
}
