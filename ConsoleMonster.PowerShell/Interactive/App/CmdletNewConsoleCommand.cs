using System;
using System.Management.Automation;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Defines an in-app action for buttons, palettes and grid rows.</para>
    /// <para type="description">Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 requests cancellation while the app stays open. After a two-second grace period the app becomes available even when a native call is still stopping. That command cannot start again until its previous runspace exits; other commands remain available. Cancellation cannot undo effects already performed by an external operation.</para>
    /// </summary>
    /// <example><para>Preview restarting selected services after confirmation.</para><code>$restart = New-ConsoleCommand -Key Restart -Label 'Preview restart' -Shortcut Ctrl+R -ConfirmPrompt 'Preview selected services?' -ScriptBlock { param($Row) Restart-Service -Name $Row.Name -WhatIf }; Get-Service | Show-ConsoleGrid -Command $restart</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleCommand")]
    [OutputType(typeof(AppCommand))]
    public sealed class CmdletNewConsoleCommand : PSCmdlet {
        /// <summary><para type="description">Stable command identity.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] [ValidateNotNullOrEmpty] public string Key { get; set; } = string.Empty;
        /// <summary><para type="description">Visible command text; defaults to Key.</para></summary>
        [Parameter] public string? Label { get; set; }
        /// <summary><para type="description">Optional key chord, for example Ctrl+R or F5.</para></summary>
        [Parameter] public string? Shortcut { get; set; }
        /// <summary><para type="description">Optional modal confirmation text. {Selection} expands to the focused row label or marked-row count. No is the initial answer.</para></summary>
        [Parameter] public string? ConfirmPrompt { get; set; }
        /// <summary><para type="description">Shows a progress dialog with Cancel and Background, followed by the operation summary.</para></summary>
        [Parameter] public SwitchParameter ProgressDialog { get; set; }
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
