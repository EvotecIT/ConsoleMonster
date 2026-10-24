using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named multiline text input for Show-ConsoleApp.</para>
    /// <para type="description">Emits a stable core TextArea widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para>
    /// </summary>
    /// <example><para>Create an application input.</para><code>New-ConsoleTextArea -Key Notes -Height 4 -Value 'Draft'</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleTextArea")]
    [OutputType(typeof(TextArea))]
    public sealed class CmdletNewConsoleTextArea : AppInputCmdlet {
        /// <summary><para type="description">Initial multiline text; empty by default.</para></summary>
        [Parameter]
        public string? Value { get; set; }

        /// <summary><para type="description">Muted hint while the value is empty.</para></summary>
        [Parameter]
        public string? Placeholder { get; set; }

        /// <summary><para type="description">Maximum text length in UTF-16 units.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary><para type="description">Number of visible text rows; defaults to six.</para></summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Height { get; set; } = 6;

        /// <inheritdoc />
        protected override void EndProcessing() {
            Emit(new TextArea(Key, Value) { Placeholder = Placeholder, MaxLength = MaxLength, Height = Height });
        }
    }
}
