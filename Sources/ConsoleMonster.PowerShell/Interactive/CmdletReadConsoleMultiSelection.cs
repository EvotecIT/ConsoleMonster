using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks the user to pick any number of the choices and returns the chosen objects.</para>
    /// <para type="description">Everything Read-ConsoleSelection does, plus checkboxes: Space toggles (on a group it toggles the whole group, shown as [–] when partly selected; Left and Right collapse and expand groups), Ctrl+A selects all shown, Ctrl+N none, Ctrl+T inverts, Ctrl+O shows only the selected ones. -Minimum and -Maximum are enforced, and a live count shows how many are selected. The chosen objects are written to the pipeline in list order.</para>
    /// <example>
    ///   <para>Grouped tasks with a preview</para>
    ///   <code>$tasks = Read-ConsoleMultiSelection 'Tasks' -Choices $tasks -Display Name -GroupBy Category -Preview { $_.Description } -Minimum 1</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Read, "ConsoleMultiSelection")]
    public sealed class CmdletReadConsoleMultiSelection : SelectionCmdlet {
        /// <summary><para type="description">Pre-selected choices: the objects or their displayed texts, or a scriptblock returning $true for selected ones.</para></summary>
        [Parameter]
        [Alias("Default")]
        public object? Selected { get; set; }

        /// <summary><para type="description">The fewest choices accepted.</para></summary>
        [Parameter]
        public int? Minimum { get; set; }

        /// <summary><para type="description">The most choices accepted.</para></summary>
        [Parameter]
        public int? Maximum { get; set; }

        /// <summary><para type="description">Starts with every group collapsed.</para></summary>
        [Parameter]
        public SwitchParameter Collapsed { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<Choice> choices = BuildChoices(Selected);
            MultiSelectPrompt<object?> prompt = new MultiSelectPrompt<object?>(Title, choices) { Minimum = Minimum, Maximum = Maximum, Collapsed = Collapsed.IsPresent };
            if (Preview != null) {
                ScriptBlock preview = Preview;
                prompt.Preview = value => PSInteractive.ToWidget(PSInteractive.InvokeWith(preview, value));
            }
            if (PageSize > 0) {
                prompt.PageSize = PageSize;
            }
            Configure(prompt);
            if (Selected != null) {
                List<object?> defaults = new List<object?>();
                foreach (Choice c in choices) {
                    if (c.Selected) {
                        defaults.Add(c.Value);
                    }
                }
                prompt.Default = defaults;
            }
            foreach (object? value in Ask(prompt)) {
                WriteObject(value);
            }
        }
    }
}
