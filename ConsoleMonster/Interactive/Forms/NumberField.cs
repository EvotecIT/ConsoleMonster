using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>A number with an optional range and step.</summary>
    public sealed class NumberField : Field {
        /// <summary>Creates a number field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        public NumberField(string key, string? label = null) : base(key, label) {
        }

        /// <inheritdoc />
        public override FieldKind Kind => FieldKind.Number;

        /// <summary>Whether only whole numbers are accepted.</summary>
        public bool Integer { get; set; }

        /// <summary>The smallest value.</summary>
        public decimal? Minimum { get; set; }

        /// <summary>The largest value.</summary>
        public decimal? Maximum { get; set; }

        /// <summary>The step of Up and Down.</summary>
        public decimal Step { get; set; } = 1;

        /// <summary>Lossless editable text; compact display formatting must never change a submitted value.</summary>
        internal string FormatInput(decimal value) => DecimalInput.Format(value, Integer, CultureInfo.CurrentCulture);

        internal string Format(decimal value) => value.ToString(Integer ? "0" : "0.############", CultureInfo.CurrentCulture);

        /// <inheritdoc />
        public override string Display(object? value, PromptLabels labels) => Normalize(value) is decimal d ? Format(d) : base.Display(value, labels);

        internal string? Parse(string text, PromptLabels labels, out decimal? value) {
            value = null;
            string trimmed = text.Trim();
            if (trimmed.Length == 0) {
                return null;
            }
            if (!DecimalInput.TryParse(trimmed, Integer, CultureInfo.CurrentCulture, out decimal parsed)) {
                return Integer ? labels.NotAWholeNumber : labels.NotANumber;
            }
            value = parsed;
            return null;
        }

        internal override string? Check(object? value, FormValues values, PromptLabels labels) {
            if (Normalize(value) is decimal d) {
                if (Minimum.HasValue && d < Minimum.Value) {
                    return PromptLabels.Format(labels.Minimum, Format(Minimum.Value));
                }
                if (Maximum.HasValue && d > Maximum.Value) {
                    return PromptLabels.Format(labels.Maximum, Format(Maximum.Value));
                }
            }
            return base.Check(value, values, labels);
        }

        internal override bool TryRestore(object? stored, out object? value) {
            value = null;
            if (stored is string s && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d)) {
                value = d;
                return true;
            }
            return false;
        }
    }
}
