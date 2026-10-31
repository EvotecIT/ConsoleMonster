using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Live;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Where output written during a full-screen app goes (Console.WriteLine from a worker, PowerShell streams): the app's
    /// action output panel when it has commands, else its first log viewport, else it is kept and printed after the app
    /// restores the screen. A full-screen frame has no room below it, so such lines never land under the footer.
    /// </summary>
    internal sealed class AppLog : ILogTarget {
        private static long _next;
        private readonly AppCommandHost _actions;
        private readonly LogViewport? _viewport;
        private readonly List<string> _kept = new List<string>();

        internal AppLog(AppCommandHost actions, LogViewport? viewport) {
            _actions = actions;
            _viewport = viewport;
            WidgetId = "AppLog" + Interlocked.Increment(ref _next);
        }

        public string WidgetId { get; }

        public void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation != "write" || liveEvent.Text == null) { return; }
            string level = liveEvent.Status ?? "out";
            if (_actions.HasCommands) { _actions.WriteOutput(liveEvent.Text, level); }
            else if (_viewport != null) { _viewport.Write(liveEvent.Text, level == "err" ? "error" : level == "out" ? "info" : level); }
            else { lock (_kept) { _kept.Add(liveEvent.Text); } }
        }

        public string? Describe(LiveEvent liveEvent) => null;

        // Lines with nowhere to show while the app ran, for the caller to print once the screen is restored.
        internal IReadOnlyList<string> Kept() { lock (_kept) { return _kept.ToArray(); } }
    }
}
