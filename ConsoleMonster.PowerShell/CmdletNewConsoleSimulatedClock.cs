using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a run clock (ConsoleMonster.Live.SimulatedClock) the script moves forward, for simulated runs whose screen shows simulated time.</para>
    /// <para type="description">A live session reads the clock for the header clock, the summary bar's elapsed time and time left, the times of log lines and activities, and the run result's duration, so a demo or a replay of logged work shows its own time instead of the wall clock. Give it to a menu run as the third argument of $MenuContext.Run($dashboard, { ... }, $clock), or to Invoke-ConsoleDashboard -Clock. In the script, $clock.Wait(seconds) and $clock.WaitUntil(seconds) move it forward in small steps, sleeping 1/-Speed of each simulated second (never at speed 0, which runs at once); $clock.Advance(seconds) jumps without waiting and $clock.Hold(seconds) waits real time with the clock standing still. $clock.Elapsed is the time on the clock. Once the dashboard is stopped (Ctrl+C) waits stop sleeping, so the script reaches its end at once.</para>
    /// </summary>
    /// <example>
    ///   <para>A menu run on a simulated clock seven times faster than real time</para>
    ///   <code>
    /// New-ConsoleMenuItem 'Run checks' -Action {
    ///     $clock = New-ConsoleSimulatedClock -Speed 7
    ///     $dashboard = New-ConsoleDashboard -Title 'Checks' -Content { New-ConsoleSummaryBar -Total 3; New-ConsoleLogRegion -Height 5 }
    ///     $null = $MenuContext.Run($dashboard, {
    ///         foreach ($done in 1..3) {
    ///             $clock.Wait(4.5)
    ///             Update-ConsoleSummaryBar -Done $done -Counter @{ passed = $done }
    ///             Write-ConsoleLogEntry "Check $done passed"
    ///         }
    ///         New-ConsoleMenuRunResult 'Checks finished'
    ///     }, $clock)
    /// }</code>
    /// </example>
    /// <example>
    ///   <para>A dashboard replaying logged work at its recorded pace, ten times faster</para>
    ///   <code>
    /// $clock = New-ConsoleSimulatedClock -Speed 10
    /// Invoke-ConsoleDashboard $dashboard -Clock $clock -ArgumentList $clock, $entries {
    ///     param($Clock, $Entries)
    ///     foreach ($entry in $Entries) { $Clock.WaitUntil($entry.Seconds); Write-ConsoleLogEntry $entry.Text }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleSimulatedClock")]
    [OutputType(typeof(SimulatedClock))]
    public sealed class CmdletNewConsoleSimulatedClock : PSCmdlet {
        /// <summary><para type="description">Simulated seconds per real second while the script waits (default 1, real time); 0 never sleeps.</para></summary>
        [Parameter(Position = 0)]
        [ValidateRange(0, double.MaxValue)]
        public double Speed { get; set; } = 1;

        /// <inheritdoc />
        protected override void EndProcessing() {
            WriteObject(new SimulatedClock(Speed));
        }
    }
}
