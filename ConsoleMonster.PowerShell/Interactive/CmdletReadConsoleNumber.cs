using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks for a number.</para>
    /// <para type="description">Whole numbers with -Integer (returns [int]), decimals otherwise (returns [decimal]). -Minimum and -Maximum are shown as a hint and checked; Up and Down step by -Step (PgUp and PgDn by ten steps).</para>
    /// </summary>
    /// <example>
    ///   <para>A port number</para>
    ///   <code>$port = Read-ConsoleNumber 'Port' -Integer -Minimum 1 -Maximum 65535 -Default 443</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleNumber")]
    [OutputType(typeof(int), typeof(decimal))]
    public sealed class CmdletReadConsoleNumber : PromptCmdlet {
        /// <summary><para type="description">The default.</para></summary>
        [Parameter]
        public decimal? Default { get; set; }

        /// <summary><para type="description">The smallest value.</para></summary>
        [Parameter]
        public decimal? Minimum { get; set; }

        /// <summary><para type="description">The largest value.</para></summary>
        [Parameter]
        public decimal? Maximum { get; set; }

        /// <summary><para type="description">The step of Up and Down (default 1).</para></summary>
        [Parameter]
        public decimal Step { get; set; } = 1;

        /// <summary><para type="description">Accepts whole numbers only and returns [int].</para></summary>
        [Parameter]
        public SwitchParameter Integer { get; set; }

        /// <summary><para type="description">A validator: $_ is the number; return $true, $false or an error message.</para></summary>
        [Parameter]
        public ScriptBlock? Validate { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            NumberPrompt prompt = new NumberPrompt(Title) { WholeNumber = Integer.IsPresent, Minimum = Minimum, Maximum = Maximum, Step = Step };
            Configure(prompt);
            if (Default.HasValue) {
                prompt.Default = Default.Value;
            }
            if (Validate != null) {
                ScriptBlock validate = Validate;
                prompt.Validator = value => PSInteractive.Validation(validate, value, PromptLabels.Default);
            }
            if (TrySnapshot(prompt)) { return; }
            decimal result = Ask(prompt);
            if (Integer.IsPresent) {
                WriteObject((int)result);
            } else {
                WriteObject(result);
            }
        }
    }
}
