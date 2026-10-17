using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>A line of text.</summary>
    public sealed class TextField : Field {
        /// <summary>Creates a text field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        public TextField(string name, string? label = null) : base(name, label) {
        }

        /// <inheritdoc />
        public override FieldKind Kind => FieldKind.Text;

        /// <summary>Muted text while empty.</summary>
        public string? Placeholder { get; set; }

        /// <summary>The fewest characters.</summary>
        public int MinLength { get; set; }

        /// <summary>The most characters.</summary>
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary>A pattern the whole value must match.</summary>
        public string? Pattern { get; set; }

        /// <summary>The message when the pattern does not match.</summary>
        public string? PatternMessage { get; set; }

        internal override string? Check(object? value, FormValues values, PromptLabels labels) {
            string text = value as string ?? string.Empty;
            if (text.Length > 0 && text.Length < MinLength) {
                return PromptLabels.Format(labels.MinimumLength, MinLength);
            }
            if (text.Length > 0 && !string.IsNullOrEmpty(Pattern) && !System.Text.RegularExpressions.Regex.IsMatch(text, "^(?:" + Pattern + ")$", System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))) {
                return PatternMessage ?? labels.InvalidFormat;
            }
            return base.Check(value, values, labels);
        }
    }
}
