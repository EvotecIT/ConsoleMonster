using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a phase strip for pipelines: Restore › Build › Test › Pack.</para>
    /// <para type="description">Phases come from -Phase (names, all pending) or from a script block of New-ConsoleStatusItem calls, each with its status and a detail such as a duration. The active phase is emphasised and finished phases show their detail. To add phases later, pipe the strip to Add-ConsoleItem; in a live dashboard, update phases with Update-ConsolePhase, which also measures their duration.</para>
    /// </summary>
    /// <example>
    ///   <para>A run part-way through</para>
    ///   <code>
    /// New-ConsolePhases {
    ///     New-ConsoleStatusItem 'Discover' -Status Pass -Detail '2.1s'
    ///     New-ConsoleStatusItem 'Collect' -Status Pass -Detail '14s'
    ///     New-ConsoleStatusItem 'Evaluate' -Status Running -Detail '64%'
    ///     New-ConsoleStatusItem 'Report'
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Build phases for a live dashboard</para>
    ///   <code>New-ConsolePhases -Phase 'Restore', 'Build', 'Test', 'Pack'</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsolePhases")]
    [OutputType(typeof(Phases))]
    public sealed class CmdletNewConsolePhases : PSCmdlet {
        private readonly List<object> _piped = new List<object>();

        /// <summary><para type="description">The phase names in order, all pending (a script block here is taken as -Content).</para></summary>
        [Parameter(Position = 0)]
        public object[]? Phase { get; set; }

        /// <summary><para type="description">A script block that emits the phases: New-ConsoleStatusItem calls (or phase names).</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Phases from the pipeline: New-ConsoleStatusItem objects or names.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">The live name (default phases).</para></summary>
        [Parameter]
        public string Name { get; set; } = "phases";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Phases phases = new Phases(Name);
            ScriptBlock? content = Content;
            if (Phase != null) {
                foreach (object phase in Phase) {
                    if (PSText.Unwrap(phase) is ScriptBlock script) {
                        content = script;
                    } else {
                        phases.Set(PSWidgets.ToText(phase));
                    }
                }
            }
            PSItems.AddAll(this, phases, PSItems.Items(content));
            PSItems.AddAll(this, phases, _piped);
            WriteObject(phases);
        }
    }
}
