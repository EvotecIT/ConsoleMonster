using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named select input for Show-ConsoleApp.</para>
    /// <para type="description">Choices use the shared prompt conversion and retain original objects as values. Keep the input in the stable outer layout. Disabled choices cannot be selected.</para>
    /// </summary>
    /// <example><para>Choose initial values.</para><code>New-ConsoleSelect -Key Environment -Choices 'Production', 'Staging' -Value Staging</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleSelect")]
    [OutputType(typeof(Select))]
    public sealed class CmdletNewConsoleSelect : AppChoiceCmdlet {
        /// <summary><para type="description">Initial selected value from the supplied choices. Omit to retain the core default selection.</para></summary>
        [Parameter]
        [AllowNull]
        [AllowEmptyCollection]
        public object? Value { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Select control = new Select(Key, BuildChoices());
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) { control.Value = Value; }
            Emit(control);
        }
    }
}
