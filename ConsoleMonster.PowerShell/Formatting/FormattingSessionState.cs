using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.PowerShell {
    /// <summary>Mutable formatting installation state belongs to the runspace whose commands are being replaced.</summary>
    internal sealed class FormattingSessionState {
        private static readonly ConditionalWeakTable<Runspace, FormattingSessionState> Sessions = new ConditionalWeakTable<Runspace, FormattingSessionState>();

        internal static FormattingSessionState Current => Sessions.GetValue(
            Runspace.DefaultRunspace ?? throw new InvalidOperationException("Formatting configuration requires a PowerShell runspace."),
            _ => new FormattingSessionState());

        internal bool TakeOverBuiltIns { get; set; } = true;
        internal ConsoleFormattingFeature Features { get; set; }
        internal ScriptBlock? SavedOutDefault { get; set; }
        internal string? SavedWriteProgressAlias { get; set; }
        internal bool ProgressEnabled { get; set; }
        internal ScriptBlock? ProgressCleanupHook { get; set; }
        internal ScriptBlock? PreviousModuleOnRemove { get; set; }

        /// <summary>Ends this installation without changing any other runspace's saved commands.</summary>
        internal void Reset() {
            if (ProgressEnabled) {
                SessionProgress.EndPipeline();
            }
            Features = ConsoleFormattingFeature.None;
            ProgressEnabled = false;
            TakeOverBuiltIns = true;
            SavedOutDefault = null;
            SavedWriteProgressAlias = null;
            ProgressCleanupHook = null;
            PreviousModuleOnRemove = null;
        }
    }
}
