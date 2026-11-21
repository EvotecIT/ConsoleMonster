using System;
using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>Pauses dashboard work, or captures its next checkpoint during a recording.</summary>
    /// <remarks><para>Waits cancellably on the resolved live session and writes no output. A recording target with SkipDelays enabled captures earlier queued updates instead of waiting.</para></remarks>
    /// <example>
    /// <para>Update progress between cancellable pauses</para>
    /// <code>
    /// $dashboardOptions2 = @{
    ///     Title = 'Scan'
    ///     Content = {
    ///         $progressOptions = @{
    ///             WidgetId = 'overall'
    ///             Label = 'Targets'
    ///             Total = 3
    ///         }
    ///         New-ConsoleProgress @progressOptions
    ///     }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions2
    /// $dashboardOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         foreach ($count in 1..3) {
    ///             $updateConsoleOptions = @{
    ///                 WidgetId = 'overall'
    ///                 Value = $count
    ///             }
    ///             Update-Console @updateConsoleOptions
    ///             Wait-ConsoleDashboard -Seconds 0.25
    ///         }
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @dashboardOptions</code>
    /// </example>
    /// <example>
    /// <para>Pause briefly between live server updates</para>
    /// <code>
    /// $dashboardOptions2 = @{
    ///     Title = 'Inventory'
    ///     Content = { New-ConsoleLanes -WidgetId servers }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions2
    /// $dashboardOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         $updateConsoleOptions2 = @{
    ///             WidgetId = 'servers'
    ///             Key = 'web01'
    ///             Status = 'Running'
    ///         }
    ///         Update-Console @updateConsoleOptions2
    ///         Wait-ConsoleDashboard -Seconds 0.1
    ///         $updateConsoleOptions = @{
    ///             Complete = $true
    ///             WidgetId = 'servers'
    ///             Key = 'web01'
    ///             Status = 'Pass'
    ///         }
    ///         Update-Console @updateConsoleOptions
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @dashboardOptions</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see Wait-ConsoleDashboard in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>
    /// Get-ConsoleDemo -Slug 'build-steps' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'log-tail' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Wait, "ConsoleDashboard", HelpUri = "https://consolemonster.dev/powershell/commands/wait-consoledashboard/")]
    public sealed class CmdletWaitConsoleDashboard : NoPipelineLiveCmdlet {
        private volatile LiveSession? _waitingSession;
        private volatile bool _stopping;

        /// <summary><para type="description">The pause in seconds, fractions allowed (0.25 is a quarter second); zero captures a checkpoint without waiting.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateRange(0, 2147483.0)]
        [PSDefaultValue(Help = "0")]
        public double Seconds { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            LiveSession? session = ResolveSession();
            if (session == null) {
                WriteError(new ErrorRecord(new PSInvalidOperationException("No console dashboard is running. Use Invoke-ConsoleDashboard first."),
                    "ConsoleMonsterNoSession", ErrorCategory.InvalidOperation, null));
                return;
            }
            _waitingSession = session;
            try {
                if (_stopping) { session.Cancel(); }
                session.Delay(TimeSpan.FromSeconds(Seconds));
            } catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested) {
                // Stopping a dashboard or its pipeline cancels the pause without adding pipeline output or errors.
            } finally {
                _waitingSession = null;
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            _stopping = true;
            _waitingSession?.Cancel();
        }
    }
}
