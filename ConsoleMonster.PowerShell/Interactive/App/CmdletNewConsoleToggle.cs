using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a named Boolean toggle for Show-ConsoleApp.</summary>
    /// <remarks><para>Emits a stable core Toggle widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para></remarks>
    /// <example><para>Create an application input.</para><code>
    /// $toggleOptions = @{
    ///     Key = 'Enabled'
    ///     Label = 'Enable checks'
    ///     Value = $true
    /// }
    /// New-ConsoleToggle @toggleOptions</code></example>
    /// <example>
    /// <para>Compose an inventory toggle</para>
    /// <code>
    /// Show-ConsoleApp -Content {
    ///     $toggleOptions = @{
    ///         Key = 'includeOffline'
    ///         Label = 'Include offline servers'
    ///     }
    ///     New-ConsoleToggle @toggleOptions
    /// }</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see New-ConsoleToggle in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>
    /// Get-ConsoleDemo -Slug 'settings-tabs' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'incident-workbench' | Select-Object -ExpandProperty Source</code>
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
