using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    public sealed partial class TopologyDiagram {
        internal bool ScrollKeyHints { get { lock (_gate) { return _scrollKeyHints; } } set { lock (_gate) { _scrollKeyHints = value; } } }
        private bool _scrollKeyHints;

        /// <summary>
        /// Handles a key: <c>p</c>/Space play or pause, <c>n</c>/Right next step, <c>b</c>/Left previous step,
        /// <c>r</c>/Home restart, <c>e</c>/End last step, <c>x</c> clear the step, <c>s</c>/Tab next use case (Shift+Tab
        /// previous), Down/Up focus the next or previous node, and <c>d</c> show or hide details.
        /// </summary>
        /// <param name="input">The input event.</param>
        /// <returns>True when the key was handled; step keys are left to the host when there is no use case.</returns>
        public bool HandleInput(InputEvent input) {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Kind != InputEventKind.Key || (input.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) != 0) return false;
            lock (_gate) {
                bool steps = _scenario != null && _scenario.Steps.Count > 0;
                switch (input.Key) {
                    case ConsoleKey.RightArrow: if (!steps) return false; NextStep(); return true;
                    case ConsoleKey.LeftArrow: if (!steps) return false; PreviousStep(); return true;
                    case ConsoleKey.Home: if (!steps) return false; FirstStep(); return true;
                    case ConsoleKey.End: if (!steps) return false; LastStep(); return true;
                    case ConsoleKey.Spacebar: if (!steps) return false; TogglePlay(); return true;
                    case ConsoleKey.DownArrow: return MoveFocus(1);
                    case ConsoleKey.UpArrow: return MoveFocus(-1);
                    case ConsoleKey.Tab: if (_chart.Scenarios.Count < 2) return false; NextScenario((input.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1); return true;
                }

                switch (char.ToLowerInvariant(input.Char)) {
                    case 'p': if (!steps) return false; TogglePlay(); return true;
                    case 'n': if (!steps) return false; NextStep(); return true;
                    case 'b': if (!steps) return false; PreviousStep(); return true;
                    case 'r': if (!steps) return false; FirstStep(); return true;
                    case 'e': if (!steps) return false; LastStep(); return true;
                    case 'x': if (!steps) return false; ClearStep(); return true;
                    case 's': if (_chart.Scenarios.Count < 2) return false; NextScenario(1); return true;
                    case 'd':
                        if (_chart.Nodes.Count == 0) return false;
                        if (_focus == null) { _showDetails = true; FocusNext(1); } else _showDetails = !_showDetails;
                        return true;
                    default: return false;
                }
            }
        }

        private bool MoveFocus(int delta) {
            if (_chart.Nodes.Count == 0) return false;
            string? before = _focus;
            FocusNext(delta);
            return _focus != before;
        }
    }

    /// <summary>Runs a <see cref="TopologyDiagram"/> as a keyboard app, or prints a snapshot when interaction is unavailable.</summary>
    public static class TopologyDiagramApp {
        /// <summary>Shows the diagram with keys, scrolling and live playback in a rich console. Otherwise prints one static snapshot without reading input.</summary>
        /// <remarks>The selected scenario's AutoPlay setting starts playback when the keyboard view opens. Static snapshots retain the current step and do not start autoplay.</remarks>
        /// <param name="diagram">The diagram.</param>
        /// <param name="options">Optional app options. They are restored when the app ends; an existing input handler still runs first.</param>
        /// <param name="cancellationToken">Cancels the app.</param>
        /// <returns>The final app state.</returns>
        public static AppState Run(this TopologyDiagram diagram, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default) {
            if (diagram == null) throw new ArgumentNullException(nameof(diagram));
            ConsoleAppOptions settings = options ?? new ConsoleAppOptions();
            InteractiveOptions? originalInteractive = settings.Interactive;
            InteractiveOptions interactive = (originalInteractive ?? InteractiveOptions.Default).Clone();
            if (interactive.ResolveMode() != InteractionMode.Rich) {
                interactive.Mode = InteractionMode.Snapshot;
                try {
                    settings.Interactive = interactive;
                    return ConsoleApp.Run(diagram, null, settings, cancellationToken);
                } finally {
                    settings.Interactive = originalInteractive;
                }
            }
            Func<AppState, InputEvent, bool>? previous = settings.HandleInput;
            KeyChord? nextFocus = settings.Keys.NextFocus;
            KeyChord? previousFocus = settings.Keys.PreviousFocus;
            bool keys = diagram.ShowKeys;
            bool scrollHints = diagram.ScrollKeyHints;
            var viewport = new DiagramViewport(diagram);
            try {
                cancellationToken.ThrowIfCancellationRequested();
                if (diagram.Scenario?.Scenario.AutoPlay == true) diagram.Play();
                // Tab switches use cases here; the diagram has no inputs to move focus between.
                if (IsTab(nextFocus)) settings.Keys.NextFocus = null;
                if (IsTab(previousFocus)) settings.Keys.PreviousFocus = null;
                settings.HandleInput = (state, input) => (previous != null && previous(state, input))
                    || viewport.HandleInput(input);
                diagram.ShowKeys = true;
                return ConsoleApp.Run(viewport, null, settings, cancellationToken);
            } finally {
                settings.HandleInput = previous;
                settings.Keys.NextFocus = nextFocus;
                settings.Keys.PreviousFocus = previousFocus;
                diagram.ShowKeys = keys;
                diagram.ScrollKeyHints = scrollHints;
            }
        }

        private sealed class DiagramViewport : IContainer {
            private readonly TopologyDiagram _diagram;
            private readonly ScrollView _scroll;
            private int _canScroll;

            internal DiagramViewport(TopologyDiagram diagram) { _diagram = diagram; _scroll = new ScrollView(diagram); }
            public IReadOnlyList<IWidget> Children => new IWidget[] { _scroll };
            public Size Measure(Constraints constraints, RenderContext context) => _scroll.Measure(constraints, context);
            public void Render(Region region, RenderContext context) {
                bool canScroll = _diagram.Measure(new Constraints(region.Width), context).Height > region.Height;
                Volatile.Write(ref _canScroll, canScroll ? 1 : 0);
                _diagram.ScrollKeyHints = canScroll;
                _scroll.Render(region, context);
            }

            internal bool HandleInput(InputEvent input) {
                bool scrollKey = input.Kind == InputEventKind.Key && input.Modifiers == KeyModifiers.None
                    && (input.Key == ConsoleKey.DownArrow || input.Key == ConsoleKey.UpArrow);
                if (Volatile.Read(ref _canScroll) == 0 || !scrollKey) return _diagram.HandleInput(input);
                int before = _scroll.ScrollOffset;
                if (input.Key == ConsoleKey.DownArrow) _scroll.ScrollBy(1);
                else if (input.Key == ConsoleKey.UpArrow) _scroll.ScrollBy(-1);
                else return false;
                return _scroll.ScrollOffset != before;
            }
        }

        private static bool IsTab(KeyChord? chord) => chord.HasValue && chord.Value.Key == ConsoleKey.Tab;
    }
}
