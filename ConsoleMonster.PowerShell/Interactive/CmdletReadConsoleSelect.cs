using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Prompts for a selection and returns the selected objects.</summary>
    /// <remarks><para>Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-DisabledReason) and previewed (-Preview, beside the list when the window is wide). The selected objects are returned. In remoting and the ISE the choices are numbered and the host reads the answer.</para><para>The default title is &quot;Choose one&quot; for a single selection and &quot;Choose items&quot; with -Multiple. Use -NoSummary to omit the confirmation text after a successful selection.</para></remarks>
    /// <example>
    /// <para>Pick an object from a small inventory</para>
    /// <code>
    /// $servers = @(
    ///     [pscustomobject]@{ Name = 'web01'; Region = 'East'; Status = 'Pass' }
    ///     [pscustomobject]@{ Name = 'web02'; Region = 'West'; Status = 'Warn' }
    /// )
    /// $selectOptions = @{
    ///     Title = 'Server to inspect'
    ///     Display = 'Name'
    ///     Describe = 'Status'
    ///     GroupBy = 'Region'
    /// }
    /// $server = $servers | Read-ConsoleSelect @selectOptions</code>
    /// </example>
    /// <example>
    /// <para>Choose from strings with a default</para>
    /// <code>
    /// $selectOptions = @{
    ///     Title = 'Environment'
    ///     Choices = @('Production', 'Staging', 'Lab')
    ///     Default = 'Staging'
    /// }
    /// $environment = Read-ConsoleSelect @selectOptions</code>
    /// </example>
    /// <example>
    /// <para>Choose checks and omit the confirmation text</para>
    /// <code>$checks = Read-ConsoleSelect -Choices 'DNS', 'TLS', 'HTTP' -Multiple -NoSummary</code>
    /// </example>
    /// <example>
    /// <para>Read the gallery script to see Read-ConsoleSelect in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'prompt-tour' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleSelect", DefaultParameterSetName = "Single", HelpUri = "https://consolemonster.dev/powershell/commands/read-consoleselect/")]
    [PowerForge.Fluent.FluentCmdlet(typeof(global::ConsoleMonster.Interactive.SelectQuestionOptions), Include = new[] { "PageSize", "Minimum", "Maximum" })]
    public sealed partial class CmdletReadConsoleSelect : SelectionCmdlet {
        /// <summary><para type="description">The prompt displayed above the choices.</para></summary>
        [Parameter(Position = 0)]
        [PSDefaultValue(Help = "Choose one (Single) or Choose items (Multiple)")]
        public override string Title { get => base.Title; set => base.Title = value; }

        /// <summary><para type="description">The default: one of the choices, or its displayed text.</para></summary>
        [Parameter]
        public object? Default { get; set; }

        /// <summary><para type="description">Allows multiple answers; Default supplies initial selected choices.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Multiple")] [PSDefaultValue(Help = "$false")]
        public SwitchParameter Multiple { get; set; }

        /// <summary><para type="description">Omits the confirmation text after a successful selection. The selected objects are still returned.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoSummary { get; set; }

        /// <summary><para type="description">The fewest choices accepted.</para></summary>
        [Parameter(ParameterSetName = "Multiple")]
        public int? Minimum { get; set; }

        /// <summary><para type="description">The most choices accepted.</para></summary>
        [Parameter(ParameterSetName = "Multiple")]
        public int? Maximum { get; set; }

        /// <summary><para type="description">Starts with every group collapsed.</para></summary>
        [Parameter(ParameterSetName = "Multiple")]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Collapsed { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            bool multiple = Multiple.IsPresent;
            if (multiple && !MyInvocation.BoundParameters.ContainsKey(nameof(Title))) { Title = "Choose items"; }
            List<Choice> choices = BuildChoices(multiple ? Default : null);
            var definition = new SelectQuestionOptions {
                Title = Title, Description = Description, ParameterName = ParameterName, Multiple = multiple, ShowSummary = !NoSummary.IsPresent
            };
            foreach (Choice choice in choices) { definition.Choices.Add(choice); }
            if (Preview != null) { ScriptBlock preview = Preview; definition.Preview = value => PSInteractive.ToWidget(PSInteractive.InvokeWith(preview, value)); }
            ApplyParameters(definition);
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Collapsed))) { definition.Collapsed = Collapsed.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Default))) {
                if (multiple) { definition.Default = choices.FindAll(c => c.Selected).ConvertAll(c => c.Value); }
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
            if (multiple) { foreach (object? value in (IReadOnlyList<object?>)answer!) { WriteObject(value); } }
            else { WriteObject(answer); }
        }
    }
}
