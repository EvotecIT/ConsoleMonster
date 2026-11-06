using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A live diagram with a dependency-free tree view and an optional Graphics topology renderer.</summary>
    /// <remarks>Configure collections before sharing the diagram with a session. Use Handle for concurrent live
    /// updates; direct collection edits are intended for sequential configuration, not concurrent writers.</remarks>
    public sealed partial class Diagram : LiveWidget {
        private IDiagramRenderer? _renderer;
        internal readonly object Gate = new object();
        /// <summary>Creates a diagram with a title and a live-update identity.</summary>
        public Diagram(string title = "", string widgetId = "diagram") : base(widgetId) { Title = title; }
        /// <summary>The diagram title.</summary>
        public string Title { get; set; }
        /// <summary>The reading direction.</summary>
        public DiagramDirection Direction { get; set; }
        /// <summary>Ungrouped components.</summary>
        public List<DiagramNode> Nodes { get; } = new List<DiagramNode>();
        /// <summary>Explicit connections.</summary>
        public List<DiagramLink> Links { get; } = new List<DiagramLink>();
        /// <summary>Top-level groups.</summary>
        public List<DiagramGroup> Groups { get; } = new List<DiagramGroup>();
        /// <summary>Selectable sequences of interactions.</summary>
        public List<DiagramUseCase> UseCases { get; } = new List<DiagramUseCase>();
        /// <summary>Lists every node and link whose status is not Pass, with unabridged text.</summary>
        public bool ShowSummary { get; set; } = true;
        /// <summary>Whether status meanings are shown below the topology.</summary>
        public bool ShowLegend { get; set; } = true;
        /// <summary>Whether the selected use case's steps are shown.</summary>
        public bool ShowStepPanel { get; set; } = true;
        /// <summary>Whether focused-node details are shown.</summary>
        public bool ShowDetails { get; set; } = true;
        /// <summary>Whether keyboard hints are shown.</summary>
        public bool ShowKeys { get; set; }
        /// <summary>The maximum visible payload lines.</summary>
        public int MaxPayloadLines { get; set; } = 8;
        /// <summary>The maximum visible step lines.</summary>
        public int MaxStepLines { get; set; } = 7;
        /// <summary>The selected use case id, or null.</summary>
        public string? ScenarioId => Renderer?.ScenarioId;
        /// <summary>The selected use case, or null.</summary>
        public DiagramUseCase? Scenario => UseCases.FirstOrDefault(useCase => useCase.Id == ScenarioId);
        /// <summary>A snapshot of component values, keyed by their explicit id or label.</summary>
        public IReadOnlyDictionary<string, string> Values { get { lock (Gate) { return Resolve().Where(node => node.Value != null).ToDictionary(node => node.Id, node => node.Value!, StringComparer.Ordinal); } } }
        /// <summary>The active zero-based step, or null.</summary>
        public int? StepIndex => Renderer?.StepIndex;
        /// <summary>The focused component id, or null.</summary>
        public string? FocusNodeId => Renderer?.FocusNodeId;
        /// <summary>Whether the use case is playing.</summary>
        public bool Playing => Renderer?.Playing ?? false;
        internal IDiagramRenderer? Renderer => _renderer ?? (_renderer = RendererFactory?.Invoke(this));
        internal void AttachRenderer(IDiagramRenderer renderer) { _renderer = renderer; }
        internal static Func<Diagram, IDiagramRenderer>? RendererFactory;
        internal static Func<string, string?, Diagram>? FileLoader;
        // Lets the Markdown add-on draw diagram fences (language, source) without referencing Graphics; null keeps the code block.
        internal static Func<string?, string, Diagram?>? FenceParser;
        internal IDiagramRenderer RequireRenderer() => Renderer ?? throw new NotSupportedException("Reference ConsoleMonster.Graphics and call DiagramRendererProvider.Register() to use topology files, playback or keyboard navigation.");
        /// <summary>Loads a ChartForgeX file using the registered Graphics provider.</summary>
        public static Diagram Load(string path, string? diagramId = null) => FileLoader != null ? FileLoader(path, diagramId) : throw new NotSupportedException("Call ConsoleMonster.Graphics.DiagramRendererProvider.Register() before Diagram.Load().");
        /// <summary>Creates a diagram from connection rows. Endpoints are created automatically; group selects the source's group.</summary>
        public static Diagram FromLinks<T>(IEnumerable<T> rows, Func<T, string> from, Func<T, string> to, Func<T, string?>? label = null, Func<T, StatusKind>? status = null, Func<T, string?>? group = null) {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            var diagram = new Diagram();
            foreach (T row in rows) {
                string source = from(row), target = to(row);
                var link = new DiagramLink(source, target) { Label = label?.Invoke(row) };
                if (status != null) link.Status = status(row);
                diagram.Links.Add(link);
                string? name = group?.Invoke(row);
                if (!string.IsNullOrEmpty(name) && !diagram.AllNodes().Any(n => n.Id == source)) {
                    DiagramGroup? owner = diagram.Groups.FirstOrDefault(g => g.Label == name);
                    if (owner == null) { owner = new DiagramGroup(name!); diagram.Groups.Add(owner); }
                    owner.Nodes.Add(new DiagramNode(source));
                }
            }
            diagram.Resolve();
            return diagram;
        }
        /// <summary>Posts live updates through a session.</summary>
        public DiagramHandle Handle(ILiveDispatcher dispatcher) => new DiagramHandle(dispatcher, WidgetId);
        /// <summary>Sets a component's third line and optionally its status.</summary>
        public void SetValue(string node, string? value, StatusKind? status = null) {
            lock (Gate) {
                DiagramNode? found = Resolve().FirstOrDefault(n => n.Id == node);
                if (found == null) throw new ArgumentException("Unknown diagram node: " + node, nameof(node));
                found.Value = value;
                if (status.HasValue) found.Status = status.Value;
            }
        }
        /// <summary>Selects a use case and clears its active step.</summary>
        public Diagram SelectScenario(string? id) { RequireRenderer().SelectScenario(id); return this; }
        /// <summary>Selects the next or previous use case.</summary>
        public void NextScenario(int delta = 1) => RequireRenderer().NextScenario(delta);
        /// <summary>Selects a zero-based step.</summary>
        public void Step(int index) => RequireRenderer().Step(index);
        /// <summary>Advances to the next step.</summary>
        public void NextStep() => RequireRenderer().NextStep();
        /// <summary>Moves to the previous step.</summary>
        public void PreviousStep() => RequireRenderer().PreviousStep();
        /// <summary>Selects the first step.</summary>
        public void FirstStep() => RequireRenderer().Step(0);
        /// <summary>Selects the last step.</summary>
        public void LastStep() => RequireRenderer().Step(int.MaxValue);
        /// <summary>Clears the active step.</summary>
        public void ClearStep() => RequireRenderer().ClearStep();
        /// <summary>Starts use-case playback.</summary>
        public void Play() => RequireRenderer().Play();
        /// <summary>Pauses use-case playback.</summary>
        public void Pause() => RequireRenderer().Pause();
        /// <summary>Toggles use-case playback.</summary>
        public void TogglePlay() { if (Playing) Pause(); else Play(); }
        /// <summary>Focuses a component, or clears focus with null.</summary>
        public void FocusNode(string? id) => RequireRenderer().FocusNode(id);
        /// <summary>Moves focus in reading order.</summary>
        public void FocusNext(int delta = 1) => RequireRenderer().FocusNext(delta);
        /// <summary>Handles a diagram key; false lets the host handle it.</summary>
        public bool HandleInput(InputEvent input) => Renderer?.HandleInput(input) ?? false;
        /// <summary>Opens the Graphics keyboard view, or prints a tree snapshot without Graphics.</summary>
        public AppState Run(ConsoleAppOptions? options = null, CancellationToken cancellationToken = default) {
            if (Renderer != null) return Renderer.Run(options, cancellationToken);
            ConsoleAppOptions settings = options ?? new ConsoleAppOptions();
            InteractiveOptions? original = settings.Interactive;
            InteractiveOptions snapshot = (original ?? InteractiveOptions.Default).Clone();
            snapshot.Mode = InteractionMode.Snapshot;
            try { settings.Interactive = snapshot; return ConsoleApp.Run(TreeView(), null, settings, cancellationToken); }
            finally { settings.Interactive = original; }
        }
        /// <summary>Exports the topology as PNG through Graphics.</summary>
        public byte[] ToPng() => RequireRenderer().ToPng();
        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) { lock (Gate) { Renderer?.Advance(context.Elapsed, context.ReducedMotion); return (Renderer?.View(constraints.MaxWidth, context) ?? TreeView(context)).Measure(constraints, context); } }
        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) { lock (Gate) { Renderer?.Advance(context.Elapsed, context.ReducedMotion); (Renderer?.View(region.Width, context) ?? TreeView(context)).Render(region, context); } }
        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            switch (liveEvent.Operation) {
                case "value": SetValue(liveEvent.Key ?? string.Empty, liveEvent.Text, StatusKinds.TryParse(liveEvent.Status, out StatusKind valueStatus) ? valueStatus : (StatusKind?)null); break;
                case "status": if (StatusKinds.TryParse(liveEvent.Status, out StatusKind status)) { lock (Gate) { var node = Resolve().FirstOrDefault(n => n.Id == liveEvent.Key); if (node != null) node.Status = status; } } break;
                case "scenario": SelectScenario(liveEvent.Text); break;
                case "step": Step((int)liveEvent.Value); break;
                case "focus": FocusNode(liveEvent.Key); break;
                case "play": Play(); break;
                case "pause": Pause(); break;
            }
        }
        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) => liveEvent.Operation == "value" ? liveEvent.Key + " = " + liveEvent.Text : null;
    }

    internal interface IDiagramRenderer {
        void Advance(TimeSpan elapsed, bool reducedMotion);
        IWidget View(int width, RenderContext context);
        IWidget? Panels(RenderContext context);
        string? ScenarioId { get; }
        int? StepIndex { get; }
        string? FocusNodeId { get; }
        bool Playing { get; }
        void SelectScenario(string? id);
        void NextScenario(int delta);
        void Step(int index);
        void NextStep();
        void PreviousStep();
        void ClearStep();
        void Play();
        void Pause();
        void FocusNode(string? id);
        void FocusNext(int delta);
        bool HandleInput(InputEvent input);
        AppState Run(ConsoleAppOptions? options, CancellationToken cancellationToken);
        byte[] ToPng();
    }
}
