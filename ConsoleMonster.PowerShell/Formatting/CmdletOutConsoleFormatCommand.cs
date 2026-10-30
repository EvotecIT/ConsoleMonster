using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Renders native PowerShell formatting records through ConsoleMonster.</para>
    /// <para type="description">The host-only sink used by Enable-ConsoleFormatting -Include FormatCommands. PowerShell still resolves properties, calculated expressions, grouping and named views. Unrecognized formatting records are passed to the native Out-Default. Use -AsWidget to export the same view with Export-ConsoleFrame.</para>
    /// </summary>
    /// <example>
    /// <para>Export a table through the public frame exporter</para>
    /// <code>Get-Process | Select-Object -First 5 Name, Id | New-ConsoleTable | Export-ConsoleFrame -Format Html</code>
    /// </example>
    internal sealed class CmdletOutConsoleFormatCommand : PSCmdlet {
        private readonly List<PSObject> _records = new List<PSObject>();

        /// <summary><para type="description">Native Format-Table, Format-List or Format-Wide output records.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Returns the view widget for composition or export.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <summary>The host proxy's evaluated values, retained before native string formatting.</summary>
        [Parameter(DontShow = true)]
        public NativeFormatCapture? Capture { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) { _records.Add(InputObject); }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_records.Count == 0) { return; }
            if (NativeWideView.TryBuild(this, _records, out IWidget? widget) || NativeFormatView.TryBuild(_records, out widget, Capture)) {
                if (AsWidget.IsPresent) { WriteObject(widget); }
                else { PSFormatting.Write(this, widget!, 0); }
                return;
            }
            using (SteppablePipeline native = ScriptBlock.Create("Microsoft.PowerShell.Core\\Out-Default").GetSteppablePipeline(CommandOrigin.Internal)) {
                native.Begin(this);
                foreach (PSObject record in _records) { native.Process(record); }
                native.End();
            }
        }
    }
}
