using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs persistent inputs from the same kind selectors and options as PowerShell.</summary>
    public static class AppInput {
        /// <summary>Creates a scalar Select or collection-valued MultiSelect without changing choice metadata.</summary>
        public static InputControl Select(SelectInputOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            InputControl control;
            if (options.Multiple) {
                var select = new MultiSelect(options.Key, options.Choices);
                if (options.Has(nameof(options.Value))) {
                    var values = new List<object?>();
                    if (options.Value is IEnumerable sequence && !(options.Value is string)) { foreach (object? value in sequence) { values.Add(value); } }
                    else if (options.Value != null) { values.Add(options.Value); }
                    select.Value = values;
                }
                control = select;
            } else {
                var select = new Select(options.Key, options.Choices);
                if (options.Has(nameof(options.Value))) { select.Value = options.Value; }
                control = select;
            }
            Configure(control, options.Label, options.Disabled, options.TabIndex, options.ValidationMessage, options.OnChange);
            return control;
        }

        /// <summary>Creates a TextInput or TextArea, rejecting options that do not apply to its kind.</summary>
        public static InputControl Text(TextInputOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.MaxLength < 0 || options.Height < 1 || options.ScrollRow < 0) { throw new ArgumentOutOfRangeException(nameof(options)); }
            foreach (string field in options.Multiline ? new[] { nameof(options.Mask), nameof(options.Role) } : new[] { nameof(options.Height), nameof(options.ScrollRow) }) {
                if (options.Has(field)) { throw new ArgumentException(field + (options.Multiline ? " is not supported with Multiline." : " requires Multiline."), nameof(options)); }
            }
            InputControl control = options.Multiline
                ? (InputControl)new TextArea(options.Key, options.Value) { Placeholder = options.Placeholder, MaxLength = options.MaxLength, Height = options.Height, ScrollRow = options.ScrollRow }
                : new TextInput(options.Key, options.Value) { Placeholder = options.Placeholder, MaxLength = options.MaxLength, Mask = options.Mask, Role = options.Role };
            Configure(control, options.Label, options.Disabled, options.TabIndex, options.ValidationMessage, options.OnChange);
            return control;
        }

        private static void Configure(InputControl control, string? label, bool disabled, int tabIndex, string? validationMessage, EventHandler<AppValueChangedEventArgs>? onChange) {
            control.Label = label; control.Disabled = disabled; control.TabIndex = tabIndex; control.ValidationMessage = validationMessage;
            if (onChange != null) { control.Changed += onChange; }
        }
    }
}
