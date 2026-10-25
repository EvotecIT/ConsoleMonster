using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>A question whose answer is one original value or a read-only collection, selected by Multiple.</summary>
    public sealed class SelectionQuestion {
        private readonly SelectPrompt<object?>? _single;
        private readonly MultiSelectPrompt<object?>? _multiple;
        internal SelectionQuestion(SelectQuestionOptions options) {
            if (options.PageSize < 0) { throw new ArgumentOutOfRangeException(nameof(options.PageSize)); }
            if (options.Choices.Count == 0) { throw new ArgumentException("There is nothing to choose from.", nameof(options)); }
            if (!options.Multiple && (options.Has(nameof(options.Minimum)) || options.Has(nameof(options.Maximum)) || options.Has(nameof(options.Collapsed)))) {
                throw new ArgumentException("Minimum, Maximum and Collapsed require Multiple.", nameof(options));
            }
            if (options.Minimum < 0 || options.Maximum < 0 || options.Maximum < options.Minimum) { throw new ArgumentOutOfRangeException(nameof(options.Minimum)); }
            if (options.Multiple) {
                _multiple = new MultiSelectPrompt<object?>(options.Title, options.Choices) {
                    Description = options.Description, ParameterName = options.ParameterName, Minimum = options.Minimum, Maximum = options.Maximum,
                    Collapsed = options.Collapsed, Preview = options.Preview
                };
                if (options.PageSize > 0) { _multiple.PageSize = options.PageSize; }
                if (options.Has(nameof(options.Default))) {
                    var selected = new List<object?>();
                    if (options.Default is IEnumerable values && !(options.Default is string)) { foreach (object? value in values) { selected.Add(value); } }
                    else if (options.Default != null) { selected.Add(options.Default); }
                    foreach (object? value in selected) { RequireChoice(options, value); }
                    _multiple.Default = selected;
                }
            } else {
                _single = new SelectPrompt<object?>(options.Title, options.Choices) {
                    Description = options.Description, ParameterName = options.ParameterName, Preview = options.Preview
                };
                if (options.PageSize > 0) { _single.PageSize = options.PageSize; }
                if (options.Has(nameof(options.Default))) { RequireChoice(options, options.Default); _single.Default = options.Default; }
            }
        }

        /// <summary>Whether the answer is a collection.</summary>
        public bool Multiple => _multiple != null;
        /// <summary>Whether a non-interactive answer was explicitly supplied.</summary>
        public bool HasDefault => _multiple?.HasDefault ?? _single!.HasDefault;
        /// <summary>Builds the initialized prompt without reading input or returning an answer.</summary>
        public IWidget Snapshot(InteractiveOptions? options = null) => _multiple != null ? _multiple.Snapshot(options) : _single!.Snapshot(options);
        /// <summary>Asks for one original value or a read-only collection of original values.</summary>
        public object? Show(InteractiveOptions? options = null, CancellationToken cancellationToken = default) => _multiple != null ? (object)_multiple.Show(options, cancellationToken) : _single!.Show(options, cancellationToken);
        private static void RequireChoice(SelectQuestionOptions options, object? value) {
            foreach (Choice choice in options.Choices) { if (choice.IsEnabled && Equals(choice.Value, value)) { return; } }
            throw new ArgumentException("A default must identify an enabled choice.", nameof(options.Default));
        }
    }

    public static partial class Question {
        /// <summary>Builds a scalar or multiple selection through the same validated definition as PowerShell.</summary>
        public static SelectionQuestion Select(SelectQuestionOptions options) => new SelectionQuestion(options ?? throw new ArgumentNullException(nameof(options)));
    }
}
