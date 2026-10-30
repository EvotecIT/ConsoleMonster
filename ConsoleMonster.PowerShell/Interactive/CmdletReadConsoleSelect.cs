using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks the user to pick one of the choices and returns the chosen object.</para>
    /// <para type="description">Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-DisabledReason) and previewed (-Preview, beside the list when the window is wide). The chosen object itself is returned (a service, a record), not its text. In remoting and the ISE the choices are numbered and the host reads the answer.</para>
    /// </summary>
    /// <example>
    ///   <para>Pick a service</para>
    ///   <code>$service = Get-Service | Read-ConsoleSelect 'Service to restart' -Display DisplayName -Describe Name -GroupBy Status</code>
    /// </example>
    /// <example>
    ///   <para>Strings with a default</para>
    ///   <code>$environment = Read-ConsoleSelect 'Environment' -Choices 'Production', 'Staging', 'Lab' -Default 'Staging'</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleSelect", DefaultParameterSetName = "Single")]
    public sealed class CmdletReadConsoleSelect : SelectionCmdlet {
        /// <summary><para type="description">The default: one of the choices, or its displayed text.</para></summary>
        [Parameter]
        public object? Default { get; set; }

        /// <summary><para type="description">Allows multiple answers; Default supplies initial selected choices.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Multiple")] public SwitchParameter Multiple { get; set; }

        /// <summary><para type="description">The fewest choices accepted.</para></summary>
        [Parameter(ParameterSetName = "Multiple")]
        public int? Minimum { get; set; }

        /// <summary><para type="description">The most choices accepted.</para></summary>
        [Parameter(ParameterSetName = "Multiple")]
        public int? Maximum { get; set; }

        /// <summary><para type="description">Starts with every group collapsed.</para></summary>
        [Parameter(ParameterSetName = "Multiple")]
        public SwitchParameter Collapsed { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<Choice> choices = BuildChoices(Multiple.IsPresent ? Default : null);
            var definition = new SelectQuestionOptions { Title = Title, Description = Description, ParameterName = ParameterName, Multiple = Multiple.IsPresent };
            foreach (Choice choice in choices) { definition.Choices.Add(choice); }
            if (Preview != null) { ScriptBlock preview = Preview; definition.Preview = value => PSInteractive.ToWidget(PSInteractive.InvokeWith(preview, value)); }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(PageSize))) { definition.PageSize = PageSize; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Minimum))) { definition.Minimum = Minimum; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Maximum))) { definition.Maximum = Maximum; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Collapsed))) { definition.Collapsed = Collapsed.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Default))) {
                if (Multiple.IsPresent) { definition.Default = choices.FindAll(c => c.Selected).ConvertAll(c => c.Value); }
                else {
                    Choice? fallback = PSInteractive.Find(choices, Default);
                    if (fallback != null) { definition.Default = fallback.Value; }
                }
            }
            SelectionQuestion question = Question.Select(definition);
            if (Mode == InteractionMode.Snapshot) { PSFormatting.Write(this, question.Snapshot(Options()), 0); return; }
            object? answer;
            try { answer = question.Show(Options(), Token); }
            catch (System.Exception error) when (error is PromptCanceledException || error is PromptUnavailableException || error is Input.InputClosedException) { PSInteractive.Fail(this, error, Title); throw; }
            if (Multiple.IsPresent) { foreach (object? value in (IReadOnlyList<object?>)answer!) { WriteObject(value); } }
            else { WriteObject(answer); }
        }
    }
}
