using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    internal sealed partial class DiagramImportedChart {
        private readonly Dictionary<DiagramUseCase, Dictionary<DiagramStep, StepBinding>> _stepBindings = new Dictionary<DiagramUseCase, Dictionary<DiagramStep, StepBinding>>();

        private void TrackSteps(DiagramUseCase useCase, TopologyScenario scenario) {
            var bindings = new Dictionary<DiagramStep, StepBinding>();
            for (int i = 0; i < useCase.Steps.Count; i++) bindings[useCase.Steps[i]] = new StepBinding(useCase.Steps[i], scenario.Steps[i], scenario.Steps[i].Payload != null);
            _stepBindings[useCase] = bindings;
        }

        private void UpdateSteps(DiagramUseCase useCase, TopologyScenario scenario, List<TopologyEdge> edges) {
            if (!_stepBindings.TryGetValue(useCase, out Dictionary<DiagramStep, StepBinding>? previous)) previous = new Dictionary<DiagramStep, StepBinding>();
            var bindings = new Dictionary<DiagramStep, StepBinding>();
            var steps = new List<TopologyScenarioStep>();
            foreach (DiagramStep step in useCase.Steps) {
                TopologyScenarioStep target;
                previous.TryGetValue(step, out StepBinding? binding);
                bool rawPayloadCarrier = binding?.RawPayloadCarrier ?? false;
                string? payload = PayloadText(step, rawPayloadCarrier);
                if (binding != null) {
                    target = binding.Target;
                    bool kindChanged = binding.Kind != step.Kind;
                    bool wasNode = target.Kind == TopologyScenarioStepKind.Node;
                    if (binding.From != step.From || binding.To != step.To || (wasNode && kindChanged)) {
                        if (wasNode && step.From == step.To && !kindChanged) target.Id = step.From;
                        else {
                            TopologyEdge edge = StepEdge(step, edges);
                            target.Kind = TopologyScenarioStepKind.Edge;
                            target.Id = edge.Id;
                            target.Reverse = edge.SourceNodeId != step.From;
                        }
                    }
                    if (target.Kind == TopologyScenarioStepKind.Edge && (kindChanged || wasNode)) target.Interaction = Interaction(step.Kind);
                    if (binding.Label != step.Label) target.Label = step.Label;
                    if (binding.Payload != payload) {
                        target.Payload = payload;
                        if (!rawPayloadCarrier || step.Payload.Count != 1 || !step.Payload.ContainsKey("Payload")) target.PayloadFormat = TopologyScenarioPayloadFormat.Text;
                    }
                } else {
                    TopologyEdge edge = StepEdge(step, edges);
                    target = new TopologyScenarioStep {
                        Kind = TopologyScenarioStepKind.Edge, Id = edge.Id, Label = step.Label,
                        Interaction = Interaction(step.Kind), Reverse = edge.SourceNodeId != step.From, Payload = payload
                    };
                }
                steps.Add(target);
                bindings[step] = new StepBinding(step, target, rawPayloadCarrier);
            }
            scenario.Steps.Clear();
            scenario.Steps.AddRange(steps);
            _stepBindings[useCase] = bindings;
        }

        private TopologyEdge StepEdge(DiagramStep step, List<TopologyEdge> edges) {
            TopologyEdge? edge = edges.FirstOrDefault(item => item.SourceNodeId == step.From && item.TargetNodeId == step.To)
                ?? edges.FirstOrDefault(item => item.SourceNodeId == step.To && item.TargetNodeId == step.From);
            if (edge == null) {
                Chart.AddEdge(NextId("diagram-step", Chart.Edges.Select(item => item.Id)), step.From, step.To);
                edges.Add(edge = Chart.Edges.Last());
            }
            return edge;
        }

        private static TopologyScenarioInteraction Interaction(DiagramStepKind kind) => kind == DiagramStepKind.Reply ? TopologyScenarioInteraction.Response
            : kind == DiagramStepKind.Failed ? TopologyScenarioInteraction.Failure : kind == DiagramStepKind.Message ? TopologyScenarioInteraction.Message : TopologyScenarioInteraction.Request;

        private static string? PayloadText(DiagramStep step, bool rawPayloadCarrier) => step.Payload.Count == 0 ? null
            : rawPayloadCarrier && step.Payload.Count == 1 && step.Payload.TryGetValue("Payload", out object? value) ? Convert.ToString(value, CultureInfo.CurrentCulture)
            : string.Join(Environment.NewLine, step.Payload.Select(pair => pair.Key + ": " + Convert.ToString(pair.Value, CultureInfo.CurrentCulture)));

        private sealed class StepBinding {
            internal StepBinding(DiagramStep step, TopologyScenarioStep target, bool rawPayloadCarrier) {
                Target = target; From = step.From; To = step.To; Label = step.Label; Kind = step.Kind;
                RawPayloadCarrier = rawPayloadCarrier; Payload = PayloadText(step, rawPayloadCarrier);
            }
            internal TopologyScenarioStep Target { get; }
            internal string From { get; }
            internal string To { get; }
            internal string Label { get; }
            internal DiagramStepKind Kind { get; }
            internal string? Payload { get; }
            internal bool RawPayloadCarrier { get; }
        }
    }
}
