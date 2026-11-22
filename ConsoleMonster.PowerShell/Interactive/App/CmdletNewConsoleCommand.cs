using System;
using System.Management.Automation;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Defines an in-app action for buttons, palettes and grid rows.</summary>
    /// <remarks><para>Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 requests cancellation while the app stays open. After a two-second grace period the app becomes available even when a native call is still stopping. That command cannot start again until its previous runspace exits; other commands remain available. Cancellation cannot undo effects already performed by an external operation.</para></remarks>
    /// <example>
    /// <para>Confirm before inspecting a selected object</para>
    /// <code>
    /// $commandOptions = @{
    ///     Key = 'Inspect'
    ///     Label = 'Inspect process'
    ///     Shortcut = 'Ctrl+I'
    ///     ConfirmPrompt = 'Inspect the selected process?'
    ///     ScriptBlock = {
    ///         param($Row)
    ///         $Row | Select-Object Name, Id
    ///     }
    /// }
    /// $inspect = New-ConsoleCommand @commandOptions
    /// $gridOptions = @{
    ///     Command = $inspect
    /// }
    /// Get-Process | Select-Object -First 8 | Show-ConsoleGrid @gridOptions</code>
    /// </example>
    /// <example>
    /// <para>Compose an inventory command</para>
    /// <code>
    /// $commandOptions = @{
    ///     Key = 'refresh'
    ///     Label = 'Refresh inventory'
    ///     Shortcut = 'F5'
    ///     ScriptBlock = {
    ///         $logEntryOptions = @{
    ///             Message = 'Inventory refreshed'
    ///             Level = 'Info'
    ///         }
    ///         Write-ConsoleLogEntry @logEntryOptions
    ///     }
    /// }
    /// $refresh = New-ConsoleCommand @commandOptions
    /// $appOptions = @{
    ///     Command = $refresh
    ///     Content = {
    ///         $textOptions = @{
    ///             Text = @('Press F5 to refresh')
    ///         }
    ///         New-ConsoleText @textOptions
    ///     }
    /// }
    /// Show-ConsoleApp @appOptions</code>
    /// </example>
    /// <example>
    /// <para>Read the gallery script to see New-ConsoleCommand in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'service-browser' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleCommand", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolecommand/")]
    [OutputType(typeof(AppCommand))]
    public sealed class CmdletNewConsoleCommand : NoPipelineInputCmdlet {
        /// <summary><para type="description">Unique key used to invoke or enable this command within the application.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] [ValidateNotNullOrEmpty] public string Key { get; set; } = string.Empty;
        /// <summary><para type="description">Visible command text; defaults to Key.</para></summary>
        [Parameter] public string? Label { get; set; }
        /// <summary><para type="description">Optional key chord, for example Ctrl+R or F5.</para></summary>
        [Parameter] public string? Shortcut { get; set; }
        /// <summary><para type="description">Optional modal confirmation text. {Selection} expands to the focused row label or marked-row count. No is the initial answer.</para></summary>
        [Parameter] public string? ConfirmPrompt { get; set; }
        /// <summary><para type="description">Shows a progress dialog with Cancel and Background, followed by the operation summary.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter ProgressDialog { get; set; }
        /// <summary><para type="description">Operation source, receiving param($Row, ...explicit arguments). Closure variables are not inherited.</para></summary>
        [Parameter(Mandatory = true)] public ScriptBlock ScriptBlock { get; set; } = null!;
        /// <summary><para type="description">Explicit values serialized when defining the command and passed after Row.</para></summary>
        [Parameter] public object[] ArgumentList { get; set; } = Array.Empty<object>();
        /// <summary><para type="description">Modules to import in the operation's isolated runspace.</para></summary>
        [Parameter] public string[] ImportModule { get; set; } = Array.Empty<string>();
        /// <inheritdoc />
        protected override void EndProcessing() {
            WriteObject(ConsoleHostCommands.Create(new ConsoleCommandOptions {
                Key = Key, Label = Label, Shortcut = Shortcut, ConfirmPrompt = ConfirmPrompt, ProgressDialog = ProgressDialog.IsPresent,
                ScriptBlock = ScriptBlock, ArgumentList = ArgumentList, ImportModule = ImportModule
            }));
        }
    }
}
