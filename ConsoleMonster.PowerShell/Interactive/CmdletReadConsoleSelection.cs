using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks the user to pick one of the choices and returns the chosen object.</para>
    /// <para type="description">Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-Disabled) and previewed (-Preview, beside the list when the window is wide). The chosen object itself is returned (a service, a record), not its text. In remoting and the ISE the choices are numbered and the host reads the answer.</para>
    /// </summary>
    /// <example>
    ///   <para>Pick a service</para>
    ///   <code>$service = Get-Service | Read-ConsoleSelection 'Service to restart' -Display DisplayName -Describe Name -GroupBy Status</code>
    /// </example>
    /// <example>
    ///   <para>Strings with a default</para>
    ///   <code>$environment = Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging', 'Lab' -Default 'Staging'</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleSelection")]
    public sealed class CmdletReadConsoleSelection : SelectionCmdlet {
        /// <summary><para type="description">The default: one of the choices, or its displayed text.</para></summary>
        [Parameter]
        public object? Default { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<Choice> choices = BuildChoices(null);
            if (choices.Count == 0) {
                ThrowTerminatingError(new ErrorRecord(new System.ArgumentException("There is nothing to choose from."), "NoChoices", ErrorCategory.InvalidArgument, Title));
            }
            SelectPrompt<object?> prompt = new SelectPrompt<object?>(Title, choices);
            if (Preview != null) {
                ScriptBlock preview = Preview;
                prompt.Preview = value => PSInteractive.ToWidget(PSInteractive.InvokeWith(preview, value));
            }
            if (PageSize > 0) {
                prompt.PageSize = PageSize;
            }
            Configure(prompt);
            Choice? fallback = PSInteractive.Find(choices, Default);
            if (fallback != null) {
                prompt.Default = fallback.Value;
            }
            if (TrySnapshot(prompt)) { return; }
            WriteObject(Ask(prompt));
        }
    }
}
