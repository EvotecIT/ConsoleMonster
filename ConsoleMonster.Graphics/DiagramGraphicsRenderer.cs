using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using ChartForgeX.Topology;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>Adapts the core model to one cached engine layout; live values do not replace subtitles.</summary>
    internal sealed class DiagramGraphicsRenderer : IDiagramRenderer {
        private readonly Diagram _model;
        private TopologyDiagram _view;
        private string _key;
        private readonly DiagramImportedChart? _imported;
        internal DiagramGraphicsRenderer(Diagram model, DiagramImportedChart? imported = null) {
            _model = model;
            _key = DiagramConversion.Key(model);
            _imported = imported;
            _view = new TopologyDiagram(imported?.Chart ?? DiagramConversion.ToChart(model), model.WidgetId);
            SetBoundaries();
        }
        private TopologyDiagram Current {
            get {
                lock (_model.Gate) {
                    string key = DiagramConversion.Key(_model);
                    if (_key != key) {
                        string? scenario = ScenarioId;
                        int? step = _view.StepIndex;
                        string? focus = _view.FocusNodeId;
                        bool playing = _view.Playing;
                        _view = new TopologyDiagram(_imported?.Update(_model) ?? DiagramConversion.ToChart(_model), _model.WidgetId);
                        _key = key;
                        SetBoundaries();
                        int index = _model.UseCases.FindIndex(c => c.Id == scenario);
                        if (index >= 0) _view.SelectScenario(_imported?.ScenarioId(_model.UseCases[index]) ?? "case-" + index.ToString(CultureInfo.InvariantCulture));
                        if (step.HasValue) _view.Step(step.Value);
                        if (focus != null) _view.FocusNode(focus);
                        if (playing) _view.Play();
                    }
                    _view.ShowLegend = false;
                    _view.ShowStepPanel = _model.ShowStepPanel;
                    _view.ShowDetails = _model.ShowDetails;
                    _view.ShowKeys = _model.ShowKeys;
                    _view.MaxPayloadLines = _model.MaxPayloadLines;
                    _view.MaxStepLines = _model.MaxStepLines;
                    foreach (DiagramNode node in _model.Resolve()) {
                        _view.SetValue(node.Id, node.Value, DiagramConversion.Health(node.Status));
                        _view.SetNodeMetadata(node.Id, node.Status, node.Kind, node.HasStatus);
                    }
                    var links = _model.AllLinks().ToList();
                    for (int index = 0; index < links.Count && index < _view.Chart.Edges.Count; index++) _view.Chart.Edges[index].Status = DiagramConversion.Health(links[index].Status);
                    return _view;
                }
            }
        }
        private void SetBoundaries() {
            _view.DetailsChanged = details => _model.ShowDetails = details;
            int index = 0;
            void Group(DiagramGroup group) {
                string id = "group-" + index++.ToString(CultureInfo.InvariantCulture);
                if (!group.Boundary) _view.BorderlessGroups.Add(id);
                foreach (DiagramGroup child in group.Groups) Group(child);
            }
            if (_imported == null) foreach (DiagramGroup group in _model.Groups) Group(group);
            else foreach (string id in _imported.BorderlessGroups) _view.BorderlessGroups.Add(id);
        }
        public IWidget View(int width, RenderContext context) {
            if (width >= Constraints.Unbounded / 2) width = 120;
            TopologyDiagram view = Current;
            if (context.Profile.IsAccessible) return _model.TreeView(context);
            if (width < 20) return _model.TreeView(context);
            TopologyCellLayout layout = view.Layout(width, context.Profile.Glyphs);
            bool lost = layout.Columns > width || view.Chart.Nodes.Any(node => {
                TopologyCellNode? placed = layout.FindNode(node.Id);
                return placed == null || placed.Label != DiagramText.ForTier(node.Label, context) || (!string.IsNullOrEmpty(node.Subtitle) && placed.Subtitle != DiagramText.ForTier(node.Subtitle!, context));
            }) || layout.Edges.Any(edge => !string.IsNullOrEmpty(edge.Label) && !edge.LabelPosition.HasValue);
            // Check the painted group labels too: a positioned label can still be overwritten by a route.
            if (!lost && layout.Groups.Count > 0) {
                TopologyCellCanvas canvas = layout.Paint();
                string[] lines = Enumerable.Range(0, canvas.Rows).Select(row => string.Concat(Enumerable.Range(0, canvas.Columns).Select(column => canvas[column, row].Glyph))).ToArray();
                lost = layout.Groups.Any(group => !lines.Any(line => line.Contains(DiagramText.ForTier(group.Label, context))));
            }
            // A forest must retain its requested parent-to-child reading direction. Let the engine repair
            // the geometry; use the lossless hierarchy until its projection satisfies this contract.
            var links = _model.AllLinks().ToList();
            if (!lost && _imported == null && links.Count > 0 && _model.Groups.Count == 0 && _model.ForestParents(links).Count == links.Count) {
                lost = links.Any(link => {
                    TopologyCellNode? from = layout.FindNode(link.From), to = layout.FindNode(link.To);
                    if (from == null || to == null) return true;
                    switch (_model.Direction) {
                        case DiagramDirection.Down: return to.Row < from.Row + from.Height;
                        case DiagramDirection.Up: return from.Row < to.Row + to.Height;
                        case DiagramDirection.Left: return from.Column < to.Column + to.Width;
                        default: return to.Column < from.Column + from.Width;
                    }
                });
            }
            if (lost) return _model.TreeView(context);
            var widgets = new System.Collections.Generic.List<IWidget> { view };
            IWidget? legend = _model.Legend(context);
            if (legend != null) widgets.Add(legend);
            IWidget? summary = _model.Attention(context);
            if (summary != null) widgets.Add(summary);
            return widgets.Count == 1 ? view : new Rows(widgets.ToArray());
        }
        public void Advance(TimeSpan elapsed, bool reducedMotion) => Current.AdvanceFrame(elapsed, reducedMotion);
        public IWidget? Panels(RenderContext context) => Current.Panels(context);
        public string? ScenarioId {
            get {
                if (_view.ScenarioId == null) return null;
                if (_imported != null) return _imported.ModelScenarioId(_view.ScenarioId);
                int index = _view.Chart.Scenarios.FindIndex(c => c.Id == _view.ScenarioId);
                return index >= 0 && index < _model.UseCases.Count ? _model.UseCases[index].Id : null;
            }
        }
        public int? StepIndex => _view.StepIndex;
        public string? FocusNodeId => _view.FocusNodeId;
        public bool Playing => _view.Playing;
        public void SelectScenario(string? id) {
            var view = Current;
            if (id == null) { view.SelectScenario(null); return; }
            int index = _model.UseCases.FindIndex(c => c.Id == id);
            if (index < 0) throw new ArgumentException("Unknown diagram use case: " + id, nameof(id));
            view.SelectScenario(_imported?.ScenarioId(_model.UseCases[index]) ?? "case-" + index.ToString(CultureInfo.InvariantCulture));
        }
        public void NextScenario(int delta) => Current.NextScenario(delta);
        public void Step(int index) => Current.Step(index);
        public void NextStep() => Current.NextStep();
        public void PreviousStep() => Current.PreviousStep();
        public void ClearStep() => Current.ClearStep();
        public void Play() => Current.Play();
        public void Pause() => Current.Pause();
        public void FocusNode(string? id) => Current.FocusNode(id);
        public void FocusNext(int delta) => Current.FocusNext(delta);
        public bool HandleInput(InputEvent input) => Current.HandleInput(input);
        public AppState Run(ConsoleAppOptions? options, CancellationToken cancellationToken) {
            bool keys = _model.ShowKeys;
            try { _model.ShowKeys = keys || (options?.Interactive ?? InteractiveOptions.Default).ResolveMode() == InteractionMode.Rich; return Current.Run(options, cancellationToken, _model); }
            finally { _model.ShowKeys = keys; }
        }
        public byte[] ToPng() => ConsoleGraphicsExtensions.ToPng(_model);
    }
}
