using System.Management.Automation;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows what ConsoleMonster detected about the current terminal.</para>
    /// <para type="description">Returns the terminal profile: kind, mode (Interactive, Stream, Plain), colour depth, glyph tier, size, VT support, console font and the reasons behind each decision. -Refresh detects again (after a resize or font change).</para>
    /// </summary>
    /// <example>
    ///   <para>Show the profile and the decisions</para>
    ///   <code>Get-ConsoleCapability | Select-Object -ExpandProperty Decisions</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "ConsoleCapability")]
    [OutputType(typeof(TerminalProfile))]
    public sealed class CmdletGetConsoleCapability : PSCmdlet {
        /// <summary><para type="description">Detect again instead of returning the cached profile.</para></summary>
        [Parameter]
        public SwitchParameter Refresh { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Refresh.IsPresent) {
                Terminal.Refresh();
            }
            WriteObject(Terminal.Profile);
        }
    }
}
