using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Diagram {
        internal IEnumerable<DiagramNode> AllNodes() => Nodes.Concat(Groups.SelectMany(g => GroupNodes(g, new HashSet<DiagramGroup>())));
        private static IEnumerable<DiagramNode> GroupNodes(DiagramGroup group, HashSet<DiagramGroup> path) {
            if (!path.Add(group)) throw new ArgumentException("Diagram groups cannot contain themselves.");
            foreach (DiagramNode node in group.Nodes) yield return node;
            foreach (DiagramGroup child in group.Groups) foreach (DiagramNode node in GroupNodes(child, path)) yield return node;
            path.Remove(group);
        }
        internal IEnumerable<DiagramLink> AllLinks() => Links.Concat(AllNodes().SelectMany(n => n.To.Select(to => new DiagramLink(n.Id, to))));
        internal List<DiagramNode> Resolve() { lock (Gate) { return ResolveCore(); } }
        private List<DiagramNode> ResolveCore() {
            var result = AllNodes().ToList();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (DiagramNode node in result) {
                if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Label)) throw new ArgumentException("A diagram node needs a nonempty id and label.");
                if (!seen.Add(node.Id)) throw new ArgumentException("Duplicate diagram node id: " + node.Id);
            }
            void Endpoint(string id) {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A diagram connection needs two node ids.");
                if (seen.Add(id)) { var node = new DiagramNode(id); Nodes.Add(node); result.Add(node); }
            }
            foreach (DiagramLink link in AllLinks().ToList()) { Endpoint(link.From); Endpoint(link.To); }
            var useCases = new HashSet<string>(StringComparer.Ordinal);
            foreach (DiagramUseCase useCase in UseCases) {
                if (string.IsNullOrWhiteSpace(useCase.Id) || !useCases.Add(useCase.Id)) throw new ArgumentException("Use case ids must be nonempty and unique: " + useCase.Id);
                foreach (DiagramStep step in useCase.Steps) { Endpoint(step.From); Endpoint(step.To); }
            }
            return result;
        }
        private static string Literal(string? text, RenderContext context) => Markup.Escape(DiagramText.ForTier(text ?? string.Empty, context));
        internal Dictionary<string, DiagramLink> ForestParents(IReadOnlyList<DiagramLink> links) {
            var parents = new Dictionary<string, DiagramLink>(StringComparer.Ordinal);
            if (Groups.Count == 0) foreach (DiagramLink link in links.Where(link => !link.Both)) {
                if (parents.ContainsKey(link.To)) continue;
                string ancestor = link.From;
                while (ancestor != link.To && parents.TryGetValue(ancestor, out DiagramLink? previous)) ancestor = previous.From;
                if (ancestor != link.To) parents.Add(link.To, link);
            }
            return parents;
        }
        private sealed class TreeSnapshot : IWidget {
            private readonly Diagram _diagram;
            internal TreeSnapshot(Diagram diagram) { _diagram = diagram; }
            public Size Measure(Constraints constraints, RenderContext context) => _diagram.TreeView(context).Measure(constraints, context);
            public void Render(Region region, RenderContext context) => _diagram.TreeView(context).Render(region, context);
        }
        internal IWidget TreeView(RenderContext? context = null) {
            if (context == null) return new TreeSnapshot(this);
            string Literal(string? text) => Diagram.Literal(text, context);
            List<DiagramNode> nodes = Resolve();
            List<DiagramLink> links = AllLinks().ToList();
            var tree = new Tree(string.IsNullOrEmpty(Title) ? null : Literal(Title)) { WrapLabels = true };
            string Arrow(bool both) => context.Profile.Glyphs == Capabilities.GlyphTier.Ascii ? both ? "<->" : "->" : both ? "↔" : "→";
            var parents = ForestParents(links);
            void AddNode(TreeNode parent, DiagramNode node, DiagramLink? incoming = null) {
                string label = Literal((FocusNodeId == node.Id ? "> " : "") + node.Label);
                if (incoming != null && incoming.HasStatus && incoming.Status != StatusKind.Pass)
                    label += " [" + incoming.Status.ToToken().ToString().ToLowerInvariant() + "]" + Literal(context.Glyphs.StatusSymbol(incoming.Status)) + "[/]";
                string detail = string.Join("  ", new[] { node.Subtitle, node.Value, incoming?.Label }.Where(value => !string.IsNullOrEmpty(value)));
                TreeNode item = parent.Add(label, node.HasStatus ? node.Status : (StatusKind?)null, DiagramText.ForTier(detail, context));
                foreach (DiagramLink link in links.Where(l => l.From == node.Id)) {
                    if (parents.TryGetValue(link.To, out DiagramLink? nested) && ReferenceEquals(nested, link)) {
                        AddNode(item, nodes.First(n => n.Id == link.To), link);
                        continue;
                    }
                    string target = nodes.First(n => n.Id == link.To).Label;
                    item.Add(Literal(Arrow(link.Both) + " " + target), link.HasStatus && link.Status != StatusKind.Pass ? link.Status : (StatusKind?)null,
                        DiagramText.ForTier(link.Label ?? "", context));
                }
            }
            void AddGroup(TreeNode parent, DiagramGroup group) {
                TreeNode item = parent.Add(Literal(group.Label));
                foreach (DiagramNode node in group.Nodes) AddNode(item, node);
                foreach (DiagramGroup child in group.Groups) AddGroup(item, child);
            }
            foreach (DiagramGroup group in Groups) AddGroup(tree.Root, group);
            foreach (DiagramNode node in Nodes.Where(node => !parents.ContainsKey(node.Id))) AddNode(tree.Root, node);
            var widgets = new List<IWidget> { tree };
            IWidget? legend = Legend(context);
            if (legend != null) widgets.Add(legend);
            IWidget? summary = Attention(context);
            if (summary != null) widgets.Add(summary);
            IWidget? panels = Renderer?.Panels(context);
            if (panels != null) widgets.Add(panels);
            DiagramUseCase? useCase = Renderer == null ? UseCases.FirstOrDefault() : null;
            if (ShowStepPanel && useCase != null) {
                var steps = new Tree(Literal(useCase.Label)) { WrapLabels = true };
                int? active = StepIndex;
                int visible = Math.Min(useCase.Steps.Count, Math.Max(1, MaxStepLines));
                for (int index = 0; index < visible; index++) {
                    DiagramStep step = useCase.Steps[index];
                    string text = (index == active ? "> " : "") + (index + 1) + ". " + step.From + " -> " + step.To + " : " + step.Label + " (" + step.Kind.ToString().ToLowerInvariant() + ")";
                    TreeNode row = steps.Add(Literal(text), step.Kind == DiagramStepKind.Failed ? StatusKind.Fail : (StatusKind?)null);
                    if (index == active) foreach (var field in step.Payload.Take(MaxPayloadLines)) row.Add(Literal(field.Key + ": " + field.Value));
                }
                if (visible < useCase.Steps.Count) steps.Add((useCase.Steps.Count - visible) + " more steps");
                widgets.Add(steps);
            }
            return widgets.Count == 1 ? tree : new Rows(widgets.ToArray());
        }
        internal IWidget? Attention(RenderContext context) {
            string Literal(string? text) => Diagram.Literal(text, context);
            if (!ShowSummary) return null;
            List<DiagramNode> nodes = Resolve();
            var summary = new Tree("[bold]Needs attention[/]") { WrapLabels = true };
            var links = AllLinks().ToList();
            foreach (DiagramNode node in nodes.Where(n => n.HasStatus && n.Status != StatusKind.Pass)) {
                string? problem = !string.IsNullOrEmpty(node.Value) ? node.Value : node.Subtitle;
                TreeNode row = summary.Add(Literal(node.Label), node.Status, DiagramText.ForTier(problem ?? "", context));
                row.Bare = true;
            }
            foreach (DiagramLink link in links.Where(l => l.HasStatus && l.Status != StatusKind.Pass)) {
                string from = nodes.First(n => n.Id == link.From).Label, to = nodes.First(n => n.Id == link.To).Label;
                string arrow = context.Profile.Glyphs == Capabilities.GlyphTier.Ascii ? link.Both ? " <-> " : " -> " : link.Both ? " ↔ " : " → ";
                TreeNode row = summary.Add(Literal(from + arrow + to), link.Status, DiagramText.ForTier(link.Label ?? "", context));
                row.Bare = true;
            }
            return summary.Root.Children.Count == 0 ? null : summary;
        }
        internal IWidget? Legend(RenderContext context) {
            if (!ShowLegend) return null;
            var statuses = Resolve().Where(node => node.HasStatus).Select(node => node.Status)
                .Concat(AllLinks().Where(link => link.HasStatus).Select(link => link.Status)).Distinct().ToList();
            if (!statuses.Any(status => status != StatusKind.Pass)) return null;
            return new FactFlow(statuses.Select(status => "[" + status.ToToken().ToString().ToLowerInvariant() + "]"
                + Markup.Escape(context.Glyphs.StatusSymbol(status) + " " + status) + "[/]")) { Separator = "   " };
        }
    }
}
