using System;
using System.Collections.Generic;

using ConsoleMonster.Live;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Where output written during a full-screen app goes (Console.WriteLine from a worker, PowerShell streams): the app's
    /// action output panel when it has commands, else its first log viewport or log region, else the last thousand lines
    /// are kept and printed after the app restores the screen. A full-screen frame has no room below it, so such lines
    /// never land under the footer.
    /// </summary>
    internal sealed class AppLog : ILogTarget {
        internal const string Id = "AppLog";
        private const int KeepLimit = 1000;
        private readonly AppCommandHost _actions;
        private readonly LogViewport? _viewport;
        private readonly ILogTarget? _region;
        private readonly Queue<string> _kept = new Queue<string>();

        internal AppLog(AppCommandHost actions, LogViewport? viewport, ILogTarget? region) {
            _actions = actions;
            _viewport = viewport;
            _region = region;
        }

        public string WidgetId => Id;

        public void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation != "write" || liveEvent.Text == null) { return; }
            string level = liveEvent.Status ?? "out";
            if (_actions.HasCommands) { _actions.WriteOutput(liveEvent.Text, level); }
            else if (_viewport != null) { _viewport.Write(liveEvent.Text, level == "err" ? "error" : level == "out" ? "info" : level); }
            else if (_region != null) { _region.Apply(liveEvent); }
            else { lock (_kept) { if (_kept.Count == KeepLimit) { _kept.Dequeue(); } _kept.Enqueue(liveEvent.Text); } }
        }

        public string? Describe(LiveEvent liveEvent) => null;

        // Lines with nowhere to show while the app ran, for the caller to print once the screen is restored.
        internal IReadOnlyList<string> Kept() { lock (_kept) { return _kept.ToArray(); } }
    }
}
