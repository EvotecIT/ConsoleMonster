using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named single-line text input for Show-ConsoleApp.</para>
    /// <para type="description">Emits a stable core TextInput widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para>
    /// </summary>
    /// <example><para>Create an application input.</para><code>New-ConsoleTextInput -Key Query -Placeholder 'Filter' -OnChange { $State.Edits = 1 + $State.Edits }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTextInput", DefaultParameterSetName = "Single")]
    [OutputType(typeof(TextInput), typeof(TextArea))]
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

        /// <summary><para type="description">Creates a multiline text area.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Multiline")] public SwitchParameter Multiline { get; set; }
        /// <summary><para type="description">Visible rows for a multiline input; default six.</para></summary>
        [Parameter(ParameterSetName = "Multiline"), ValidateRange(1, int.MaxValue)] public int Height { get; set; } = 6;

        /// <summary><para type="description">The first visible multiline row.</para></summary>
        [Parameter(ParameterSetName = "Multiline"), ValidateRange(0, int.MaxValue)] public int ScrollRow { get; set; }
        /// <summary><para type="description">Single-line mask; an empty string hides the text entirely.</para></summary>
        [Parameter(ParameterSetName = "Single"), AllowEmptyString] public string? Mask { get; set; }
        /// <summary><para type="description">The single-line entered-text role.</para></summary>
        [Parameter(ParameterSetName = "Single")] public Styling.ThemeToken Role { get; set; } = Styling.ThemeToken.Text;

        /// <inheritdoc />
        protected override void EndProcessing() {
            var options = new TextInputOptions { Key = Key, Multiline = Multiline.IsPresent, Value = Value, Placeholder = Placeholder, MaxLength = MaxLength };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Height))) { options.Height = Height; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ScrollRow))) { options.ScrollRow = ScrollRow; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Mask))) { options.Mask = Mask; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Role))) { options.Role = Role; }
            Emit(AppInput.Text(options));
        }
    }
}
