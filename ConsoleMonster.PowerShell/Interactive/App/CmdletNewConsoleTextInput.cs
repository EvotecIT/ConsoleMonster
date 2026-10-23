using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named single-line text input for Show-ConsoleApp.</para>
    /// <para type="description">Emits a stable core TextInput widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para>
    /// </summary>
    /// <example><para>Create an application input.</para><code>New-ConsoleTextInput -Name Query -Placeholder 'Filter' -OnChange { $State.Edits = 1 + $State.Edits }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTextInput")]
    [OutputType(typeof(TextInput))]
    public sealed class CmdletNewConsoleTextInput : AppInputCmdlet {
        /// <summary><para type="description">Initial text; empty by default.</para></summary>
        [Parameter]
        public string? Value { get; set; }

        /// <summary><para type="description">Muted hint while the value is empty.</para></summary>
        [Parameter]
        public string? Placeholder { get; set; }

        /// <summary><para type="description">Maximum text length in UTF-16 units.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int MaxLength { get; set; } = int.MaxValue;

        /// <inheritdoc />
        protected override void EndProcessing() {
            Emit(new TextInput(Name, Value) { Placeholder = Placeholder, MaxLength = MaxLength });
        }
    }
}
