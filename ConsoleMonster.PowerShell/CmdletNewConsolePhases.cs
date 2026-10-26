using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a phase strip for pipelines: Restore › Build › Test › Pack.</para>
    /// <para type="description">Phases come from -Phase (names, all pending) or from a script block of Key/Label/Status records calls, each with its status and a detail such as a duration. The active phase is emphasised and finished phases show their detail. To add phases later, pipe the strip to Add-ConsoleItem; in a live dashboard, update phases with Update-Console, which also measures their duration.</para>
    /// </summary>
    /// <example>
    ///   <para>A run part-way through</para>
    ///   <code>
    /// New-ConsolePhases -Content {
    ///     [pscustomobject]@{ Label = 'Discover'; Status = 'Pass'; Detail = '2.1s' }
    ///     [pscustomobject]@{ Label = 'Collect'; Status = 'Pass'; Detail = '14s' }
    ///     [pscustomobject]@{ Label = 'Evaluate'; Status = 'Running'; Detail = '64%' }
    ///     [pscustomobject]@{ Label = 'Report' }
    /// } | Show-Console</code>
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
        [Parameter(Position = 1)]
        public object[]? Phase { get; set; }

        /// <summary><para type="description">A script block that emits the phases: Key/Label/Status records calls (or phase names).</para></summary>
        [Parameter(Position = 2)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Phases from the pipeline: Key/Label/Status records objects or names.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">The live name (default phases).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "phases";

        /// <summary><para type="description">Draws [01/05] counters with right-aligned results from each item's Detail. Wrapped labels hang under the label text; results move below when the complete label and result cannot share a row.</para></summary>
        [Parameter] public SwitchParameter StepCounter { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Phases phases = new Phases(WidgetId) { StepCounter = StepCounter.IsPresent };
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
