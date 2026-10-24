using System.Collections;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.</para>
    /// <para type="description">Executes the same self-contained PowerShell source offered by Get-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot and -Step are passed to the demo; omitted parameters retain the script's defaults. Demo output and errors follow the normal PowerShell streams.</para>
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

            var parameters = new Hashtable();
            if (MyInvocation.BoundParameters.ContainsKey(nameof(StaticSnapshot))) { parameters[nameof(StaticSnapshot)] = StaticSnapshot; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Step))) { parameters[nameof(Step)] = Step; }
            ScriptBlock demo = ScriptBlock.Create(PSDemoResources.Script(slug));
            ScriptBlock invoke = ScriptBlock.Create("param($demo, $parameters) & $demo @parameters");
            foreach (PSObject output in InvokeCommand.InvokeScript(true, invoke, null, demo, parameters)) {
                WriteObject(output);
            }
        }
    }
}
