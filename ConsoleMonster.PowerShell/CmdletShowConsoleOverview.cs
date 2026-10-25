using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Overview;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a compact, read-only summary of the local host.</para>
    /// <para type="description">Collects CPU, memory, local fixed disks, network addresses and safely available services, update/reboot and security observations. Unavailable panels are hidden. Slow sections show a pending marker after the caller budget; each probe receives a two-second cooperative timeout. At 80 columns or more panels use two columns and at most 24 rows. Below 80 columns panels stack at their natural height, preserving volumes and login rows; terminal scrolling is allowed. No elevation, child processes, remote access or system changes are used. Security data requires existing read access. Linux update and security support depends on local update-notifier and auth.log facilities; macOS optional service, update and security panels are hidden. Register application sections through OverviewSectionRegistry.Default.Register. Custom providers must honor cancellation and release their resources.</para>
    /// </summary>
    /// <example><para>Show the local login overview.</para><code>Show-ConsoleOverview</code></example>
    /// <example><para>Return selected data for a script.</para><code>$snapshot = Show-ConsoleOverview -Section Resources,Disks -AsObject</code></example>
    /// <example><para>Capture a compact plain-text overview.</para><code>Show-ConsoleOverview -Width 60 -AsString</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleOverview", DefaultParameterSetName = "Render")]
    [OutputType(typeof(OverviewSnapshot), typeof(string))]
    public sealed class CmdletShowConsoleOverview : PSCmdlet {
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();

        /// <summary><para type="description">Registry section keys, in requested order. Built-ins: Resources, Disks, Network, Services, Updates, Security. Omit to collect all registered sections. Unknown keys produce an error.</para></summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string[]? Section { get; set; }

        /// <summary><para type="description">Returns the immutable OverviewSnapshot instead of drawing it.</para></summary>
        [Parameter(ParameterSetName = "Object")]
        public SwitchParameter AsObject { get; set; }

        /// <summary><para type="description">Layout width in cells; zero resolves the terminal width, with a 100-cell fallback.</para></summary>
        [Parameter(ParameterSetName = "Render")]
        [ValidateRange(0, 32767)]
        public int Width { get; set; }

        /// <summary><para type="description">Returns one plain-text string for capture or redirection on PowerShell 5.1 and 7.</para></summary>
        [Parameter(ParameterSetName = "Render")]
        public SwitchParameter AsString { get; set; }

        /// <summary><para type="description">The caller's collection budget in milliseconds, from 1 to 900; default 750. Slow sections become pending markers. Native scheduling and rendering time are outside this waiting budget.</para></summary>
        [Parameter]
        [ValidateRange(1, 900)]
        public int BudgetMilliseconds { get; set; } = 750;

        /// <summary><para type="description">Returns the view widget without drawing or acquiring input.</para></summary>
        [Parameter(ParameterSetName = "Render")]
        public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            PSOutputExport.Validate(this);
            try {
                OverviewSnapshot snapshot = new OverviewCollector().CollectAsync(new OverviewOptions {
                    Sections = Section,
                    CollectionBudget = TimeSpan.FromMilliseconds(BudgetMilliseconds)
                }, _stop.Token).GetAwaiter().GetResult();
                if (AsObject.IsPresent) { WriteObject(snapshot); }
                else if (AsWidget.IsPresent) { WriteObject(new OverviewWidget(snapshot)); }
                else { PSFormatting.Emit(this, new OverviewWidget(snapshot), Width, null, AsString.IsPresent); }
            } finally { _stop.Dispose(); }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try { _stop.Cancel(); } catch (ObjectDisposedException) { }
        }
    }
}
