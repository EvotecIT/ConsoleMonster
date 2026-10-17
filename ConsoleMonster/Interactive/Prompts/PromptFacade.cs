using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// One-call prompts: <c>Prompt.Select("Target", servers, s =&gt; s.Name)</c>, <c>Prompt.Confirm("Continue?", true)</c>.
    /// For every option, build the prompt object (<see cref="SelectPrompt{T}"/>, <see cref="TextPrompt"/>, …) and call
    /// <c>Show</c>.
    /// </summary>
    public static class Prompt {
        /// <summary>Asks a yes/no question.</summary>
        /// <param name="title">The question.</param>
        /// <param name="defaultValue">The default answer, or null.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The answer.</returns>
        public static bool Confirm(string title, bool? defaultValue = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            ConfirmPrompt prompt = defaultValue.HasValue ? new ConfirmPrompt(title, defaultValue.Value) : new ConfirmPrompt(title);
            return prompt.Show(options, cancellationToken);
        }

        /// <summary>Asks a yes/no question on a worker thread.</summary>
        /// <param name="title">The question.</param>
        /// <param name="defaultValue">The default answer, or null.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The answer.</returns>
        public static Task<bool> ConfirmAsync(string title, bool? defaultValue = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            ConfirmPrompt prompt = defaultValue.HasValue ? new ConfirmPrompt(title, defaultValue.Value) : new ConfirmPrompt(title);
            return prompt.ShowAsync(options, cancellationToken);
        }

        /// <summary>Asks for a line of text.</summary>
        /// <param name="title">The question.</param>
        /// <param name="defaultValue">The default, or null.</param>
        /// <param name="validator">Returns an error message, or null when the answer is fine.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The answer.</returns>
        public static string Text(string title, string? defaultValue = null, Func<string, string?>? validator = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            TextPrompt prompt = new TextPrompt(title) { Validator = validator };
            if (defaultValue != null) {
                prompt.Default = defaultValue;
            }
            return prompt.Show(options, cancellationToken);
        }

        /// <summary>Asks for a secret (masked).</summary>
        /// <param name="title">The question.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The secret.</returns>
        public static string Secret(string title, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return new SecretPrompt(title).Show(options, cancellationToken);
        }

        /// <summary>Asks for a number.</summary>
        /// <param name="title">The question.</param>
        /// <param name="defaultValue">The default, or null.</param>
        /// <param name="minimum">The smallest value, or null.</param>
        /// <param name="maximum">The largest value, or null.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The number.</returns>
        public static decimal Number(string title, decimal? defaultValue = null, decimal? minimum = null, decimal? maximum = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            NumberPrompt prompt = new NumberPrompt(title) { Minimum = minimum, Maximum = maximum };
            if (defaultValue.HasValue) {
                prompt.Default = defaultValue.Value;
            }
            return prompt.Show(options, cancellationToken);
        }

        /// <summary>Asks for a whole number.</summary>
        /// <param name="title">The question.</param>
        /// <param name="defaultValue">The default, or null.</param>
        /// <param name="minimum">The smallest value, or null.</param>
        /// <param name="maximum">The largest value, or null.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The number.</returns>
        public static int Integer(string title, int? defaultValue = null, int? minimum = null, int? maximum = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            NumberPrompt prompt = new NumberPrompt(title) { WholeNumber = true, Minimum = minimum, Maximum = maximum };
            if (defaultValue.HasValue) {
                prompt.Default = defaultValue.Value;
            }
            return (int)prompt.Show(options, cancellationToken);
        }

        /// <summary>Picks one value.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="title">The question.</param>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value; null uses its text.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The value.</returns>
        public static T Select<T>(string title, IEnumerable<T> values, Func<T, string>? display = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return new SelectPrompt<T>(title, values, display).Show(options, cancellationToken);
        }

        /// <summary>Picks one value on a worker thread.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="title">The question.</param>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value; null uses its text.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The value.</returns>
        public static Task<T> SelectAsync<T>(string title, IEnumerable<T> values, Func<T, string>? display = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return new SelectPrompt<T>(title, values, display).ShowAsync(options, cancellationToken);
        }

        /// <summary>Picks any number of values.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="title">The question.</param>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value; null uses its text.</param>
        /// <param name="group">The group of a value; null for none.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The values in list order.</returns>
        public static IReadOnlyList<T> MultiSelect<T>(string title, IEnumerable<T> values, Func<T, string>? display = null, Func<T, string?>? group = null, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return new MultiSelectPrompt<T>(title, values, display, group).Show(options, cancellationToken);
        }

        /// <summary>Asks for a file or folder path.</summary>
        /// <param name="title">The question.</param>
        /// <param name="kind">What is accepted.</param>
        /// <param name="mustExist">Whether the path must exist.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The path as typed.</returns>
        public static string Path(string title, PathKind kind = PathKind.Any, bool mustExist = false, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return new PathPrompt(title) { Kind = kind, MustExist = mustExist }.Show(options, cancellationToken);
        }

        /// <summary>Asks the user to type a text before a destructive action.</summary>
        /// <param name="title">The question.</param>
        /// <param name="expected">The text to type.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>True when confirmed.</returns>
        public static bool TypedConfirm(string title, string expected, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return new TypedConfirmPrompt(title, expected).Show(options, cancellationToken);
        }
    }
}
