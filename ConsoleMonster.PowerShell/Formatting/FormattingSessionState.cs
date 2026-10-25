using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.PowerShell {
    /// <summary>Mutable formatting installation state belongs to the runspace whose commands are being replaced.</summary>
    internal sealed class FormattingSessionState {
        private static readonly ConditionalWeakTable<Runspace, FormattingSessionState> Sessions = new ConditionalWeakTable<Runspace, FormattingSessionState>();

        internal static FormattingSessionState Current => Sessions.GetValue(
            Runspace.DefaultRunspace ?? throw new InvalidOperationException("Formatting configuration requires a PowerShell runspace."),
            owner => new FormattingSessionState(owner));

        private FormattingSessionState(Runspace owner) {
            Progress = new RunspaceProgressState(owner);
            owner.StateChanged += OnOwnerStateChanged;
        }

        // Use the captured installations: a Closed/Broken callback need not have a default runspace.
        private void OnOwnerStateChanged(object? sender, RunspaceStateEventArgs args) {
            if (args.RunspaceStateInfo.State != RunspaceState.Closed && args.RunspaceStateInfo.State != RunspaceState.Broken) { return; }
            Streams.Restore();
            ReadLine.Restore();
            if (sender is Runspace owner) { owner.StateChanged -= OnOwnerStateChanged; }
        }

        internal bool TakeOverBuiltIns { get; set; } = true;
        internal ConsoleFormattingFeature Features { get; set; }
        internal bool DetailedErrors { get; set; }
        internal ScriptBlock? SavedOutDefault { get; set; }
        internal bool ProgressEnabled { get; set; }
        internal RunspaceProgressState Progress { get; }
        internal ScriptBlock? ProgressCleanupHook { get; set; }
        internal ScriptBlock? PreviousModuleOnRemove { get; set; }

        internal FormatCommandInstallation? FormatCommands { get; set; }
        internal FormatCommandInstallation? NativeProgress { get; set; }
        internal FormatCommandInstallation? Help { get; set; }
        internal GridViewInstallation? GridView { get; set; }
        internal StreamFormattingInstallation Streams { get; } = new StreamFormattingInstallation();
        internal ReadLineFormattingInstallation ReadLine { get; } = new ReadLineFormattingInstallation();
        internal PromptInstallation? Prompt { get; set; }

        internal Dictionary<string, ConsoleViewRegistration> Views { get; } = new Dictionary<string, ConsoleViewRegistration>(StringComparer.OrdinalIgnoreCase);
        internal HashSet<string> ViewModules { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Ends this installation without changing any other runspace's saved commands.</summary>
        internal void Reset() {
            PromptInstallation.Remove(this);
            Streams.Restore();
            ReadLine.Restore();
            SessionProgress.Reset(Progress);
            Features = ConsoleFormattingFeature.None;
            DetailedErrors = false;
            ProgressEnabled = false;
            TakeOverBuiltIns = true;
            SavedOutDefault = null;
            ProgressCleanupHook = null;
            PreviousModuleOnRemove = null;
        }
    }
}
