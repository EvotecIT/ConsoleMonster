using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks for a line of text.</para>
    /// <para type="description">A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validate check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.</para>
    /// <example>
    ///   <para>A validated name</para>
    ///   <code>$name = Read-ConsoleText 'Server name' -Default 'srv01' -Pattern '[a-z][a-z0-9-]{1,14}' -PatternMessage 'Lower case letters, digits and dashes'</code>
    /// </example>
    /// <example>
    ///   <para>A scriptblock validator ($_ is the answer; return $true, $false or an error message)</para>
    ///   <code>Read-ConsoleText 'Email' -Validate { if ($_ -notmatch '@') { 'Enter an email address' } }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Read, "ConsoleText")]
    [OutputType(typeof(string))]
    public sealed class CmdletReadConsoleText : PromptCmdlet {
        /// <summary><para type="description">The default answer.</para></summary>
        [Parameter]
        public string? Default { get; set; }

        /// <summary><para type="description">Muted text in the empty box.</para></summary>
        [Parameter]
        public string? Placeholder { get; set; }

        /// <summary><para type="description">Refuses an empty answer.</para></summary>
        [Parameter]
        public SwitchParameter Required { get; set; }

        /// <summary><para type="description">The fewest characters.</para></summary>
        [Parameter]
        public int MinLength { get; set; }

        /// <summary><para type="description">The most characters.</para></summary>
        [Parameter]
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary><para type="description">A regular expression the whole answer must match.</para></summary>
        [Parameter]
        public string? Pattern { get; set; }

        /// <summary><para type="description">The message when -Pattern does not match.</para></summary>
        [Parameter]
        public string? PatternMessage { get; set; }

        /// <summary><para type="description">A validator: $_ is the answer; return $true (or nothing) to accept, $false or an error message to refuse.</para></summary>
        [Parameter]
        public ScriptBlock? Validate { get; set; }

        /// <summary><para type="description">Earlier answers offered with Up and Down (oldest first).</para></summary>
        [Parameter]
        public string[]? History { get; set; }

        /// <summary><para type="description">Puts the default in the box (editable) instead of showing it as a hint.</para></summary>
        [Parameter]
        public SwitchParameter Prefill { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            TextPrompt prompt = new TextPrompt(Title) {
                Placeholder = Placeholder,
                Required = Required.IsPresent,
                MinLength = MinLength,
                MaxLength = MaxLength,
                Pattern = Pattern,
                PatternMessage = PatternMessage,
                PrefillDefault = Prefill.IsPresent
            };
            Configure(prompt);
            if (Default != null) {
                prompt.Default = Default;
            }
            if (History != null) {
                foreach (string entry in History) {
                    prompt.History.Add(entry);
                }
            }
            if (Validate != null) {
                ScriptBlock validate = Validate;
                prompt.Validator = text => PSInteractive.Validation(validate, text, PromptLabels.Default);
            }
            WriteObject(Ask(prompt));
        }
    }
}
