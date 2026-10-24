using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named action button for Show-ConsoleApp.</para>
    /// <para type="description">Emits a stable core Button widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para>
    /// </summary>
    /// <example><para>Create an application input.</para><code>New-ConsoleButton -Key Apply -OnClick { $State.Applied = $true }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleButton")]
    [OutputType(typeof(Button))]
    public sealed class CmdletNewConsoleButton : AppInputCmdlet {
        /// <summary><para type="description">Runs once per activation on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control; Value is the activation count. Output is discarded.</para></summary>
        [Parameter]
        public ScriptBlock? OnClick { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Emit(new Button(Key, Label), OnClick);
        }
    }
}
