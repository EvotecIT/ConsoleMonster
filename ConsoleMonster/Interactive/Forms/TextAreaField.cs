using System;
using System.Globalization;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Several lines of text (notes, change reasons). In a form it shows the first line and the number of further lines;
    /// Enter opens the text in a dialog with soft wrap, where Ctrl+Enter saves and Esc keeps the old value.
    /// </summary>
    public sealed class TextAreaField : Field {
        /// <summary>Creates the field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        public TextAreaField(string name, string? label = null) : base(name, label) {
        }

        /// <inheritdoc />
        public override FieldKind Kind => FieldKind.TextArea;

        /// <summary>Muted text while empty.</summary>
        public string? Placeholder { get; set; }

        /// <summary>The fewest characters.</summary>
        public int MinLength { get; set; }

        /// <summary>The most characters.</summary>
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary>The most lines.</summary>
        public int MaxLines { get; set; } = int.MaxValue;

        /// <summary>The visible rows of the editor (default 8).</summary>
        public int Height { get; set; } = 8;

        /// <inheritdoc />
        public override string Display(object? value, PromptLabels labels) {
            string text = value as string ?? string.Empty;
            if (text.Length == 0) {
                return string.Empty;
            }
            string[] lines = text.Split('\n');
            return lines.Length == 1 ? lines[0] : lines[0] + " (+" + (lines.Length - 1).ToString(CultureInfo.InvariantCulture) + ")";
        }

        internal override string? Check(object? value, FormValues values, PromptLabels labels) {
            string text = value as string ?? string.Empty;
            if (text.Length > 0 && text.Length < MinLength) {
                return PromptLabels.Format(labels.MinimumLength, MinLength);
            }
            if (text.Length > MaxLength) {
                return PromptLabels.Format(labels.MaximumLength, MaxLength);
            }
            if (text.Length > 0 && text.Split('\n').Length > MaxLines) {
                return PromptLabels.Format(labels.MaximumLines, MaxLines);
            }
            return base.Check(value, values, labels);
        }
    }
}
