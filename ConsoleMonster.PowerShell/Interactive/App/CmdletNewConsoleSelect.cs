using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named select input for Show-ConsoleApp.</para>
    /// <para type="description">Choices use the shared prompt conversion and retain original objects as values. Keep the input in the stable outer layout. Disabled choices cannot be selected.</para>
    /// </summary>
    /// <example><para>Choose initial values.</para><code>New-ConsoleSelect -Key Environment -Choices 'Production', 'Staging' -Value Staging</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleSelect")]
    [OutputType(typeof(Select), typeof(MultiSelect))]
    public sealed class CmdletNewConsoleSelect : AppChoiceCmdlet {
        /// <summary><para type="description">Initial selected value from the supplied choices. Omit to retain the core default selection.</para></summary>
        [Parameter]
        [AllowNull]
        [AllowEmptyCollection]
        public object? Value { get; set; }

        /// <summary><para type="description">Allows multiple selected values.</para></summary>
        [Parameter] public SwitchParameter Multiple { get; set; }
        /// <summary><para type="description">Shows single-choice items with radio indicators.</para></summary>
        [Parameter] public SwitchParameter Radio { get; set; }

        /// <summary><para type="description">Visible choice rows; omitted retains the control default.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] public int? PageSize { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            var options = new SelectInputOptions { Key = Key, Multiple = Multiple.IsPresent, PageSize = PageSize, Radio = Radio.IsPresent };
            foreach (Choice choice in BuildChoices()) { options.Choices.Add(choice); }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) {
                options.Value = Multiple.IsPresent && Value != null ? LanguagePrimitives.ConvertTo<object[]>(Value) : Value;
            }
            Emit(AppInput.Select(options));
        }
    }
}
