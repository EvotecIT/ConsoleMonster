using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named multiselect input for Show-ConsoleApp.</para>
    /// <para type="description">Choices use the shared prompt conversion and retain original objects as values. Keep the input in the stable outer layout. Disabled choices cannot be selected.</para>
    /// </summary>
    /// <example><para>Choose initial values.</para><code>New-ConsoleMultiSelect -Name Sites -Choices 'Warsaw', 'Berlin' -Value Warsaw</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleMultiSelect")]
    [OutputType(typeof(MultiSelect))]
    public sealed class CmdletNewConsoleMultiSelect : AppChoiceCmdlet {
        /// <summary><para type="description">Initial selected values from the supplied choices. Omit to retain the core default selection.</para></summary>
        [Parameter]
        [AllowNull]
        [AllowEmptyCollection]
        public object[]? Value { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            MultiSelect control = new MultiSelect(Name, BuildChoices());
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) { control.Value = Value ?? new object[0]; }
            Emit(control);
        }
    }
}
