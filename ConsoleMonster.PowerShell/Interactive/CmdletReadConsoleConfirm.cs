using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks a yes/no question and returns $true or $false.</para>
    /// <para type="description">Shows the question with the answers inline (the default focused); Y and N answer at once, arrows move, Enter takes the focused answer and Esc answers no. With -TypedConfirmation the user must type a text (a server name, a domain) before a destructive action; Esc then returns $false. In remoting and the ISE the host asks (PromptForChoice); without a user the default is returned.</para>
    /// </summary>
    /// <example>
    ///   <para>A question with a default</para>
    ///   <code>if (Read-ConsoleConfirm 'Restart the service now?' -Default $false) { Restart-Service Spooler -WhatIf }</code>
    /// </example>
    /// <example>
    ///   <para>A typed confirmation before deleting</para>
    ///   <code>if (Read-ConsoleConfirm 'Remove 3 servers from the inventory?' -TypedConfirmation 'inventory' -Warning 'This cannot be undone.') { Write-Host 'Would remove servers from the inventory' }</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleConfirm")]
    [OutputType(typeof(bool))]
    public sealed class CmdletReadConsoleConfirm : PromptCmdlet {
        /// <summary><para type="description">The default answer.</para></summary>
        [Parameter]
        public bool? Default { get; set; }

        /// <summary><para type="description">Marks the question as destructive (a warning mark; No is focused unless -Default says otherwise).</para></summary>
        [Parameter]
        public SwitchParameter Destructive { get; set; }

        /// <summary><para type="description">The text the user must type to confirm.</para></summary>
        [Parameter]
        public string? TypedConfirmation { get; set; }

        /// <summary><para type="description">Warning shown for a typed confirmation. Overrides -Description as the warning text; without -Warning the existing description remains the warning.</para></summary>
        [Parameter]
        public string? Warning { get; set; }

        /// <summary><para type="description">The caption of the positive answer.</para></summary>
        [Parameter]
        public string? YesText { get; set; }

        /// <summary><para type="description">The caption of the negative answer.</para></summary>
        [Parameter]
        public string? NoText { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (!string.IsNullOrEmpty(TypedConfirmation)) {
                TypedConfirmPrompt typed = new TypedConfirmPrompt(Title, TypedConfirmation!) { Warning = Warning ?? Description };
                typed.ParameterName = ParameterName;
                if (Default.HasValue) {
                    typed.Default = Default.Value;
                }
                if (TrySnapshot(typed)) { return; }
                WriteObject(Ask(typed));
                return;
            }
            ConfirmPrompt prompt = new ConfirmPrompt(Title) { Destructive = Destructive.IsPresent, YesText = YesText, NoText = NoText };
            Configure(prompt);
            if (Default.HasValue) {
                prompt.Default = Default.Value;
            }
            if (TrySnapshot(prompt)) { return; }
            WriteObject(Ask(prompt));
        }
    }
}
