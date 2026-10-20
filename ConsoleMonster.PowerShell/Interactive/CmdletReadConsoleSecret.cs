using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks for a secret and returns a SecureString (or a string with -AsPlainText).</para>
    /// <para type="description">The typed text is masked (or not shown at all with -Hidden); Ctrl+R reveals it while held open. The answer never appears in the scrollback summary. In remoting and the ISE the host reads it (ReadLineAsSecureString).</para>
    /// </summary>
    /// <example>
    ///   <para>A password for a credential</para>
    ///   <code>$password = Read-ConsoleSecret 'Password for svc-backup'; $credential = New-Object pscredential 'svc-backup', $password</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleSecret")]
    [OutputType(typeof(System.Security.SecureString), typeof(string))]
    public sealed class CmdletReadConsoleSecret : PromptCmdlet {
        /// <summary><para type="description">Returns a plain string instead of a SecureString.</para></summary>
        [Parameter]
        public SwitchParameter AsPlainText { get; set; }

        /// <summary><para type="description">Shows nothing while typing (not even the length).</para></summary>
        [Parameter]
        public SwitchParameter Hidden { get; set; }

        /// <summary><para type="description">Accepts an empty answer.</para></summary>
        [Parameter]
        public SwitchParameter AllowEmpty { get; set; }

        /// <summary><para type="description">The fewest characters.</para></summary>
        [Parameter]
        public int MinLength { get; set; }

        /// <summary><para type="description">A validator: $_ is the secret; return $true, $false or an error message.</para></summary>
        [Parameter]
        public ScriptBlock? Validate { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            SecretPrompt prompt = new SecretPrompt(Title) {
                Display = Hidden.IsPresent ? SecretDisplay.Hidden : SecretDisplay.Masked,
                Required = !AllowEmpty.IsPresent,
                MinLength = MinLength
            };
            Configure(prompt);
            if (Validate != null) {
                ScriptBlock validate = Validate;
                prompt.Validator = text => PSInteractive.Validation(validate, text, PromptLabels.Default);
            }
            string secret = Ask(prompt);
            if (AsPlainText.IsPresent) {
                WriteObject(secret);
            } else {
                WriteObject(PSInteractive.ToSecureString(secret));
            }
        }
    }
}
