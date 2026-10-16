using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes console widgets once (tables, trees, panels, charts, dashboards).</para>
    /// <para type="description">Renders each widget at the terminal width in the detected mode: VT colours and Unicode glyphs in a terminal, append-only lines when redirected, plain ASCII with CONSOLEMONSTER_MODE=plain. Override detection with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE or NO_COLOR.</para>
    /// <example>
    ///   <para>Print a panel with a table</para>
    ///   <code>New-ConsolePanel -Title 'Disks' { Get-Volume | Select-Object DriveLetter, FileSystemLabel, SizeRemaining | New-ConsoleTable -Border Minimal } | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleWidget")]
    public sealed class CmdletWriteConsoleWidget : PSCmdlet {
        /// <summary><para type="description">The widgets to write; strings are treated as markup.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public object[] Widget { get; set; } = new object[0];

        /// <summary><para type="description">Render at this width instead of the terminal width.</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (Width > 0) {
                profile = profile.WithSize(Width, profile.Height);
            }
            foreach (object item in Widget) {
                IWidget? widget = PSWidgets.AsWidget(item);
                if (widget == null) {
                    WriteError(new ErrorRecord(new PSArgumentException("Not a console widget: " + item), "ConsoleMonsterNotWidget", ErrorCategory.InvalidArgument, item));
                    continue;
                }
                HostOutput.Write(this, widget, profile, Width > 0);
            }
        }
    }
}
