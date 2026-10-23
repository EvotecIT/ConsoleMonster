using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>Completes the presenter names that <c>CellPresenters.FromName</c> understands (Bytes, Duration, Date...).</summary>
    public sealed class PresenterNameCompleter : IArgumentCompleter {
        private static readonly string[] Names = {
            "Auto", "Text", "Number", "Bytes", "Duration", "Date", "DateRelative", "DateBoth", "Boolean", "Status", "StatusWord",
            "Progress", "DataBar", "Sparkline", "Link", "Markup", "Emoji", "Identifier", "Chip", "MemberSignature"
        };

        /// <inheritdoc />
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) =>
            WordCompletion.Complete(Names, wordToComplete);
    }
}
