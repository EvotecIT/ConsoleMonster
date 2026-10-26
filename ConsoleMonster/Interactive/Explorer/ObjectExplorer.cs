using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Data;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Browses object members lazily with the grid's keyboard and screen infrastructure.</summary>
    public sealed class ObjectExplorer {
        /// <summary>Creates an explorer without reading nested values.</summary>
        /// <param name="value">The object, dictionary, JSON value or collection to explore.</param>
        /// <param name="options">Member adapters and expansion limits.</param>
        /// <param name="rootPath">The root PowerShell expression used in returned paths.</param>
        public ObjectExplorer(object? value, InspectorOptions? options = null, string rootPath = "$x") {
            if (string.IsNullOrWhiteSpace(rootPath)) { throw new ArgumentException("A root expression is required.", nameof(rootPath)); }
            Options = options ?? new InspectorOptions();
            Root = new ExplorerNode(rootPath, rootPath, () => value, Options, null);
        }

        /// <summary>The lazily inspected root.</summary>
        public ExplorerNode Root { get; }

        /// <summary>The adapters and limits shared by each expansion.</summary>
        public InspectorOptions Options { get; }

        /// <summary>The title in the screen header.</summary>
        public string Title { get; set; } = "Object explorer";

        /// <summary>Creates a deterministic, non-interactive initial view for previews and capture.</summary>
        /// <returns>The initial explorer screen as a widget.</returns>
        public IWidget Snapshot() => new ExplorerView(this, PromptLabels.Default).Snapshot();

        /// <summary>Runs the keyboard explorer, returning the selected node on Ctrl+Enter or F10.</summary>
        /// <param name="interactive">Input, output and theme options.</param>
        /// <param name="cancellationToken">Stops an active session.</param>
        /// <returns>The selected node, or null after cancellation or static fallback.</returns>
#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("The static fallback inspects object members. Supply member sources for trimmed applications.")]
#endif
        public ExplorerNode? Show(InteractiveOptions? interactive = null, CancellationToken cancellationToken = default) {
            InteractiveOptions actual = interactive ?? InteractiveOptions.Default;
            if (actual.ResolveMode() != InteractionMode.Rich) {
                Terminal.Write(new InspectorView(ObjectInspector.Inspect(Root.Value, Options), Title), actual.ResolveProfile());
                return null;
            }
            using (InputLease lease = ConsoleInput.Acquire(actual.Input)) {
                Capabilities.TerminalProfile profile = actual.ResolveProfile();
                RenderContext context = Terminal.CreateContext(profile, actual.ResolveTheme());
                IPromptSurface? surface = actual.Surface;
                bool owns = surface == null;
                surface ??= PromptHost.Resolve(context) ?? new ScreenSurface(profile);
                try {
                    ExplorerView view = new ExplorerView(this, actual.ResolveLabels());
                    InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    surface.Complete(null);
                    return view.Accepted ? view.Selected : null;
                } finally { if (owns) { surface.Dispose(); } }
            }
        }
    }
}
