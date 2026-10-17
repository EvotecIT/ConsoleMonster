using System;
using System.Collections;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>The list editor a choice field opens over its form page (filter, groups, preview, selection count).</summary>
    internal sealed class ChoiceEditor {
        private readonly FormView _form;
        private ChoiceList _list;
        private ChoicePane _pane;
        private readonly CaretSlot _caret = new CaretSlot();

        public ChoiceEditor(FormView form, ChoiceField field) {
            _form = form;
            Field = field;
            _list = CreateList();
            _pane = CreatePane();
        }

        public ChoiceField Field { get; }

        public CaretSlot Caret => _caret;

        public ChoiceList List => _list;

        private ChoiceList CreateList() {
            List<Choice> choices = new List<Choice>(Field.Choices);
            HashSet<object?> selected = new HashSet<object?>();
            if (Field.Multiple && Field.Value is IEnumerable values && !(Field.Value is string)) {
                foreach (object? v in values) {
                    selected.Add(v);
                }
            }
            ChoiceList list = new ChoiceList(choices, Field.Multiple) { Labels = _form.Labels, Minimum = Field.Minimum, Maximum = Field.Maximum };
            foreach (Choice c in choices) {
                if (Field.Multiple) {
                    list.SetSelected(c, selected.Contains(c.Value));
                } else if (Equals(c.Value, Field.Value)) {
                    list.FocusChoice(c);
                }
            }
            return list;
        }

        private ChoicePane CreatePane() => new ChoicePane(_list, _caret) { Fill = true, Preview = Field.Preview };

        public void Reload() {
            _list = CreateList();
            _pane = CreatePane();
        }

        public IWidget Build(ViewFrame frame, CaretSlot caret) {
            Rows rows = new Rows();
            rows.Add(new UiLine(context => Text.StyledLine.From(Field.Label, context.Style(ThemeToken.Title))));
            if (!string.IsNullOrEmpty(Field.Help)) {
                rows.Add(new UiParagraph(Field.Help!, ThemeToken.Muted, 2));
            }
            rows.Add(UiLine.Empty);
            rows.Add(new CaretRelay(_pane, _caret, caret), LayoutSize.Fraction());
            return rows;
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            _list.AddBindings(bindings, frame.Context);
            bindings.Add(new KeyBinding(Field.Multiple ? _form.Labels.KeyConfirm : _form.Labels.KeySelect, Accept, new KeyChord(ConsoleKey.Enter)) { Order = 25 });
            bindings.Add(new KeyBinding(_list.IsFiltering ? _form.Labels.KeyClearFilter : _form.Labels.KeyBack, () => {
                if (!_list.ClearFilter()) {
                    _form.CloseEditor(Field, null, false);
                }
            }, new KeyChord(ConsoleKey.Escape)) { Order = 70 });
        }

        public bool HandleUnbound(InputEvent input) {
            if (_list.HandleText(input, out Choice? picked)) {
                _pane.InvalidatePreview();
                if (picked != null) {
                    if (Field.Multiple) {
                        _list.SetSelected(picked, !_list.IsSelected(picked));
                    } else {
                        _form.CloseEditor(Field, picked.Value, true);
                    }
                }
                return true;
            }
            return false;
        }

        private void Accept() {
            if (Field.Multiple) {
                string? problem = _list.ValidateSelection();
                if (problem != null) {
                    _pane.Error = problem;
                    return;
                }
                List<object?> values = new List<object?>();
                foreach (Choice c in _list.SelectedChoices) {
                    values.Add(c.Value);
                }
                _form.CloseEditor(Field, values, true);
                return;
            }
            Choice? focused = _list.FocusedChoice;
            if (focused == null) {
                return;
            }
            if (!focused.IsEnabled) {
                _list.Message = PromptLabels.Format(_form.Labels.Disabled, focused.DisabledReason);
                return;
            }
            _form.CloseEditor(Field, focused.Value, true);
        }

        /// <summary>Renders a child and copies its caret into the frame's caret slot.</summary>
        private sealed class CaretRelay : IWidget {
            private readonly IWidget _child;
            private readonly CaretSlot _inner;
            private readonly CaretSlot _outer;

            public CaretRelay(IWidget child, CaretSlot inner, CaretSlot outer) {
                _child = child;
                _inner = inner;
                _outer = outer;
            }

            public Size Measure(Constraints constraints, Canvas.RenderContext context) => _child.Measure(constraints, context);

            public void Render(Canvas.Region region, Canvas.RenderContext context) {
                _inner.Clear();
                _child.Render(region, context);
                if (_inner.Caret.HasValue) {
                    _outer.Set(_inner.Caret.Value.X, _inner.Caret.Value.Y);
                }
            }
        }
    }
}
