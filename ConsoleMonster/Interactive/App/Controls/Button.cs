using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A keyboard action button whose value counts activations.</summary>
    public sealed class Button : InputControl {
        private int _value;

        /// <summary>Creates an action button.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="label">The caption; null uses the name.</param>
        public Button(string key, string? label = null) : base(key) {
            Label = label;
            // InputControl raises Changed after releasing its state lock. Forwarding here
            // keeps synchronous application callbacks safe to read or edit other controls.
            Changed += (_, __) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>The number of handled Enter or Space activations, initially zero.</summary>
        public int Value => Read(() => _value);

        /// <summary>Raised synchronously once per activation, after releasing the control's state lock.</summary>
        public event EventHandler? Clicked;

        /// <inheritdoc />
        protected override object ReadValueCore() => _value;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (!input.Is(ConsoleKey.Enter) && !input.Is(ConsoleKey.Spacebar)) { return false; }
            _value++;
            return true;
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) =>
            new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) =>
            region.Write(0, 0, new StyledLine(Buttons.Spans(new[] { Label ?? Key }, 0, context,
                Focused && Enabled, blocked: Enabled ? -1 : 0)));
    }
}
