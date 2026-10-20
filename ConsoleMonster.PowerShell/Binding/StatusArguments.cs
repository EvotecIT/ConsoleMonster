using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// Accepts PowerShell's own status words for a <see cref="StatusKind"/> parameter (Success, Ok, Passed, Information,
    /// Warning, Error, Failed, Failure, Verbose, Debug, Skipped) as well as the enum names, through the core's one
    /// vocabulary (<see cref="StatusKinds.TryParse"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StatusKindTransformationAttribute : ArgumentTransformationAttribute {
        /// <inheritdoc />
        public override object? Transform(EngineIntrinsics engineIntrinsics, object? inputData) {
            object? value = inputData is PSObject wrapped ? wrapped.BaseObject : inputData;
            if (value == null || value is StatusKind) {
                return value;
            }
            if (value is string text) {
                if (StatusArguments.TryParse(text, out StatusKind status)) {
                    return status;
                }
                throw new ArgumentTransformationMetadataException("'" + text + "' is not a status. Use one of: " + string.Join(", ", StatusKinds.Words) + ".");
            }
            return value;
        }
    }

    /// <summary>Reads a status given as a <see cref="StatusKind"/>, an enum name or one of PowerShell's status words.</summary>
    internal static class StatusArguments {
        /// <summary>Reads a status; numbers are refused, so a count is never taken for a status.</summary>
        /// <param name="value">A <see cref="StatusKind"/> or its text.</param>
        /// <param name="status">The status read.</param>
        /// <returns>Whether the value is a status.</returns>
        public static bool TryParse(object? value, out StatusKind status) {
            object? unwrapped = value is PSObject wrapped ? wrapped.BaseObject : value;
            if (unwrapped is StatusKind kind) {
                status = kind;
                return true;
            }
            string text = Convert.ToString(unwrapped, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (Enum.TryParse(text, true, out status) && Enum.IsDefined(typeof(StatusKind), status) && !int.TryParse(text, out _)) {
                return true;
            }
            return StatusKinds.TryParse(text, out status);
        }
    }

    /// <summary>Completes status words for a <see cref="StatusKind"/> parameter.</summary>
    public sealed class StatusKindCompleter : IArgumentCompleter {
        /// <inheritdoc />
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) =>
            WordCompletion.Complete(StatusKinds.Words, wordToComplete);
    }

    /// <summary>
    /// Accepts PowerShell's words for a log level (Information, Warning, Error, Verbose, Debug, Success, Skipped) as well as
    /// info, warn, err, verbose, debug, pass and skip, and passes on the canonical name (<see cref="LogLevels.TryNormalize"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class LogLevelTransformationAttribute : ArgumentTransformationAttribute {
        /// <inheritdoc />
        public override object? Transform(EngineIntrinsics engineIntrinsics, object? inputData) {
            object? value = inputData is PSObject wrapped ? wrapped.BaseObject : inputData;
            if (value == null) {
                return null;
            }
            string text = value is StatusKind kind ? kind.ToString() : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (LogLevels.TryNormalize(text, out string level)) {
                return level;
            }
            throw new ArgumentTransformationMetadataException("'" + text + "' is not a log level. Use one of: " + string.Join(", ", LogLevels.Words) + ".");
        }
    }

    /// <summary>Completes log level words.</summary>
    public sealed class LogLevelCompleter : IArgumentCompleter {
        /// <inheritdoc />
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) =>
            WordCompletion.Complete(LogLevels.Words, wordToComplete);
    }

    internal static class WordCompletion {
        internal static IEnumerable<CompletionResult> Complete(IEnumerable<string> words, string? typed) {
            string prefix = (typed ?? string.Empty).Trim('\'', '"');
            return words
                .Where(word => word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(word => new CompletionResult(word, word, CompletionResultType.ParameterValue, word))
                .ToList();
        }
    }
}
