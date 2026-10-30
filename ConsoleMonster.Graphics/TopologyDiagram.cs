using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>
    /// Draws a ChartForgeX topology in terminal cells: boxes, routed edges, dashed group boundaries and a legend, with a
    /// use case player (step list, payload, keys), node focus details and live node values. ChartForgeX lays the
    /// diagram out and projects it into cells; this widget only styles and draws those cells.
    /// </summary>
    /// <remarks>Keys, live events and rendering may arrive on different threads, so every state change takes one lock.</remarks>
    public sealed partial class TopologyDiagram : LiveWidget {
        private readonly object _gate = new object();
        private readonly TopologyChart _chart;
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, TopologyHealthStatus> _statuses = new Dictionary<string, TopologyHealthStatus>(StringComparer.Ordinal);
        private PreparedTopology? _prepared;
        private TopologyCellLayout? _layout;
        private int _layoutWidth = -1;
        private int _layoutReserve = -1;
        private TopologyResolvedScenario? _scenario;
        private int? _step;
        private string? _focus;
        private bool _playing;
        private bool _showDetails = true;
        private TimeSpan? _playAnchor;
        private int _playFrom;

        /// <summary>Creates a view of a topology. The first use case that resolves, when there is one, is selected without an active step.</summary>
        /// <param name="chart">The topology chart, for example from <c>MarkupArchitectureParser</c>.</param>
        /// <param name="widgetId">The live event target.</param>
        public TopologyDiagram(TopologyChart chart, string widgetId = "topology") : base(widgetId) {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            foreach (TopologyScenario scenario in chart.Scenarios) {
                if (TrySelect(scenario.Id)) break;
            }
        }

        /// <summary>The topology being drawn.</summary>
        public TopologyChart Chart => _chart;
        /// <summary>The selected use case id, or null.</summary>
        public string? ScenarioId { get { lock (_gate) { return _scenario?.Id; } } }
        /// <summary>The selected use case resolved against the topology, or null.</summary>
        public TopologyResolvedScenario? Scenario { get { lock (_gate) { return _scenario; } } }
        /// <summary>The zero-based active step, or null when no step is highlighted.</summary>
        public int? StepIndex { get { lock (_gate) { return _step; } } }
        /// <summary>The focused node id, or null.</summary>
        public string? FocusNodeId { get { lock (_gate) { return _focus; } } }
        /// <summary>Whether the use case is playing; steps advance with the live session clock.</summary>
        public bool Playing { get { lock (_gate) { return _playing; } } }
        /// <summary>Whether the status legend is drawn under the diagram.</summary>
        public bool ShowLegend { get; set; } = true;
        /// <summary>Whether the use case panel (steps and payload) is drawn.</summary>
        public bool ShowStepPanel { get; set; } = true;
        /// <summary>Whether the focused node's details are drawn.</summary>
        public bool ShowDetails { get { lock (_gate) { return _showDetails; } } set { lock (_gate) { _showDetails = value; } } }
        /// <summary>Whether the key hints line is drawn in the use case panel; <see cref="TopologyDiagramApp.Run"/> turns it on.</summary>
        public bool ShowKeys { get; set; }
        /// <summary>The most payload lines shown for the active step.</summary>
        public int MaxPayloadLines { get => _maxPayloadLines; set => _maxPayloadLines = Math.Max(1, value); }
        private int _maxPayloadLines = 8;
        /// <summary>The most step lines shown around the active step.</summary>
        public int MaxStepLines { get => _maxStepLines; set => _maxStepLines = Math.Max(1, value); }
        private int _maxStepLines = 7;
        /// <summary>A copy of the live values shown on the nodes' second line.</summary>
        public IReadOnlyDictionary<string, string> Values { get { lock (_gate) { return new Dictionary<string, string>(_values, StringComparer.Ordinal); } } }

        /// <summary>Selects a use case and clears the active step.</summary>
        /// <param name="scenarioId">The use case id, or null to show the plain diagram.</param>
        /// <returns>This view.</returns>
        /// <exception cref="ArgumentException">The use case does not exist or references missing nodes or edges.</exception>
        public TopologyDiagram SelectScenario(string? scenarioId) {
            lock (_gate) {
                _scenario = string.IsNullOrWhiteSpace(scenarioId) ? null : _chart.ResolveScenario(scenarioId!);
                _step = null;
                StopPlayback();
            }

            return this;
        }

        /// <summary>Selects the next or previous use case that resolves, wrapping around.</summary>
        /// <param name="delta">1 for the next use case, -1 for the previous one.</param>
        public void NextScenario(int delta = 1) {
            lock (_gate) {
                int count = _chart.Scenarios.Count;
                if (count == 0) return;
                int current = _scenario == null ? (delta > 0 ? -1 : 0) : _chart.Scenarios.FindIndex(item => item.Id == _scenario.Id);
                for (int attempt = 1; attempt <= count; attempt++) {
                    if (TrySelect(_chart.Scenarios[((current + delta * attempt) % count + count) % count].Id)) return;
                }
            }
        }

        /// <summary>Highlights one step of the selected use case; the index is clamped to the steps.</summary>
        /// <param name="index">The zero-based step.</param>
        public void Step(int index) { lock (_gate) { StopPlayback(); SetStep(index); } }
        /// <summary>Moves to the next step, or to the first when none is active.</summary>
        public void NextStep() { lock (_gate) { StopPlayback(); SetStep(_step.HasValue ? _step.Value + 1 : 0); } }
        /// <summary>Moves to the previous step, or to the last when none is active.</summary>
        public void PreviousStep() { lock (_gate) { StopPlayback(); SetStep(_step.HasValue ? _step.Value - 1 : int.MaxValue); } }
        /// <summary>Moves to the first step.</summary>
        public void FirstStep() => Step(0);
        /// <summary>Moves to the last step.</summary>
        public void LastStep() => Step(int.MaxValue);
        /// <summary>Removes the step highlight.</summary>
        public void ClearStep() { lock (_gate) { StopPlayback(); _step = null; } }

        /// <summary>Starts playback from the step after the active one; the live session clock advances the steps.</summary>
        public void Play() {
            lock (_gate) {
                if (_scenario == null || _scenario.Steps.Count == 0) return;
                if (_step.HasValue && _step.Value >= _scenario.Steps.Count - 1) _step = null;
                _playing = true;
                _playAnchor = null;
            }
        }

        /// <summary>Stops playback on the current step.</summary>
        public void Pause() { lock (_gate) { StopPlayback(); } }

        /// <summary>Starts or stops playback.</summary>
        public void TogglePlay() { lock (_gate) { if (_playing) StopPlayback(); else Play(); } }

        /// <summary>Focuses a node so its details are shown.</summary>
        /// <param name="nodeId">The node id, or null to clear the focus.</param>
        public void FocusNode(string? nodeId) {
            lock (_gate) { _focus = nodeId != null && _chart.Nodes.Any(node => node.Id == nodeId) ? nodeId : null; }
        }

        /// <summary>Moves the focus to the next or previous node in reading order (top to bottom, left to right).</summary>
        /// <param name="delta">1 for the next node, -1 for the previous one.</param>
        public void FocusNext(int delta = 1) {
            lock (_gate) {
                TopologyCellLayout layout = Layout(_layoutWidth > 0 ? _layoutWidth : 120);
                List<TopologyCellNode> order = layout.Nodes.OrderBy(node => node.Row).ThenBy(node => node.Column).ToList();
                if (order.Count == 0) return;
                int current = _focus == null ? (delta > 0 ? -1 : 0) : order.FindIndex(node => node.Id == _focus);
                _focus = order[((current + delta) % order.Count + order.Count) % order.Count].Id;
            }
        }

        /// <summary>Sets a live value and optional status on a node, such as a health percentage or a count.</summary>
        /// <param name="nodeId">The node id.</param>
        /// <param name="value">The value text, or null to show the node's subtitle again.</param>
        /// <param name="status">An optional live status that replaces the node's own status. With a null value and no status, the live status is cleared too.</param>
        public void SetValue(string nodeId, string? value, TopologyHealthStatus? status = null) {
            if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("A node id is required.", nameof(nodeId));
            lock (_gate) {
                if (value == null) _values.Remove(nodeId); else _values[nodeId] = value;
                if (status.HasValue) _statuses[nodeId] = status.Value;
                else if (value == null) _statuses.Remove(nodeId);
            }
        }

        /// <summary>Returns a handle that posts value, status, step and focus changes through a live dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher, usually a live session.</param>
        /// <returns>The handle.</returns>
        public TopologyDiagramHandle Handle(ILiveDispatcher dispatcher) => new TopologyDiagramHandle(dispatcher, WidgetId);

        /// <summary>
        /// Renders the diagram with ChartForgeX as PNG, with the active step highlighted (live values and focus stay in
        /// the cell drawing). Terminals that can show images use this for an inline picture.
        /// </summary>
        /// <returns>PNG bytes.</returns>
        public byte[] ToPng() {
            TopologyResolvedScenario? scenario;
            int? step;
            lock (_gate) { scenario = _scenario; step = _step; }
            return scenario != null && step.HasValue ? _chart.PrepareScenarioStep(scenario.Id, step.Value).ToPng() : Prepared.ToPng();
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            switch (liveEvent.Operation) {
                case "value":
                    if (liveEvent.Key != null) SetValue(liveEvent.Key, liveEvent.Text, TryStatus(liveEvent.Status));
                    break;
                case "status":
                    if (liveEvent.Key != null && TryStatus(liveEvent.Status) is TopologyHealthStatus status) lock (_gate) { _statuses[liveEvent.Key] = status; }
                    break;
                case "scenario":
                    lock (_gate) { if (string.IsNullOrWhiteSpace(liveEvent.Text)) SelectScenario(null); else TrySelect(liveEvent.Text!); }
                    break;
                case "step":
                    Step((int)liveEvent.Value);
                    break;
                case "focus":
                    FocusNode(liveEvent.Key);
                    break;
                case "play":
                    Play();
                    break;
                case "pause":
                    Pause();
                    break;
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) => liveEvent.Operation == "value" && liveEvent.Key != null
            ? liveEvent.Key + " = " + (liveEvent.Text ?? string.Empty)
            : null;

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            lock (_gate) {
                Advance(context.Elapsed);
                Frame frame = Compose(WidthFor(constraints.MaxWidth), context);
                return new Size(Math.Min(constraints.MaxWidth, frame.Width), constraints.IsHeightUnbounded ? frame.Height : Math.Min(frame.Height, constraints.MaxHeight));
            }
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            lock (_gate) {
                Advance(context.Elapsed);
                Draw(region, Compose(region.Width, context), context);
            }
        }

        internal PreparedTopology Prepared { get { lock (_gate) { return _prepared ??= _chart.Prepare(); } } }

        internal TopologyCellLayout Layout(int width) {
            int reserve = Math.Min(64, _values.Values.Select(value => DisplayWidth.Measure(value)).DefaultIfEmpty(0).Max());
            if (_values.Count > 0) reserve = Math.Max(reserve, 8);
            bool fits = _layout != null && reserve <= _layoutReserve && (_layoutWidth == width || _layout.Columns <= width && width <= _layoutWidth);
            if (!fits) {
                _layout = TopologyCellLayout.Create(Prepared, new TopologyCellLayoutOptions {
                    MaximumColumns = Math.Max(20, Math.Min(1000, width)), ReservedValueWidth = reserve, CompactToFit = true
                });
                _layoutWidth = width;
                _layoutReserve = reserve;
            }

            return _layout!;
        }

        private bool TrySelect(string scenarioId) {
            try {
                SelectScenario(scenarioId);
                return true;
            } catch (ArgumentException) {
                return false;
            }
        }

        private void SetStep(int index) {
            if (_scenario == null || _scenario.Steps.Count == 0) return;
            _step = Math.Max(0, Math.Min(_scenario.Steps.Count - 1, index));
        }

        private void StopPlayback() { _playing = false; _playAnchor = null; }

        private void Advance(TimeSpan elapsed) {
            if (!_playing || _scenario == null || _scenario.Steps.Count == 0) return;
            if (!_playAnchor.HasValue || elapsed < _playAnchor.Value) {
                // A new clock (another session, or a frame drawn outside the session) restarts from the current step.
                _playAnchor = elapsed;
                _playFrom = _step ?? -1;
            }

            int delay = Math.Max(200, _scenario.Scenario.PlaybackDelayMilliseconds);
            int index = Math.Max(0, _playFrom + 1 + (int)((elapsed - _playAnchor.Value).TotalMilliseconds / delay));
            if (index >= _scenario.Steps.Count) {
                if (_scenario.Scenario.LoopPlayback) index %= _scenario.Steps.Count;
                else { index = _scenario.Steps.Count - 1; StopPlayback(); }
            }

            _step = index;
        }

        private static int WidthFor(int maxWidth) => maxWidth >= Constraints.Unbounded / 2 ? 120 : Math.Max(20, maxWidth);

        private static TopologyHealthStatus? TryStatus(string? text) {
            if (string.IsNullOrWhiteSpace(text)) return null;
            switch (text!.Trim().ToLowerInvariant()) {
                case "healthy": case "pass": case "ok": case "up": return TopologyHealthStatus.Healthy;
                case "warning": case "warn": case "degraded": return TopologyHealthStatus.Warning;
                case "critical": case "fail": case "failed": case "down": case "error": return TopologyHealthStatus.Critical;
                case "disabled": case "skip": case "off": return TopologyHealthStatus.Disabled;
                case "unknown": case "none": return TopologyHealthStatus.Unknown;
                default: return null;
            }
        }
    }
}
