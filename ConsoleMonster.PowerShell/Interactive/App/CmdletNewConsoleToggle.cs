using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a named Boolean toggle for Show-ConsoleApp.</summary>
    /// <remarks><para>Emits a stable core Toggle widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para></remarks>
    /// <example><para>Create an application input.</para><code>New-ConsoleToggle -Key Enabled -Label 'Enable checks' -Value $true</code></example>
    /// <example>
    /// <para>Compose an inventory toggle</para>
    /// <code>Show-ConsoleApp -Content {
    ///     New-ConsoleToggle -Key includeOffline -Label 'Include offline servers'
    /// }
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleToggle", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoletoggle/")]
    [OutputType(typeof(Toggle))]
    public sealed class CmdletNewConsoleToggle : AppInputCmdlet {
        /// <summary><para type="description">Initial Boolean value; false by default.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public bool Value { get; set; }

        /// <summary><para type="description">Checked-state text; omit for the localized Yes label.</para></summary>
        [Parameter] public string? OnText { get; set; }
        /// <summary><para type="description">Unchecked-state text; omit for the localized No label.</para></summary>
        [Parameter] public string? OffText { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Emit(AppInput.Toggle(new ToggleInputOptions { Key = Key, Label = Label, Value = Value, OnText = OnText, OffText = OffText }));
        }
    }
}
