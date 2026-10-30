using System;
using System.Collections;
using System.Collections.Generic;
using ConsoleMonster.Live;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>Input, live rendering and fallback settings for one application run.</summary>
    public sealed class ConsoleAppOptions {
        /// <summary>Host navigation, cancellation and palette gestures. Set individual gestures to null to release them to the app registry.</summary>
        public AppHostKeys Keys { get; set; } = new AppHostKeys();
        /// <summary>Lets the terminal generate signals for Ctrl+C and Ctrl+backslash. False keeps signal keys as input events, including the Commander's Ctrl+backslash.</summary>
        public bool SignalKeys { get; set; }
        /// <summary>App-thread registry routing after modal and cancellation handling, before focused-input handling. Return true when consumed.</summary>
        public Func<AppState, InputEvent, bool>? HandleInput { get; set; }
        /// <summary>Optional terminal handoff service, attached only for this run.</summary>
        public TerminalHandoff? TerminalHandoff { get; set; }
        /// <summary>Optional context footer. Null uses the host's standard hints.</summary>
        public Layout.IWidget? Footer { get; set; }
        /// <summary>Optional typed modal form service for this run.</summary>
        public AppDialogs? Dialogs { get; set; }
        /// <summary>Optional app-thread bridge to the shared action runner for registry and dialog callbacks.</summary>
        public AppActions? Actions { get; set; }
        /// <summary>Optional minimum terminal width; zero permits any width.</summary>
        public int MinimumWidth { get; set; }
        /// <summary>Optional minimum terminal height; zero permits any height.</summary>
        public int MinimumHeight { get; set; }
        /// <summary>Passive commands shared by the app's buttons, palette and keyboard shortcuts.</summary>
        public IList<AppCommand> Command { get; } = new List<AppCommand>();
        /// <summary>Captures action rows on the app thread before confirmation. Null supplies an empty row set.</summary>
        public Func<AppState, IReadOnlyList<object?>>? Selection { get; set; }
        /// <summary>State key supplying one selected row or a collection to commands when Selection is unset.</summary>
        public string? SelectionKey { get; set; }

        internal IReadOnlyList<object?> CaptureSelection(AppState state) {
            if (Selection != null) { return Selection(state); }
            if (SelectionKey == null || !state.TryGetValue(SelectionKey, out object? value) || value == null) { return Array.Empty<object?>(); }
            var rows = new List<object?>();
            if (value is IEnumerable items && !(value is string) && !Data.DictionaryMemberSource.IsDictionary(value) && !(value is Layout.IWidget)) { foreach (object? item in items) { rows.Add(item); } }
            else { rows.Add(value); }
            return rows;
        }
        /// <summary>Optional mailbox for this run. The host drains it on the input thread and closes it on exit; create a new dispatcher for another run.</summary>
        public AppDispatcher? Dispatcher { get; set; }
        /// <summary>Shared prompt input, capture, capability, theme and language settings.</summary>
        public InteractiveOptions? Interactive { get; set; }

        /// <summary>Optional full-screen live-session configuration, set before the run begins. Inline mode is rejected because its moving origin cannot provide frame-local mouse coordinates.</summary>
        public LiveSessionOptions? Live { get; set; }

        /// <summary>Enables optional terminal mouse reporting for an interactive run. Keyboard operation is always available.</summary>
        public bool Mouse { get; set; } = true;

        /// <summary>The default width below which column layouts stack; explicitly configured column thresholds take precedence.</summary>
        public int ReflowBelow { get; set; } = 60;

        /// <summary>Called on the owning input thread before the next read, for hosts that dispatch callbacks on that thread.</summary>
        public Action<AppState>? ProcessEvents { get; set; }
    }
}
