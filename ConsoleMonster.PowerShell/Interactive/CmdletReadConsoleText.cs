using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks for a line of text.</para>
    /// <para type="description">A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validator check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.</para>
    /// </summary>
    /// <example>
    ///   <para>A validated name</para>
    ///   <code>$name = Read-ConsoleText 'Server name' -Default 'srv01' -Pattern '[a-z][a-z0-9-]{1,14}' -PatternMessage 'Lower case letters, digits and dashes'</code>
    /// </example>
    /// <example>
    ///   <para>A scriptblock validator ($_ is the answer; return $true, $false or an error message)</para>
    ///   <code>Read-ConsoleText 'Email' -Validator { if ($_ -notmatch '@') { 'Enter an email address' } }</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleText", DefaultParameterSetName = "Text")]
    [OutputType(typeof(string), typeof(System.Security.SecureString))]
    public sealed class CmdletReadConsoleText : NonSelectionPromptCmdlet {
        /// <summary><para type="description">The default answer.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public string? Default { get; set; }

        /// <summary><para type="description">Muted text in the empty box.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public string? Placeholder { get; set; }

        /// <summary><para type="description">Refuses an empty answer.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public SwitchParameter Required { get; set; }

        /// <summary><para type="description">The fewest characters.</para></summary>
        [Parameter]
        public int MinLength { get; set; }

        /// <summary><para type="description">The most characters.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary><para type="description">A regular expression the whole answer must match.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public string? Pattern { get; set; }

        /// <summary><para type="description">The message when -Pattern does not match.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public string? PatternMessage { get; set; }

        /// <summary><para type="description">A validator: $_ is the answer; return $true (or nothing) to accept, $false or an error message to refuse.</para></summary>
        [Parameter]
        public ScriptBlock? Validator { get; set; }

        /// <summary><para type="description">Earlier answers offered with Up and Down (oldest first).</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public string[]? History { get; set; }

        /// <summary><para type="description">Puts the default in the box (editable) instead of showing it as a hint.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public SwitchParameter PrefillDefault { get; set; }

        /// <summary><para type="description">Asks for several lines of text in a text area: soft wrap, Enter for a new line, Ctrl+Enter (or Ctrl+S) to save, paste keeps line breaks. In line mode the answer ends with an empty line.</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public SwitchParameter Multiline { get; set; }

        /// <summary><para type="description">The most lines (-Multiline).</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public int MaxLines { get; set; } = int.MaxValue;

        /// <summary><para type="description">The visible rows of the text area (-Multiline, default 6).</para></summary>
        [Parameter(ParameterSetName = "Text")]
        public int Height { get; set; } = 6;

        /// <summary><para type="description">Reads a masked secret and returns SecureString by default. History and defaults are forbidden.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Secret")] public SwitchParameter AsSecret { get; set; }
        /// <summary><para type="description">With AsSecret, explicitly returns plaintext.</para></summary>
        [Parameter(ParameterSetName = "Secret")] public SwitchParameter AsPlainText { get; set; }
        /// <summary><para type="description">With AsSecret, hides even the typed length.</para></summary>
        [Parameter(ParameterSetName = "Secret")] public SwitchParameter Hidden { get; set; }
        /// <summary><para type="description">With AsSecret, accepts an empty answer.</para></summary>
        [Parameter(ParameterSetName = "Secret")] public SwitchParameter AllowEmpty { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (AsPlainText.IsPresent && !AsSecret.IsPresent) { throw new PSArgumentException("AsPlainText requires AsSecret."); }
            var options = new TextQuestionOptions { Title = Title, Description = Description, ParameterName = ParameterName };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(AsSecret))) { options.AsSecret = AsSecret.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Multiline))) { options.Multiline = Multiline.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Default))) { options.Default = Default; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Placeholder))) { options.Placeholder = Placeholder; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Required))) { options.Required = Required.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(MinLength))) { options.MinLength = MinLength; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(MaxLength))) { options.MaxLength = MaxLength; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Pattern))) { options.Pattern = Pattern; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(PatternMessage))) { options.PatternMessage = PatternMessage; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(PrefillDefault))) { options.PrefillDefault = PrefillDefault.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(MaxLines))) { options.MaxLines = MaxLines; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Height))) { options.Height = Height; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Hidden))) { options.Hidden = Hidden.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(AllowEmpty))) { options.AllowEmpty = AllowEmpty.IsPresent; }
            if (History != null) { foreach (string entry in History) { options.History.Add(entry); } }
            if (Validator != null) {
                ScriptBlock validate = Validator;
                options.Validator = text => PSInteractive.Validation(validate, text, PromptLabels.Default);
            }
            Prompt<string> prompt = Question.Text(options);
            if (TrySnapshot(prompt)) { return; }
            string answer = Ask(prompt);
            WriteObject(AsSecret.IsPresent && !AsPlainText.IsPresent ? (object)PSInteractive.ToSecureString(answer) : answer);
        }
    }
}
