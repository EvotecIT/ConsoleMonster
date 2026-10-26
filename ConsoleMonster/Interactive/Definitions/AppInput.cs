using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs persistent inputs from the same kind selectors and options as PowerShell.</summary>
    public static class AppInput {
        /// <summary>Creates a scalar Select or collection-valued MultiSelect without changing choice metadata.</summary>
        public static InputControl Select(SelectInputOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.PageSize < 1) { throw new ArgumentOutOfRangeException(nameof(options.PageSize)); }
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
            if (options.PageSize.HasValue) { ((SelectionInputBase)control).PageSize = options.PageSize.Value; }
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

        /// <summary>Constructs a button and attaches typed callbacks after initialization.</summary>
        /// <param name="options">Key, label, state and callback configuration.</param>
        /// <returns>The persistent button.</returns>
        public static Button Button(ButtonInputOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.Command != null && options.OnClick != null) { throw new ArgumentException("Use Command or OnClick, not both.", nameof(options)); }
            var button = options.Command == null ? new Button(options.Key, options.Label) : new Button(options.Command, string.IsNullOrEmpty(options.Key) ? options.Command.Key : options.Key);
            Configure(button, options.Label ?? options.Command?.Label, options.Disabled, options.TabIndex, options.ValidationMessage, options.OnChange);
            if (options.OnClick != null) { button.Clicked += options.OnClick; }
            return button;
        }

        /// <summary>Constructs a Boolean control using the same definition as New-ConsoleToggle.</summary>
        /// <param name="options">Key, label, initial state and callback configuration.</param>
        /// <returns>The persistent toggle.</returns>
        public static Toggle Toggle(ToggleInputOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            var toggle = new Toggle(options.Key, options.Value) { OnText = options.OnText, OffText = options.OffText };
            Configure(toggle, options.Label, options.Disabled, options.TabIndex, options.ValidationMessage, options.OnChange);
            return toggle;
        }

        private static void Configure(InputControl control, string? label, bool disabled, int tabIndex, string? validationMessage, EventHandler<AppValueChangedEventArgs>? onChange) {
            control.Label = label; control.Disabled = disabled; control.TabIndex = tabIndex; control.ValidationMessage = validationMessage;
            if (onChange != null) { control.Changed += onChange; }
        }
    }
}
