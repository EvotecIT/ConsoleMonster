using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace ConsoleMonster.PowerShell.Binding {
    internal sealed class ThemeNameAttribute : ValidateArgumentsAttribute {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics) {
            if (!(arguments is string text) || !ThemeNameCompleter.Names().Contains(text, StringComparer.OrdinalIgnoreCase)) {
                throw new ValidationMetadataException("Choose a built-in theme, a catalogue scheme or family, optionally followed by · accessible.");
            }
        }
    }

    internal sealed class ThemeNameCompleter : IArgumentCompleter {
        internal static IEnumerable<string> Names() => ConsoleSettings.Get("Theme").Values.Where(name => name != "Auto");

        internal static IEnumerable<CompletionResult> Complete(string typed, bool includeAuto = false) {
            string prefix = (typed ?? string.Empty).Trim('\'', '"');
            IEnumerable<string> names = includeAuto ? new[] { "Auto" }.Concat(Names()) : Names();
            return names.Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(name => new CompletionResult(name.Contains(" ") ? "'" + name + "'" : name, name, CompletionResultType.ParameterValue, name)).ToArray();
        }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) => Complete(wordToComplete);
    }
}
