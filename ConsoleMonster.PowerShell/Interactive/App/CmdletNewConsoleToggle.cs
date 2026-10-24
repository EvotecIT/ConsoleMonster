using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named Boolean toggle for Show-ConsoleApp.</para>
    /// <para type="description">Emits a stable core Toggle widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para>
    /// </summary>
    /// <example><para>Create an application input.</para><code>New-ConsoleToggle -Key Enabled -Label 'Enable checks' -Value $true</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleToggle")]
    [OutputType(typeof(Toggle))]
    public sealed class CmdletNewConsoleToggle : AppInputCmdlet {
        /// <summary><para type="description">Initial Boolean value; false by default.</para></summary>
        [Parameter]
        public bool Value { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Emit(new Toggle(Key, Value));
        }
    }
}
