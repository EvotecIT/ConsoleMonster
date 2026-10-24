using ConsoleMonster.Canvas;
using ConsoleMonster.Rendering;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.</para>
    /// <para type="description">Executes the same self-contained PowerShell source offered by Export-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot selects a recorded state using scripted input or queued live updates controlled by the runner. The copied source has no capture parameters. Demo output and errors follow the normal PowerShell streams.</para>
    /// </summary>
    /// <example><para>Run the fleet demo.</para><code>Show-ConsoleDemo fleet</code></example>
    /// <example><para>Render a deterministic snapshot.</para><code>Show-ConsoleDemo assessment -StaticSnapshot -Step 0</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleDemo")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletShowConsoleDemo : PSCmdlet {
        /// <summary><para type="description">Exact demo slug from Get-ConsoleDemo.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Slug { get; set; } = string.Empty;

        /// <summary><para type="description">Renders one snapshot instead of running live interactions.</para></summary>
        [Parameter]
        public SwitchParameter StaticSnapshot { get; set; }

        /// <summary><para type="description">Deterministic snapshot state, starting at zero. See the demo's Steps metadata for its snapshot count.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Step { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            string? slug = PSDemoResources.Find(Slug);
            if (slug == null) {
                ThrowTerminatingError(new ErrorRecord(
                    new ItemNotFoundException("Unknown ConsoleMonster demo '" + Slug + "'. Use Get-ConsoleDemo to list available slugs."),
                    "ConsoleDemoNotFound", ErrorCategory.ObjectNotFound, Slug));
                return;
            }

            ScriptBlock demo = ScriptBlock.Create(PSDemoResources.Script(slug));
            if (StaticSnapshot.IsPresent) {
                ScriptBlock capture = ScriptBlock.Create(PSDemoResources.CaptureScript());
                ScriptBlock invokeCapture = ScriptBlock.Create("param($capture, $demo, $json, $step) & $capture -Demo $demo -Metadata (Microsoft.PowerShell.Utility\\ConvertFrom-Json -InputObject $json) -Step $step");
                foreach (PSObject result in InvokeCommand.InvokeScript(true, invokeCapture, null, capture, demo, PSDemoResources.Metadata(slug), Step)) {
                    if (result.BaseObject is CellBuffer frame) {
                        using (ITerminalOutput output = Terminal.CreateOutput(Terminal.GetCurrentProfile(), Terminal.Target.Writer)) {
                            output.WriteBlock(frame);
                        }
                    }
                }
                return;
            }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Step))) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("Use -Step with -StaticSnapshot."),
                    "ConsoleDemoSnapshotRequired", ErrorCategory.InvalidArgument, Step));
            }
            ScriptBlock invoke = ScriptBlock.Create("param($demo) & $demo");
            foreach (PSObject output in InvokeCommand.InvokeScript(true, invoke, null, demo)) {
                WriteObject(output);
            }
        }
    }
}
