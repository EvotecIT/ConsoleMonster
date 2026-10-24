using System;
using System.Collections;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A focusable layout control. Value edits and rendering are serialized; configuration is set before running.</summary>
    public abstract class InputControl : IWidget {
        private readonly object _gate = new object();
        private readonly bool _chrome;
        private bool _enabled = true;
        private bool _focused;
        private Rect _bounds;
        private Rect _contentBounds;
        private int _viewportHeight = Constraints.Unbounded;

        /// <summary>Creates a named control.</summary>
        /// <param name="key">The unique application-state key.</param>
        /// <param name="chrome">Whether to draw the public control's border and label.</param>
        protected InputControl(string key, bool chrome = true) {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException(AppMessages.Get("ControlName"), nameof(key)); }
            Key = key;
            _chrome = chrome;
        }

        /// <summary>The state key used by the application.</summary>
        public string Key { get; }

        /// <summary>The visible label, or null to use the name.</summary>
        public string? Label { get; set; }

        /// <summary>The current validation message below the control, or null.</summary>
        public string? ValidationMessage { get; set; }

        /// <summary>Focus order; equal values preserve declaration order.</summary>
        public int TabIndex { get; set; }

        /// <summary>Whether the control accepts input and participates in focus traversal.</summary>
        public bool Enabled {
            get { lock (_gate) { return _enabled; } }
            set { lock (_gate) { _enabled = value; } Invalidate(); }
        }

        /// <summary>Whether this control owns keyboard focus.</summary>
        public bool Focused {
            get { lock (_gate) { return _focused; } }
            internal set { lock (_gate) { _focused = value; } Invalidate(); }
        }

        /// <summary>The committed value, independent of the concrete control's value type.</summary>
        public object? CurrentValue => Read(ReadValueCore);

        /// <summary>Raised synchronously after a value changes, once per edit; caret-only movement does not change values.</summary>
        public event EventHandler<AppValueChangedEventArgs>? Changed;

        internal event Action<InputControl>? FocusRequested;
        internal event Action? Invalidated;
        internal event Action<AppValueChangedEventArgs>? ValueCommitted;
        internal Rect Bounds { get { lock (_gate) { return _bounds; } } }
        internal virtual Caret? CurrentCaret => null;
        internal int ViewportHeight { set { lock (_gate) { _viewportHeight = value; } } }

        /// <summary>Requests keyboard focus from the containing application.</summary>
        public void Focus() {
            if (!Enabled) { return; }
            if (FocusRequested != null) { FocusRequested(this); }
            else { Focused = true; }
        }

        /// <summary>Applies one normalized input event.</summary>
        /// <param name="input">The input event.</param>
        /// <param name="context">The active render profile and theme.</param>
        /// <returns>Whether the control handled the event.</returns>
        public bool HandleInput(InputEvent input, RenderContext context) {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (context == null) { throw new ArgumentNullException(nameof(context)); }
            object? before;
            object? after;
            bool handled;
            lock (_gate) {
                if (!_enabled) { return false; }
                before = ReadValueCore();
                handled = HandleInputCore(input, context);
                after = ReadValueCore();
            }
            Notify(before, after);
            if (handled) { Invalidate(); }
            return handled;
        }

        /// <summary>Reads control state under its render/edit lock.</summary>
        /// <typeparam name="T">The returned value type.</typeparam>
        /// <param name="read">The state reader.</param>
        /// <returns>The state snapshot.</returns>
        protected T Read<T>(Func<T> read) { lock (_gate) { return read(); } }

        /// <summary>Commits a programmatic value edit and raises a change notification after releasing the lock.</summary>
        /// <param name="edit">The state edit.</param>
        protected void Change(Action edit) {
            object? before;
            object? after;
            lock (_gate) { before = ReadValueCore(); edit(); after = ReadValueCore(); }
            Notify(before, after);
            Invalidate();
        }

        /// <summary>Reads the concrete value while the control is locked.</summary>
        /// <returns>The value.</returns>
        protected abstract object? ReadValueCore();

        /// <summary>Handles input while the control is locked.</summary>
        /// <param name="input">The normalized input.</param>
        /// <param name="context">The active profile and theme.</param>
        /// <returns>Whether the input was handled.</returns>
        protected abstract bool HandleInputCore(InputEvent input, RenderContext context);

        /// <summary>Measures the existing control content without its border.</summary>
        /// <param name="constraints">Available content size.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The content size.</returns>
        protected abstract Size MeasureContent(Constraints constraints, RenderContext context);

        /// <summary>Draws the existing control content under the render/edit lock.</summary>
        /// <param name="region">The content rectangle.</param>
        /// <param name="context">The render context.</param>
        protected abstract void RenderContent(Region region, RenderContext context);

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            lock (_gate) {
                constraints = constraints.WithHeight(Math.Min(constraints.MaxHeight, _viewportHeight));
                int extra = _chrome ? 2 + (string.IsNullOrEmpty(ValidationMessage) ? 0 : 1) : 0;
                Size content = MeasureContent(_chrome ? constraints.Shrink(4, extra) : constraints, context);
                return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, content.Height + extra));
            }
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            lock (_gate) {
                _bounds = region.Bounds;
                if (!_chrome) { RenderContent(region, context); return; }
                string title = Label ?? Key;
                if (!_enabled) { title = PromptLabels.Format(PromptLabels.For(context.Profile.Language).Disabled, title); }
                else if (_focused) { title = InteractiveGlyphs.For(context).Pointer + " " + title; }
                int errorRows = string.IsNullOrEmpty(ValidationMessage) ? 0 : 1;
                Panel panel = new Panel(new Content(this), Markup.Escape(title)) {
                    BorderRole = _focused && _enabled ? ThemeToken.Accent : ThemeToken.Border
                };
                panel.Render(region.Sub(0, 0, region.Width, Math.Max(0, region.Height - errorRows)), context);
                if (errorRows > 0 && region.Height > 0) {
                    region.Write(1, region.Height - 1, TextWrapper.Truncate(ChoicePane.Problem(ValidationMessage!, context), Math.Max(0, region.Width - 2), context.Ellipsis));
                }
                if (!_enabled) {
                    for (int y = 0; y < region.Height; y++) {
                        for (int x = 0; x < region.Width; x++) {
                            int px = region.Bounds.X + x;
                            int py = region.Bounds.Y + y;
                            Cell cell = region.Buffer[px, py];
                            region.Buffer.SetStyle(px, py, cell.Style.WithColor(context.Style(ThemeToken.Muted).Color).With(TextDecoration.Dim));
                        }
                    }
                } else if (_focused && CurrentCaret is Caret caret && caret.X >= _contentBounds.X && caret.X < _contentBounds.Right && caret.Y >= _contentBounds.Y && caret.Y < _contentBounds.Bottom) {
                    Cell cell = region.Buffer[caret.X, caret.Y];
                    if (context.HasColor) { region.Buffer.SetStyle(caret.X, caret.Y, cell.Style.With(TextDecoration.Reverse)); }
                    else { DrawPlainCaret(region.Buffer, caret, cell.Style); }
                }
            }
        }

        private void Notify(object? before, object? after) {
            if (!ValuesEqual(before, after)) {
                AppValueChangedEventArgs change = new AppValueChangedEventArgs(Key, before, after);
                ValueCommitted?.Invoke(change);
                Changed?.Invoke(this, change);
            }
        }

        internal static bool ValuesEqual(object? left, object? right) {
            if (left is IEnumerable a && right is IEnumerable b && !(left is string) && !(right is string)) {
                IEnumerator x = a.GetEnumerator();
                IEnumerator y = b.GetEnumerator();
                try {
                    while (x.MoveNext()) { if (!y.MoveNext() || !Equals(x.Current, y.Current)) { return false; } }
                    return !y.MoveNext();
                } finally { (x as IDisposable)?.Dispose(); (y as IDisposable)?.Dispose(); }
            }
            return Equals(left, right);
        }

        internal void Invalidate() => Invalidated?.Invoke();

        private void DrawPlainCaret(CellBuffer buffer, Caret caret, Style style) {
            // Insert the marker into a snapshot so a wide grapheme is never split by an in-place shift.
            int width = _contentBounds.Right - caret.X;
            if (width <= 0) { return; }
            Cell[] tail = new Cell[width];
            for (int x = 0; x < width; x++) { tail[x] = buffer[caret.X + x, caret.Y]; }
            for (int x = 0; x < width; x++) { buffer.Set(caret.X + x, caret.Y, " ", style); }
            buffer.Set(caret.X, caret.Y, "|", style);
            for (int x = 0; x < width; x++) {
                Cell cell = tail[x];
                if (!cell.IsContinuation && x + 1 + cell.Width <= width) { buffer.Set(caret.X + x + 1, caret.Y, cell.Glyph, cell.Style, cell.Width); }
            }
        }

        private sealed class Content : IWidget {
            private readonly InputControl _owner;
            internal Content(InputControl owner) { _owner = owner; }
            public Size Measure(Constraints constraints, RenderContext context) => _owner.MeasureContent(constraints, context);
            public void Render(Region region, RenderContext context) { _owner._contentBounds = region.Bounds; _owner.RenderContent(region, context); }
        }
    }
}
