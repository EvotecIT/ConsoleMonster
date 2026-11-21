using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a named action button for Show-ConsoleApp.</summary>
    /// <remarks><para>Emits a stable core Button widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para></remarks>
    /// <example><para>Create an application input.</para><code>$buttonOptions = @{
    ///     Key = 'Apply'
    ///     OnClick = { $State.Applied = $true }
    /// }
    /// New-ConsoleButton @buttonOptions</code></example>
    /// <example>
    /// <para>Compose an inventory button</para>
    /// <code>Show-ConsoleApp -Content {
    ///     $buttonOptions = @{
    ///         Key = 'inspect'
    ///         Label = 'Inspect inventory'
    ///         OnClick = {
    ///             $logEntryOptions = @{
    ///                 Message = 'Inspection requested'
    ///             }
    ///             Write-ConsoleLogEntry @logEntryOptions
    ///         }
    ///     }
    ///     New-ConsoleButton @buttonOptions
    /// }</code>
    /// </example>
    /// <example>
    /// <para>Read the gallery script to see New-ConsoleButton in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'setup-basic' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleButton", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolebutton/")]
    [OutputType(typeof(Button))]
    public sealed class CmdletNewConsoleButton : AppInputCmdlet {
        /// <summary><para type="description">Runs once per activation on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control; Value is the activation count. Output is discarded.</para></summary>
        [Parameter]
        public ScriptBlock? OnClick { get; set; }

        /// <summary><para type="description">An in-app command definition shared with the palette and shortcuts. Do not combine with OnClick.</para></summary>
        [Parameter]
        public AppCommand? Command { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Command != null && OnClick != null) { throw new System.ArgumentException("Use Command or OnClick, not both."); }
            Emit(AppInput.Button(new ButtonInputOptions { Key = Key, Label = Label, Command = Command }), OnClick);
        }
    }
}
