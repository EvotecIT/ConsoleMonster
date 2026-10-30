using System;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.Graphics {
    public sealed partial class TopologyDiagram {
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
                    case ConsoleKey.DownArrow: FocusNext(1); return true;
                    case ConsoleKey.UpArrow: FocusNext(-1); return true;
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
                        if (_focus == null) { _showDetails = true; FocusNext(1); } else _showDetails = !_showDetails;
                        return true;
                    default: return false;
                }
            }
        }
    }

    /// <summary>Runs a <see cref="TopologyDiagram"/> as a full-screen keyboard app.</summary>
    public static class TopologyDiagramApp {
        /// <summary>Shows the diagram until Escape, with its keys, key hints and live playback.</summary>
        /// <param name="diagram">The diagram.</param>
        /// <param name="options">Optional app options. They are restored when the app ends; an existing input handler still runs first.</param>
        /// <param name="cancellationToken">Cancels the app.</param>
        /// <returns>The final app state.</returns>
        public static AppState Run(this TopologyDiagram diagram, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default) {
            if (diagram == null) throw new ArgumentNullException(nameof(diagram));
            ConsoleAppOptions settings = options ?? new ConsoleAppOptions();
            Func<AppState, InputEvent, bool>? previous = settings.HandleInput;
            KeyChord? nextFocus = settings.Keys.NextFocus;
            KeyChord? previousFocus = settings.Keys.PreviousFocus;
            bool keys = diagram.ShowKeys;
            try {
                // Tab switches use cases here; the diagram has no inputs to move focus between.
                if (IsTab(nextFocus)) settings.Keys.NextFocus = null;
                if (IsTab(previousFocus)) settings.Keys.PreviousFocus = null;
                settings.HandleInput = (state, input) => (previous != null && previous(state, input)) || diagram.HandleInput(input);
                diagram.ShowKeys = true;
                return ConsoleApp.Run(diagram, null, settings, cancellationToken);
            } finally {
                settings.HandleInput = previous;
                settings.Keys.NextFocus = nextFocus;
                settings.Keys.PreviousFocus = previousFocus;
                diagram.ShowKeys = keys;
            }
        }

        private static bool IsTab(KeyChord? chord) => chord.HasValue && chord.Value.Key == ConsoleKey.Tab;
    }
}
