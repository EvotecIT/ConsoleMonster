using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Shared choice-list editing and rendering for layout selection inputs.</summary>
    public abstract class SelectionInputBase : InputControl {
        private readonly ChoiceList _list;
        private readonly ChoicePane _pane;
        private readonly bool _multiple;
        private object? _value;
        private bool _pointerDown;
        private bool _dragged;
        private Choice? _pointerOrigin;
        private bool _open;

        /// <summary>Creates a selection input backed by the canonical choice editor.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="choices">The choices, configured before the application runs.</param>
        /// <param name="multiple">Whether more than one choice can be selected.</param>
        protected SelectionInputBase(string key, IEnumerable<Choice> choices, bool multiple) : this(key, choices, multiple, true) { }

        // Without chrome for a host that already frames and titles the list (the command palette).
        private protected SelectionInputBase(string key, IEnumerable<Choice> choices, bool multiple, bool chrome) : base(key, chrome) {
            _multiple = multiple;
            _list = new ChoiceList(choices, multiple);
            _pane = new ChoicePane(_list, new CaretSlot());
        }

        /// <summary>The preferred number of choice rows, at least one. Set before running.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
        public int PageSize {
            get => _pane.PageSize;
            set {
                if (value < 1) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _pane.PageSize = value;
            }
        }

        /// <summary>Shows single-choice items as radio buttons using the existing selection value. Multiple-selection inputs retain checkboxes.</summary>
        public bool Radio { get; set; }

        /// <summary>
        /// Shows one row with the current choice. Enter, Space, Down or a click opens the list over the content below it;
        /// choosing (or Enter for several choices) closes it, and so do Escape, Tab and a click elsewhere.
        /// </summary>
        public bool Dropdown { get; set; }

        /// <summary>Reads the single committed value under the control lock.</summary>
        /// <returns>The selected value, or null before a selection or after clearing it.</returns>
        protected object? ReadSingleValue() => Read(() => _value);

        /// <summary>Assigns an enabled choice's value, or null to clear an absent null choice.</summary>
        /// <param name="value">The value to commit.</param>
        /// <exception cref="ArgumentException">The value does not identify an enabled choice.</exception>
        protected void SetSingleValue(object? value) {
            Change(() => {
                Choice? choice = FindEnabled(value);
                if (choice == null && value != null) { throw new ArgumentException(AppMessages.Get("InvalidChoice"), nameof(value)); }
                _value = choice?.Value;
                _list.ClearFilter();
                _list.FocusChoice(choice);
                _pane.InvalidatePreview();
            });
        }

        /// <summary>Reads a detached, read-only selection snapshot under the control lock.</summary>
        /// <returns>Selected values in declaration order.</returns>
        protected IReadOnlyList<object?> ReadMultipleValues() => Read(Snapshot);

        /// <summary>Replaces the selection with enabled choices matching the supplied values.</summary>
        /// <param name="values">The selected values. Duplicates are collapsed in declaration order.</param>
        /// <exception cref="ArgumentNullException">The values are null.</exception>
        /// <exception cref="ArgumentException">A value does not identify an enabled choice.</exception>
        protected void SetMultipleValues(IReadOnlyList<object?> values) {
            if (values == null) { throw new ArgumentNullException(nameof(values)); }
            Change(() => {
                HashSet<Choice> selected = new HashSet<Choice>();
                foreach (object? value in values) {
                    Choice? choice = FindEnabled(value);
                    if (choice == null) { throw new ArgumentException(AppMessages.Get("InvalidChoices"), nameof(values)); }
                    selected.Add(choice);
                }
                foreach (Choice choice in _list.Choices) { _list.SetSelected(choice, selected.Contains(choice)); }
                _pane.InvalidatePreview();
            });
        }

        /// <inheritdoc />
        protected override object? ReadValueCore() => _multiple ? (object)Snapshot() : _value;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            Configure(context);
            if (Dropdown && !_open) {
                if (!input.Is(ConsoleKey.Enter) && !input.Is(ConsoleKey.Spacebar) && !input.Is(ConsoleKey.DownArrow) && !input.Is(ConsoleKey.DownArrow, KeyModifiers.Alt)) { return false; }
                _open = true;
                _pane.InvalidatePreview();
                return true;
            }
            bool handled;
            if (input.Is(ConsoleKey.Escape)) {
                handled = _list.ClearFilter();
                if (!handled && _open) { _open = false; handled = true; }
            } else if (_open && _multiple && input.Is(ConsoleKey.Enter)) {
                _open = false;
                handled = true;
            } else if (input.Is(ConsoleKey.Enter) || (!_multiple && input.Is(ConsoleKey.Spacebar))) {
                if (_multiple) { _list.ToggleFocused(); }
                else { Close(Accept(_list.FocusedChoice)); }
                handled = true;
            } else {
                List<KeyBinding> bindings = new List<KeyBinding>();
                _list.AddBindings(bindings, context);
                handled = KeyFooter.Dispatch(bindings, input);
                if (!handled && _list.HandleText(input, out Choice? picked)) {
                    if (picked != null) {
                        if (_multiple) { _list.SetSelected(picked, !_list.IsSelected(picked)); }
                        else { Close(Accept(picked)); }
                    }
                    handled = true;
                }
            }
            if (handled) { _pane.InvalidatePreview(); }
            return handled;
        }

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            Configure(context);
            if (Dropdown) {
                if (input.MouseAction != MouseAction.Down || input.MouseButton != 1) { return false; }
                _open = !_open;
                return true;
            }
            return Point(input);
        }

        internal override bool HandlePopupPointerCore(InputEvent input, RenderContext context) {
            Configure(context);
            return _open && Point(input);
        }

        internal override void ClosePopupCore() { _open = false; CancelPointer(); }

        internal override bool Takes(KeyChord chord, RenderContext context) => Read(() => {
            Configure(context);
            bool plain = chord.Modifiers == KeyModifiers.None;
            if (Dropdown && !_open) {
                return plain && (chord.Key == ConsoleKey.Enter || chord.Key == ConsoleKey.Spacebar || chord.Key == ConsoleKey.DownArrow)
                    || chord.Equals(new KeyChord(ConsoleKey.DownArrow, KeyModifiers.Alt));
            }
            if (plain && chord.Key == ConsoleKey.Escape) { return _open || _list.IsFiltering; }
            // Typing filters the list, so characters and the filter's editing keys belong to it.
            if (LineEditor.Takes(chord) || plain && (chord.Key == ConsoleKey.Enter || chord.Key == ConsoleKey.Spacebar)) { return true; }
            List<KeyBinding> bindings = new List<KeyBinding>();
            _list.AddBindings(bindings, context);
            return bindings.Exists(binding => binding.Binds(chord));
        });
        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            if (!Dropdown) { return; }
            PromptLabels labels = PromptLabels.For(context.Profile.Language);
            if (!Read(() => _open)) {
                keys.Add(new KeyBinding(labels.KeyOpen, () => { }, new KeyChord(ConsoleKey.Enter), new KeyChord(ConsoleKey.Spacebar), new KeyChord(ConsoleKey.DownArrow)) { Order = 1 });
                return;
            }
            keys.Add(new KeyBinding(labels.KeyMove, () => { }, new KeyChord(ConsoleKey.UpArrow), new KeyChord(ConsoleKey.DownArrow)) { KeyText = InteractiveGlyphs.For(context).KeyUpDown, Order = 1 });
            keys.Add(new KeyBinding(_multiple ? labels.KeyToggle : labels.KeySelect, () => { }, _multiple ? new KeyChord(ConsoleKey.Spacebar) : new KeyChord(ConsoleKey.Enter)) { Order = 2 });
            keys.Add(new KeyBinding(_multiple ? labels.KeyDone : labels.KeyClose, () => { }, _multiple ? new KeyChord(ConsoleKey.Enter) : new KeyChord(ConsoleKey.Escape)) { Order = 3 });
            if (_multiple) { keys.Add(new KeyBinding(labels.KeyClose, () => { }, new KeyChord(ConsoleKey.Escape)) { InFooter = false }); }
        }

        private bool Point(InputEvent input) {
            bool handled = _list.HandlePointer(input);
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                _pointerDown = handled; _dragged = false;
                _pointerOrigin = handled ? _list.FocusedChoice : null;
            }
            if (_pointerDown && input.MouseAction == MouseAction.Move && (input.HeldButtons & 1) != 0 && handled) {
                if (_multiple && !_dragged && _pointerOrigin != null) { _list.SetSelected(_pointerOrigin, true); }
                _dragged = true;
                if (_multiple && _list.FocusedChoice is Choice choice) { _list.SetSelected(choice, true); }
            }
            if (input.MouseAction == MouseAction.Up && input.MouseButton == 1) {
                if (_pointerDown && handled) {
                    if (_multiple) { if (!_dragged) { _list.ToggleFocused(); } }
                    else { Close(Accept(_list.FocusedChoice)); }
                }
                CancelPointer();
            }
            return handled;
        }

        internal override void CancelPointer() { _pointerDown = false; _dragged = false; _pointerOrigin = null; }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) {
            Configure(context);
            return Dropdown ? new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight)) : _pane.Measure(constraints, context);
        }

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            Configure(context);
            if (!Dropdown) {
                _pane.Caret.Clear();
                _pane.Render(region, context);
                return;
            }
            DrawSummary(region, context);
            if (_open) { InputArrangement.RecordPopup(this, new Popup(this)); }
        }

        // The closed dropdown: the chosen value (or a dash) and the dropdown mark, as form choice fields draw it.
        private void DrawSummary(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) { return; }
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            List<string> labels = new List<string>();
            foreach (Choice choice in _list.Choices) {
                if (_multiple ? _list.IsSelected(choice) : _value != null && labels.Count == 0 && Equals(choice.Value, _value)) { labels.Add(choice.Label); }
            }
            bool empty = labels.Count == 0;
            string shown = empty ? (_multiple ? _list.Labels.KeyNone : "—") : string.Join(", ", labels.ToArray());
            string mark = " " + (_open ? glyphs.Up : glyphs.Dropdown);
            int width = Math.Max(1, region.Width - Text.DisplayWidth.Measure(mark));
            int x = region.Write(0, 0, Text.TextWrapper.Truncate(shown, width, context.Ellipsis), context.Style(empty ? Styling.ThemeToken.Muted : Styling.ThemeToken.Text));
            region.Write(Math.Max(x, region.Width - Text.DisplayWidth.Measure(mark)), 0, mark, context.Style(Focused ? Styling.ThemeToken.Accent : Styling.ThemeToken.Subtle));
        }

        private void Close(bool accepted) { if (accepted) { _open = false; } }

        private void Configure(RenderContext context) {
            if (!Focused || !Enabled) { _open = false; }
            _list.Labels = PromptLabels.For(context.Profile.Language);
            _list.Focused = Focused && Enabled;
            _list.Radio = Radio && !_multiple;
            _list.RadioValue = _value;
        }

        private bool Accept(Choice? choice) {
            if (choice == null) { return false; }
            if (!choice.IsEnabled) {
                _list.Message = PromptLabels.Format(_list.Labels.Disabled, choice.DisabledReason);
                return false;
            }
            _value = choice.Value;
            return true;
        }

        // The open list, drawn by the app overlay layer in a bordered box under the control.
        private sealed class Popup : IWidget {
            private readonly SelectionInputBase _owner;
            internal Popup(SelectionInputBase owner) { _owner = owner; }
            public Size Measure(Constraints constraints, RenderContext context) => _owner.Read(() => {
                _owner.Configure(context);
                Size pane = _owner._pane.Measure(constraints.Shrink(4, 2), context);
                return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, pane.Height + 2));
            });
            public void Render(Region region, RenderContext context) => _owner.Read(() => {
                _owner.Configure(context);
                _owner._pane.Caret.Clear();
                new Panel(_owner._pane) { BorderRole = Styling.ThemeToken.Accent }.Render(region, context);
                return true;
            });
        }

        private Choice? FindEnabled(object? value) {
            foreach (Choice choice in _list.Choices) {
                if (choice.IsEnabled && Equals(choice.Value, value)) { return choice; }
            }
            return null;
        }

        private IReadOnlyList<object?> Snapshot() {
            List<object?> values = new List<object?>();
            foreach (Choice choice in _list.SelectedChoices) { values.Add(choice.Value); }
            return values.AsReadOnly();
        }
    }
}
