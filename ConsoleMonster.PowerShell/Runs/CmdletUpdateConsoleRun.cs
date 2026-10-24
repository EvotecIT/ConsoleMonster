using System;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Reports an item's running phase or final outcome to a run dashboard.</para>
    /// <para type="description">Index is the zero-based position in New-ConsoleRun's Items. Running starts the item or updates its phase without resetting elapsed time. Pass, Fail, Warn and Skip finish an item once; repeated completions do not print duplicate lines. Updates do not produce pipeline output or change the caller's error handling. For direct worker-thread use the handle exposes Started and Finished methods with the same contracts.</para>
    /// </summary>
    /// <example><para>Start and finish one item</para><code>Update-ConsoleRun $run -Index 0 -State Running -Phase 'collecting'; Update-ConsoleRun $run -Index 0 -State Warn -Message 'Response was incomplete'</code></example>
    [Cmdlet(VerbsData.Update, "ConsoleRun")]
    public sealed class CmdletUpdateConsoleRun : PSCmdlet {
        /// <summary><para type="description">The handle returned by New-ConsoleRun.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public LiveRun Run { get; set; } = null!;
        /// <summary><para type="description">Zero-based item position.</para></summary>
        [Parameter(Mandatory = true)] [ValidateRange(0, int.MaxValue)] public int Index { get; set; }
        /// <summary><para type="description">Running, Pass, Fail, Warn or Skip.</para></summary>
        [Parameter(Mandatory = true)] [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))] public StatusKind State { get; set; }
        /// <summary><para type="description">Literal phase text for a Running update.</para></summary>
        [Parameter] public string? Phase { get; set; }
        /// <summary><para type="description">Full completion detail, retained for the final summary.</para></summary>
        [Parameter] public string? Message { get; set; }
        /// <summary><para type="description">Optional measured duration. Default time since the Running update.</para></summary>
        [Parameter] public TimeSpan? Duration { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (State == StatusKind.Running) { Run.Started(Index, Phase); }
            else { Run.Finished(Index, State, Message, Duration); }
            LiveRunHostQueues.Drain(Run, this);
        }
    }
}
