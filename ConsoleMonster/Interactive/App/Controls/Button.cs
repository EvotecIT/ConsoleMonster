using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>A keyboard action button whose value counts activations.</summary>
    public sealed class Button : InputControl {
        private int _value;
        private bool _pressed;

        /// <summary>Creates a button using the same definition as an app's palette and grid actions.</summary>
        public Button(AppCommand command) : this(command?.Key ?? throw new ArgumentNullException(nameof(command)), command.Label) { Command = command; }

        /// <summary>Creates another button for an existing command, with its own input-state key.</summary>
        public Button(AppCommand command, string key) : this(key, command?.Label ?? throw new ArgumentNullException(nameof(command))) { Command = command; }

        /// <summary>The command invoked by the containing app, when this button was constructed from a command.</summary>
        public AppCommand? Command { get; }

        /// <summary>Creates an action button.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="label">The caption; null uses the name.</param>
        public Button(string key, string? label = null) : this(key, label, true) { }

        internal Button(string key, string? label, bool chrome) : base(key, chrome) {
            _compact = !chrome;
            Label = label;
            // InputControl raises Changed after releasing its state lock. Forwarding here
            // keeps synchronous application callbacks safe to read or edit other controls.
            Changed += (_, __) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        private readonly bool _compact;

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

        internal override bool Takes(KeyChord chord, RenderContext context) => chord.Key == ConsoleKey.Enter || chord.Key == ConsoleKey.Spacebar;
        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            bool inside = input.Column >= Bounds.X && input.Column < Bounds.Right && input.Row >= Bounds.Y && input.Row < Bounds.Bottom;
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) { _pressed = inside; return inside; }
            if (input.MouseAction == MouseAction.Up && input.MouseButton == 1) {
                bool activate = _pressed && inside;
                _pressed = false;
                if (activate) { _value++; }
                return activate;
            }
            return false;
        }

        internal override void CancelPointer() { _pressed = false; }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) =>
            new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (_compact) {
                Style style = context.Style(Focused && Enabled ? ThemeToken.Focus : Enabled ? ThemeToken.Text : ThemeToken.Muted);
                string pointer = Focused && Enabled ? InteractiveGlyphs.For(context).Pointer + " " : "  ";
                region.Write(0, 0, pointer + "[ " + (Label ?? Key) + " ]", Focused ? style.With(TextDecoration.Bold) : style);
            } else {
                region.Write(0, 0, new StyledLine(Buttons.Spans(new[] { Label ?? Key }, 0, context,
                    Focused && Enabled, blocked: Enabled ? -1 : 0)));
            }
        }
    }
}
