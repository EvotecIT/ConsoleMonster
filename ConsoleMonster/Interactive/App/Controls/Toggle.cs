using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>A layout checkbox using the form toggle's labels and terminal glyphs.</summary>
    public sealed class Toggle : InputControl {
        private readonly ToggleField _field;
        private bool _value;

        /// <summary>Creates an on/off input.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="value">The initial value.</param>
        public Toggle(string key, bool value = false) : base(key) {
            _field = new ToggleField(key);
            _value = value;
        }

        /// <summary>The committed on/off value. Assignments notify only when the value changes.</summary>
        public bool Value {
            get => Read(() => _value);
            set => Change(() => _value = value);
        }

        /// <summary>Text after the checked box; null uses the current language's Yes label.</summary>
        public string? OnText { get => _field.OnText; set => _field.OnText = value; }

        /// <summary>Text after the unchecked box; null uses the current language's No label.</summary>
        public string? OffText { get => _field.OffText; set => _field.OffText = value; }

        /// <inheritdoc />
        protected override object ReadValueCore() => _value;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (input.Is(ConsoleKey.Spacebar) || input.Is(ConsoleKey.Enter)) {
                _value = !_value;
                return true;
            }
            if (input.Is(ConsoleKey.LeftArrow) || input.Is(ConsoleKey.RightArrow)) {
                _value = input.Is(ConsoleKey.RightArrow);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) =>
            new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            int x = region.Write(0, 0, _value ? glyphs.Checked : glyphs.Unchecked,
                context.Style(_value ? ThemeToken.Accent : ThemeToken.Muted));
            region.Write(x + 1, 0, _field.Display(_value, PromptLabels.For(context.Profile.Language)),
                context.Style(_value ? ThemeToken.Text : ThemeToken.Muted));
        }
    }
}
