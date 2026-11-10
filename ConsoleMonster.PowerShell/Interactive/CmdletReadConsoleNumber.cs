using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Asks for a number.</summary>
    /// <remarks><para>Whole numbers with -Integer (returns [int]), decimals otherwise (returns [decimal]). -Minimum and -Maximum are shown as a hint and checked; Up and Down step by -Step (PgUp and PgDn by ten steps).</para></remarks>
    /// <example>
    ///   <para>A port number</para>
    ///   <code>$port = Read-ConsoleNumber 'Port' -Integer -Minimum 1 -Maximum 65535 -Default 443</code>
    /// </example>
    /// <example>
    /// <para>Choose a bounded batch size</para>
    /// <code>$count = Read-ConsoleNumber -Title 'Servers per batch' -Default 5 -Minimum 1 -Maximum 20
    /// Write-ConsoleText "Batch size: $count"
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleNumber", HelpUri = "https://consolemonster.dev/powershell/commands/read-consolenumber/")]
    [OutputType(typeof(int), typeof(decimal))]
    public sealed class CmdletReadConsoleNumber : NonSelectionPromptCmdlet {
        /// <summary><para type="description">The numeric answer initially selected when the question opens.</para></summary>
        [Parameter]
        public decimal? Default { get; set; }

        /// <summary><para type="description">Lower bound accepted by the numeric editor, inclusive.</para></summary>
        [Parameter]
        public decimal? Minimum { get; set; }

        /// <summary><para type="description">Upper bound accepted by the numeric editor, inclusive.</para></summary>
        [Parameter]
        public decimal? Maximum { get; set; }

        /// <summary><para type="description">The step of Up and Down (default 1).</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "1")]
        public decimal Step { get; set; } = 1;

        /// <summary><para type="description">Accepts whole numbers only and returns [int].</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Integer { get; set; }

        /// <summary><para type="description">A validator: $_ is the number; return $true, $false or an error message.</para></summary>
        [Parameter]
        public ScriptBlock? Validator { get; set; }

        /// <summary><para type="description">Culture used to parse and display the answer; omitted uses the current culture.</para></summary>
        [Parameter] public System.Globalization.CultureInfo? Culture { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            NumberPrompt prompt = new NumberPrompt(Title) { Culture = Culture, Integer = Integer.IsPresent, Minimum = Minimum, Maximum = Maximum, Step = Step };
            Configure(prompt);
            if (Default.HasValue) {
                prompt.Default = Default.Value;
            }
            if (Validator != null) {
                ScriptBlock validate = Validator;
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
