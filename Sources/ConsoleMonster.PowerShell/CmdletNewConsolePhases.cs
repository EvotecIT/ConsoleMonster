using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a phase strip for pipelines: Restore › Build › Test › Pack.</para>
    /// <para type="description">The active phase is emphasised and finished phases show their duration. Update phases live with Update-ConsolePhase.</para>
    /// <example>
    ///   <para>Build phases</para>
    ///   <code>New-ConsolePhases -Phase 'Restore', 'Build', 'Test', 'Pack'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsolePhases")]
    [OutputType(typeof(Phases))]
    public sealed class CmdletNewConsolePhases : PSCmdlet {
        /// <summary><para type="description">The phases in order.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string[] Phase { get; set; } = new string[0];

        /// <summary><para type="description">The live name (default phases).</para></summary>
        [Parameter]
        public string Name { get; set; } = "phases";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Phases phases = new Phases(Name);
            foreach (string phase in Phase) {
                phases.Set(phase);
            }
            WriteObject(phases);
        }
    }
}
