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

        /// <summary>Creates a selection input backed by the canonical choice editor.</summary>
        /// <param name="name">The application-state key.</param>
        /// <param name="choices">The choices, configured before the application runs.</param>
        /// <param name="multiple">Whether more than one choice can be selected.</param>
        protected SelectionInputBase(string name, IEnumerable<Choice> choices, bool multiple) : base(name) {
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
            bool handled;
            if (input.Is(ConsoleKey.Escape)) {
                handled = _list.ClearFilter();
            } else if (input.Is(ConsoleKey.Enter) || (!_multiple && input.Is(ConsoleKey.Spacebar))) {
                if (_multiple) { _list.ToggleFocused(); }
                else { Accept(_list.FocusedChoice); }
                handled = true;
            } else {
                List<KeyBinding> bindings = new List<KeyBinding>();
                _list.AddBindings(bindings, context);
                handled = KeyFooter.Dispatch(bindings, input);
                if (!handled && _list.HandleText(input, out Choice? picked)) {
                    if (picked != null) {
                        if (_multiple) { _list.SetSelected(picked, !_list.IsSelected(picked)); }
                        else { Accept(picked); }
                    }
                    handled = true;
                }
            }
            if (handled) { _pane.InvalidatePreview(); }
            return handled;
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) {
            Configure(context);
            return _pane.Measure(constraints, context);
        }

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            Configure(context);
            _pane.Caret.Clear();
            _pane.Render(region, context);
        }

        private void Configure(RenderContext context) {
            _list.Labels = PromptLabels.For(context.Profile.Language);
            _list.Focused = Focused && Enabled;
        }

        private void Accept(Choice? choice) {
            if (choice == null) { return; }
            if (!choice.IsEnabled) {
                _list.Message = PromptLabels.Format(_list.Labels.Disabled, choice.DisabledReason);
                return;
            }
            _value = choice.Value;
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
