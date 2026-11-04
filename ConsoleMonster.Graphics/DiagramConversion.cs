using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    internal static class DiagramConversion {
        internal static TopologyHealthStatus Health(StatusKind status) {
            switch (status) {
                case StatusKind.Pass: return TopologyHealthStatus.Healthy;
                case StatusKind.Warn: return TopologyHealthStatus.Warning;
                case StatusKind.Fail: return TopologyHealthStatus.Critical;
                case StatusKind.Skip: return TopologyHealthStatus.Disabled;
                default: return TopologyHealthStatus.Unknown;
            }
        }
        private static StatusKind Health(TopologyHealthStatus status) {
            switch (status) {
                case TopologyHealthStatus.Healthy: return StatusKind.Pass;
                case TopologyHealthStatus.Warning: return StatusKind.Warn;
                case TopologyHealthStatus.Critical: return StatusKind.Fail;
                case TopologyHealthStatus.Disabled: return StatusKind.Skip;
                default: return StatusKind.Unknown;
            }
        }
        internal static TopologyNodeKind Kind(DiagramNodeKind kind) {
            switch (kind) {
                case DiagramNodeKind.Workstation: return TopologyNodeKind.Endpoint;
                case DiagramNodeKind.User: case DiagramNodeKind.Person: return TopologyNodeKind.Person;
                case DiagramNodeKind.App: return TopologyNodeKind.Application;
                case DiagramNodeKind.Device: case DiagramNodeKind.External: return TopologyNodeKind.Generic;
                default: return (TopologyNodeKind)Enum.Parse(typeof(TopologyNodeKind), kind.ToString());
            }
        }
        internal static string Key(Diagram model) {
            model.Resolve();
            var key = new StringBuilder();
            void Add(object? value) { string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty; key.Append(text.Length).Append(':').Append(text); }
            Add(model.Title); Add(model.Direction);
            void Node(DiagramNode node) { Add(node.Id); Add(node.Label); Add(node.Kind); Add(node.Subtitle); foreach (string to in node.To) Add(to); Add("end-node"); }
            void Group(DiagramGroup group) { Add(group.Label); Add(group.Boundary); foreach (DiagramNode node in group.Nodes) Node(node); foreach (DiagramGroup child in group.Groups) Group(child); Add("end-group"); }
            foreach (DiagramNode node in model.Nodes) Node(node);
            foreach (DiagramGroup group in model.Groups) Group(group);
            foreach (DiagramLink link in model.Links) { Add(link.From); Add(link.To); Add(link.Label); Add(link.Style); Add(link.Both); Add(link.Status); }
            foreach (DiagramUseCase useCase in model.UseCases) {
                Add(useCase.Id); Add(useCase.Label); Add(useCase.Delay); Add(useCase.Loop); Add(useCase.AutoPlay);
                foreach (DiagramStep step in useCase.Steps) { Add(step.From); Add(step.To); Add(step.Label); Add(step.Kind); foreach (var field in step.Payload.OrderBy(p => p.Key, StringComparer.Ordinal)) { Add(field.Key); Add(field.Value); } Add("end-step"); }
                Add("end-case");
            }
            return key.ToString();
        }
        internal static TopologyChart ToChart(Diagram model) {
            model.Resolve();
            TopologyLayoutDirection direction;
            switch (model.Direction) {
                case DiagramDirection.Down: direction = TopologyLayoutDirection.TopToBottom; break;
                case DiagramDirection.Left: direction = TopologyLayoutDirection.RightToLeft; break;
                case DiagramDirection.Up: direction = TopologyLayoutDirection.BottomToTop; break;
                default: direction = TopologyLayoutDirection.LeftToRight; break;
            }
            // A terminal diagram starts on a compact logical canvas. The engine grows its content as needed;
            // the terminal projection must not inherit the large default canvas used for web artwork.
            var chart = TopologyChart.Create().WithTitle(model.Title).WithLayout(TopologyLayoutMode.Layered, direction).WithViewport(480, 240, 28);
            int groupIndex = 0;
            void Node(DiagramNode node, string? group) => chart.AddAutoNode(node.Id, node.Label, Kind(node.Kind), Health(node.Status), group, node.Subtitle);
            void Group(DiagramGroup group, string? parent) {
                string id = "group-" + groupIndex++.ToString(CultureInfo.InvariantCulture);
                chart.AddGroup(id, group.Label, 0, 0, 0, 0);
                chart.Groups.Last().ParentGroupId = parent;
                foreach (DiagramNode node in group.Nodes) Node(node, id);
                foreach (DiagramGroup child in group.Groups) Group(child, id);
            }
            foreach (DiagramGroup group in model.Groups) Group(group, null);
            foreach (DiagramNode node in model.Nodes) Node(node, null);
            var links = model.AllLinks().ToList();
            int edgeIndex = 0;
            string Edge(DiagramLink link) {
                string id = "link-" + edgeIndex++.ToString(CultureInfo.InvariantCulture);
                chart.AddEdge(id, link.From, link.To, link.Label, status: Health(link.Status), direction: link.Both ? VisualLinkDirection.Bidirectional : VisualLinkDirection.Forward);
                chart.Edges.Last().LineStyle = (TopologyEdgeLineStyle)Enum.Parse(typeof(TopologyEdgeLineStyle), link.Style.ToString());
                return id;
            }
            foreach (DiagramLink link in links) Edge(link);
            for (int index = 0; index < model.UseCases.Count; index++) {
                DiagramUseCase useCase = model.UseCases[index];
                chart.AddScenario("case-" + index.ToString(CultureInfo.InvariantCulture), useCase.Label, scenario => {
                    scenario.WithPlayback(useCase.Delay, useCase.Loop, useCase.AutoPlay).WithSpotlight();
                    foreach (DiagramStep step in useCase.Steps) {
                        TopologyEdge? edge = chart.Edges.FirstOrDefault(e => e.SourceNodeId == step.From && e.TargetNodeId == step.To)
                            ?? chart.Edges.FirstOrDefault(e => e.SourceNodeId == step.To && e.TargetNodeId == step.From);
                        string id = edge?.Id ?? Edge(new DiagramLink(step.From, step.To));
                        bool reverse = edge != null && edge.SourceNodeId != step.From;
                        TopologyScenarioInteraction interaction = step.Kind == DiagramStepKind.Reply ? TopologyScenarioInteraction.Response : step.Kind == DiagramStepKind.Failed ? TopologyScenarioInteraction.Failure : step.Kind == DiagramStepKind.Message ? TopologyScenarioInteraction.Message : TopologyScenarioInteraction.Request;
                        scenario.AddMessageStep(id, interaction, step.Label, reverse, configured => {
                            if (step.Payload.Count > 0) configured.WithPayload(string.Join(Environment.NewLine, step.Payload.Select(p => p.Key + ": " + Convert.ToString(p.Value, CultureInfo.CurrentCulture))));
                        });
                    }
                });
            }
            return chart;
        }

        internal static Diagram From(TopologyChart chart) {
            var model = new Diagram(chart.Title ?? "");
            switch (chart.LayoutDirection) {
                case TopologyLayoutDirection.TopToBottom: model.Direction = DiagramDirection.Down; break;
                case TopologyLayoutDirection.BottomToTop: model.Direction = DiagramDirection.Up; break;
                case TopologyLayoutDirection.RightToLeft: model.Direction = DiagramDirection.Left; break;
            }
            var groups = chart.Groups.ToDictionary(g => g.Id, g => new DiagramGroup(g.Label), StringComparer.Ordinal);
            foreach (TopologyGroup group in chart.Groups) {
                if (group.ParentGroupId != null && groups.TryGetValue(group.ParentGroupId, out DiagramGroup? parent)) parent.Groups.Add(groups[group.Id]);
                else model.Groups.Add(groups[group.Id]);
            }
            foreach (TopologyNode node in chart.Nodes) {
                DiagramNodeKind kind = node.Kind == TopologyNodeKind.Endpoint ? DiagramNodeKind.Workstation : node.Kind == TopologyNodeKind.Application ? DiagramNodeKind.App : Enum.TryParse(node.Kind.ToString(), out DiagramNodeKind known) ? known : DiagramNodeKind.Service;
                var item = new DiagramNode(node.Label) { Id = node.Id, Kind = kind, Subtitle = node.Subtitle };
                // ChartForgeX uses Unknown for "no status"; importing it would mark every undeclared node as needing attention.
                if (node.Status != TopologyHealthStatus.Unknown) item.Status = Health(node.Status);
                if (node.GroupId != null && groups.TryGetValue(node.GroupId, out DiagramGroup? group)) group.Nodes.Add(item); else model.Nodes.Add(item);
            }
            foreach (TopologyEdge edge in chart.Edges) {
                bool backward = edge.Direction == VisualLinkDirection.Backward;
                var link = new DiagramLink(backward ? edge.TargetNodeId : edge.SourceNodeId, backward ? edge.SourceNodeId : edge.TargetNodeId) {
                    Label = edge.Label, Both = edge.Direction == VisualLinkDirection.Bidirectional,
                    Style = edge.LineStyle == TopologyEdgeLineStyle.Dotted ? DiagramLinkStyle.Dotted : edge.LineStyle == TopologyEdgeLineStyle.Dashed ? DiagramLinkStyle.Dashed : DiagramLinkStyle.Solid
                };
                if (edge.Status != TopologyHealthStatus.Unknown) link.Status = Health(edge.Status);
                model.Links.Add(link);
            }
            foreach (TopologyScenario scenario in chart.Scenarios) {
                var useCase = new DiagramUseCase(scenario.Label) { Id = scenario.Id, Delay = scenario.PlaybackDelayMilliseconds, AutoPlay = scenario.AutoPlay, Loop = scenario.LoopPlayback };
                foreach (TopologyScenarioStep step in scenario.Steps) {
                    TopologyEdge? edge = step.Kind == TopologyScenarioStepKind.Edge ? chart.Edges.FirstOrDefault(e => e.Id == step.Id) : null;
                    string from = edge == null ? step.Id : step.Reverse ? edge.TargetNodeId : edge.SourceNodeId;
                    string to = edge == null ? step.Id : step.Reverse ? edge.SourceNodeId : edge.TargetNodeId;
                    var item = new DiagramStep(from, to, step.Label ?? "") { Kind = step.Interaction == TopologyScenarioInteraction.Response ? DiagramStepKind.Reply : step.Interaction == TopologyScenarioInteraction.Failure ? DiagramStepKind.Failed : step.Interaction == TopologyScenarioInteraction.Message ? DiagramStepKind.Message : DiagramStepKind.Request };
                    if (step.Payload != null) item.Payload["Payload"] = step.Payload;
                    useCase.Steps.Add(item);
                }
                model.UseCases.Add(useCase);
            }
            // Preserve file/chart features outside the friendly model, including node-only steps and payload formats.
            model.AttachRenderer(new DiagramGraphicsRenderer(model, new DiagramImportedChart(chart, model, groups)));
            return model;
        }
    }
}
