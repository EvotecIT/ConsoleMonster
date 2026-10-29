using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a display rebuilt from application state on the owning PowerShell input thread.</para>
    /// <para type="description">Content receives param($State) and injected $State as a native hashtable. Emit widgets or text using the ordinary layout commands. Input controls must remain in the stable outer layout. Added or assigned state keys write back through AppState; bindings should normally only read state.</para>
    /// </summary>
    /// <example><para>Display the current input.</para><code>Show-ConsoleApp -Content { New-ConsoleTextInput -Key Query; New-ConsoleBinding -Content { "Query: $($State.Query)" } }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleBinding")]
    [OutputType(typeof(AppBinding))]
    public sealed class CmdletNewConsoleBinding : NoPipelineInputCmdlet {
        /// <summary><para type="description">Display builder, invoked initially and after state changes on the parent input thread.</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">Optional stable name for the dependent display region.</para></summary>
        [Parameter(Position = 0)]
        public string? WidgetId { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            PSAppScript script = new PSAppScript(Content);
            WriteObject(script.CreateBinding(state => new Rows(PSWidgets.Collect(script.Invoke(state)).ToArray()), WidgetId));
        }
    }
}
