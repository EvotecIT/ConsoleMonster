using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>Applies friendly-model edits without replacing the imported chart's richer content.</summary>
    internal sealed partial class DiagramImportedChart {
        internal TopologyChart Chart { get; }
        private readonly Dictionary<DiagramGroup, TopologyGroup> _groups;
        private readonly Dictionary<DiagramLink, TopologyEdge> _links;
        private readonly Dictionary<DiagramUseCase, TopologyScenario> _cases;
        private readonly Dictionary<DiagramUseCase, string> _steps = new Dictionary<DiagramUseCase, string>();
        private readonly Dictionary<DiagramNode, TopologyNode> _nodes = new Dictionary<DiagramNode, TopologyNode>();
        private readonly Dictionary<DiagramNode, DiagramNodeKind> _kinds = new Dictionary<DiagramNode, DiagramNodeKind>();
        private readonly Dictionary<DiagramLink, DiagramLinkStyle> _styles = new Dictionary<DiagramLink, DiagramLinkStyle>();
        private readonly Dictionary<DiagramLink, bool> _both = new Dictionary<DiagramLink, bool>();
        private DiagramDirection _direction;

        internal DiagramImportedChart(TopologyChart chart, Diagram model, Dictionary<string, DiagramGroup> groups) {
            Chart = chart;
            _direction = model.Direction;
            _groups = groups.ToDictionary(pair => pair.Value, pair => chart.Groups.Single(group => group.Id == pair.Key));
            _links = model.Links.Select((link, index) => new { link, edge = chart.Edges[index] }).ToDictionary(pair => pair.link, pair => pair.edge);
            _cases = model.UseCases.Select((useCase, index) => new { useCase, scenario = chart.Scenarios[index] }).ToDictionary(pair => pair.useCase, pair => pair.scenario);
            foreach (DiagramUseCase useCase in model.UseCases) {
                _steps[useCase] = StepsKey(useCase);
                TrackSteps(useCase, _cases[useCase]);
            }
            foreach (DiagramNode node in model.Resolve()) { _nodes[node] = chart.Nodes.Single(item => item.Id == node.Id); _kinds[node] = node.Kind; }
            foreach (DiagramLink link in model.Links) { _styles[link] = link.Style; _both[link] = link.Both; }
        }

        internal IEnumerable<string> BorderlessGroups => _groups.Where(pair => !pair.Key.Boundary).Select(pair => pair.Value.Id);
        internal string ScenarioId(DiagramUseCase useCase) => _cases[useCase].Id;
        internal string? ModelScenarioId(string id) => _cases.FirstOrDefault(pair => pair.Value.Id == id).Key?.Id;

        internal TopologyChart Update(Diagram model) {
            Chart.Title = model.Title;
            if (model.Direction != _direction) {
                Chart.LayoutDirection = model.Direction == DiagramDirection.Down ? TopologyLayoutDirection.TopToBottom
                    : model.Direction == DiagramDirection.Up ? TopologyLayoutDirection.BottomToTop
                    : model.Direction == DiagramDirection.Left ? TopologyLayoutDirection.RightToLeft : TopologyLayoutDirection.LeftToRight;
                _direction = model.Direction;
            }
            var nodeGroups = new Dictionary<string, string>(StringComparer.Ordinal);
            var currentGroups = new List<TopologyGroup>();
            void Group(DiagramGroup group, string? parent) {
                if (!_groups.TryGetValue(group, out TopologyGroup? target)) {
                    Chart.AddGroup(NextId("diagram-group", Chart.Groups.Select(item => item.Id)), group.Label, 0, 0, 0, 0);
                    _groups[group] = target = Chart.Groups.Last();
                }
                if (target.Label != group.Label) target.Label = group.Label;
                target.ParentGroupId = parent;
                currentGroups.Add(target);
                foreach (DiagramNode node in group.Nodes) nodeGroups[node.Id] = target.Id;
                foreach (DiagramGroup child in group.Groups) Group(child, target.Id);
            }
            foreach (DiagramGroup group in model.Groups) Group(group, null);
            Chart.Groups.RemoveAll(group => !currentGroups.Contains(group));
            foreach (DiagramGroup group in _groups.Keys.Where(group => !currentGroups.Contains(_groups[group])).ToArray()) _groups.Remove(group);

            var currentNodes = new List<TopologyNode>();
            foreach (DiagramNode node in model.Resolve()) {
                if (!_nodes.TryGetValue(node, out TopologyNode? target)) target = Chart.Nodes.FirstOrDefault(item => item.Id == node.Id);
                if (target == null) {
                    Chart.AddAutoNode(node.Id, node.Label, DiagramConversion.Kind(node.Kind));
                    target = Chart.Nodes.Last();
                }
                target.Id = node.Id;
                _nodes[node] = target;
                if (target.Label != node.Label) target.Label = node.Label;
                if (target.Subtitle != node.Subtitle) target.Subtitle = node.Subtitle;
                if (!_kinds.TryGetValue(node, out DiagramNodeKind previousKind) || previousKind != node.Kind) target.Kind = DiagramConversion.Kind(node.Kind);
                _kinds[node] = node.Kind;
                target.Status = DiagramConversion.Health(node.Status);
                target.GroupId = nodeGroups.TryGetValue(node.Id, out string? groupId) ? groupId : null;
                currentNodes.Add(target);
            }
            Chart.Nodes.Clear(); Chart.Nodes.AddRange(currentNodes);
            foreach (DiagramNode node in _nodes.Keys.Where(node => !currentNodes.Contains(_nodes[node])).ToArray()) { _nodes.Remove(node); _kinds.Remove(node); }

            var currentEdges = new List<TopologyEdge>();
            var currentLinks = model.AllLinks().ToList();
            foreach (DiagramLink link in currentLinks) {
                if (!_links.TryGetValue(link, out TopologyEdge? edge)) {
                    Chart.AddEdge(NextId("diagram-link", Chart.Edges.Select(item => item.Id)), link.From, link.To);
                    _links[link] = edge = Chart.Edges.Last();
                }
                if (!_both.TryGetValue(link, out bool previousBoth) || previousBoth != link.Both) edge.Direction = link.Both ? VisualLinkDirection.Bidirectional : VisualLinkDirection.Forward;
                _both[link] = link.Both;
                bool backward = edge.Direction == VisualLinkDirection.Backward;
                edge.SourceNodeId = backward ? link.To : link.From;
                edge.TargetNodeId = backward ? link.From : link.To;
                if (edge.Label != link.Label) edge.Label = link.Label;
                edge.Status = DiagramConversion.Health(link.Status);
                if (!_styles.TryGetValue(link, out DiagramLinkStyle previousStyle) || previousStyle != link.Style) edge.LineStyle = (TopologyEdgeLineStyle)Enum.Parse(typeof(TopologyEdgeLineStyle), link.Style.ToString());
                _styles[link] = link.Style;
                currentEdges.Add(edge);
            }
            foreach (DiagramLink link in _links.Keys.Where(link => !currentLinks.Contains(link)).ToArray()) { _links.Remove(link); _styles.Remove(link); _both.Remove(link); }
            foreach (DiagramUseCase useCase in model.UseCases) {
                bool created = !_cases.TryGetValue(useCase, out TopologyScenario? scenario);
                if (created) {
                    Chart.AddScenario(NextId("diagram-case", Chart.Scenarios.Select(item => item.Id)), useCase.Label);
                    _cases[useCase] = scenario = Chart.Scenarios.Last();
                }
                scenario!.Label = useCase.Label;
                scenario.PlaybackDelayMilliseconds = useCase.Delay;
                scenario.LoopPlayback = useCase.Loop;
                scenario.AutoPlay = useCase.AutoPlay;
                string steps = StepsKey(useCase);
                if (created || _steps[useCase] != steps) {
                    UpdateSteps(useCase, scenario, currentEdges);
                    _steps[useCase] = steps;
                }
            }
            Chart.Scenarios.RemoveAll(scenario => !_cases.Any(pair => model.UseCases.Contains(pair.Key) && pair.Value == scenario));
            foreach (DiagramUseCase useCase in _cases.Keys.Where(useCase => !model.UseCases.Contains(useCase)).ToArray()) { _cases.Remove(useCase); _steps.Remove(useCase); _stepBindings.Remove(useCase); }
            // Unedited imported steps can reference richer edge routes than the friendly model creates.
            var referenced = new HashSet<string>(Chart.Scenarios.SelectMany(scenario => scenario.Steps).Where(step => step.Kind == TopologyScenarioStepKind.Edge).Select(step => step.Id), StringComparer.Ordinal);
            currentEdges.AddRange(Chart.Edges.Where(edge => referenced.Contains(edge.Id) && !currentEdges.Contains(edge)));
            Chart.Edges.Clear(); Chart.Edges.AddRange(currentEdges);
            return Chart;
        }

        private static string StepsKey(DiagramUseCase useCase) {
            var holder = new Diagram();
            var steps = new DiagramUseCase("steps");
            steps.Steps.AddRange(useCase.Steps);
            holder.UseCases.Add(steps);
            return DiagramConversion.Key(holder);
        }
        private static string NextId(string prefix, IEnumerable<string> existing) {
            var ids = new HashSet<string>(existing, StringComparer.Ordinal);
            int index = 0;
            while (ids.Contains(prefix + index.ToString(CultureInfo.InvariantCulture))) index++;
            return prefix + index.ToString(CultureInfo.InvariantCulture);
        }
    }
}
