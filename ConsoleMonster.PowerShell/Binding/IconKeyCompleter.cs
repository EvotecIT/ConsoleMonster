using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>Completes the icon catalog keys ConsoleMonster draws.</summary>
    internal sealed class IconKeyCompleter : IArgumentCompleter {
        private static readonly string[] Keys = ConsoleMonster.Glyphs.Icons.All.Select(icon => icon.Key).ToArray();

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) {
            string prefix = (wordToComplete ?? string.Empty).Trim('\'', '"');
            return Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(key => new CompletionResult(key, key, CompletionResultType.ParameterValue, key)).ToArray();
        }
    }
}
