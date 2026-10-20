using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Runspaces;
using System.Threading;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Activities and overrides belong to one runspace; its area may draw only while it owns the terminal.</summary>
    internal sealed class RunspaceProgressState {
        private readonly WeakReference<Runspace> _owner;
        private int _closed;

        internal RunspaceProgressState(Runspace owner) {
            _owner = new WeakReference<Runspace>(owner);
            owner.StateChanged += OnStateChanged;
            owner.AvailabilityChanged += OnAvailabilityChanged;
        }

        // Closing is still live: its pipeline and renderer may not have unwound yet. Avoid taking a runspace lock here.
        internal bool IsAlive => _owner.TryGetTarget(out _) && Volatile.Read(ref _closed) == 0;
        internal Stopwatch Clock { get; } = new Stopwatch();
        internal Dictionary<int, string> DrawnText { get; } = new Dictionary<int, string>();
        internal ActivityList? Activities { get; set; }
        internal InlineArea? Area { get; set; }
        internal long LastDraw { get; set; } = long.MinValue;
        internal bool? Supported { get; set; }
        internal ProgressBridge? Override { get; set; }
        internal int ForegroundScopes { get; set; }
        internal bool NativePipeline { get; set; }

        private void OnAvailabilityChanged(object? sender, RunspaceAvailabilityEventArgs args) {
            if (args.RunspaceAvailability == RunspaceAvailability.Available) {
                // Standalone Use-ConsoleProgress has no formatting shim to announce the top-level pipeline boundary.
                SessionProgress.PipelineEnded(this);
            }
        }

        private void OnStateChanged(object? sender, RunspaceStateEventArgs args) {
            if (args.RunspaceStateInfo.State == RunspaceState.Closed || args.RunspaceStateInfo.State == RunspaceState.Broken) {
                Volatile.Write(ref _closed, 1);
                SessionProgress.Close(this);
            }
        }
    }
}
