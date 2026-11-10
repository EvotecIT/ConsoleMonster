using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a named action button for Show-ConsoleApp.</summary>
    /// <remarks><para>Emits a stable core Button widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.</para></remarks>
    /// <example><para>Create an application input.</para><code>New-ConsoleButton -Key Apply -OnClick { $State.Applied = $true }</code></example>
    /// <example>
    /// <para>Compose an inventory button</para>
    /// <code>Show-ConsoleApp -Content {
    ///     New-ConsoleButton -Key inspect -Label 'Inspect inventory' -OnClick { Write-ConsoleLogEntry 'Inspection requested' }
    /// }
    /// </code>
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
