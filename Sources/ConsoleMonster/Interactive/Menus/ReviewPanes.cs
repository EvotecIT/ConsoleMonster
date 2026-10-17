using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A review item: the app's current choices, problems, the equivalent command, and Run, Copy command, Save preset and
    /// Back. The model is rebuilt on every frame, so it always matches what is selected elsewhere.
    /// </summary>
    internal sealed class ReviewPane : IPane {
        private readonly MenuItem _item;
        private readonly Func<MenuContext> _context;
        private readonly PromptLabels _labels;
        private readonly Action<Action<MenuContext>> _run;
        private readonly Func<string, bool> _copy;
        private readonly Action? _savePreset;
        private readonly Action<Toast> _toast;
        private readonly Action _back;
        private ReviewModel _model = new ReviewModel();
        private int _button;

        public ReviewPane(MenuItem item, Func<MenuContext> context, PromptLabels labels, Action<Action<MenuContext>> run, Func<string, bool> copy, Action? savePreset, Action<Toast> toast, Action back) {
            _item = item;
            _context = context;
            _labels = labels;
            _run = run;
            _copy = copy;
            _savePreset = savePreset;
            _toast = toast;
            _back = back;
        }

        public string Title => _model.Title ?? _item.Title;

        public string? Caption => _model.Problems.Count > 0 ? PromptLabels.Format(_labels.ToFix, _model.Problems.Count) : null;

        public bool IsAnimating => false;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        private void Refresh() {
            try {
                _model = _item.ReviewFactory?.Invoke(_context()) ?? new ReviewModel();
            } catch (Exception ex) {
                _model = new ReviewModel();
                _model.Problem(ex.Message);
            }
        }

        private List<string> Captions() {
            List<string> captions = new List<string>();
            if (_model.Run != null) {
                captions.Add(_model.RunText ?? _labels.Run);
            }
            if (!string.IsNullOrEmpty(_model.Command)) {
                captions.Add(_labels.CopyCommand);
            }
            if (_savePreset != null) {
                captions.Add(_labels.SavePreset);
            }
            captions.Add(_labels.Back);
            return captions;
        }

        public IWidget Build(ViewFrame frame, bool focused) {
            Refresh();
            return new ReviewBody(this, focused);
        }

        private sealed class ReviewBody : IWidget {
            private readonly ReviewPane _pane;
            private readonly bool _focused;

            public ReviewBody(ReviewPane pane, bool focused) {
                _pane = pane;
                _focused = focused;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 20 : constraints.MaxHeight);

            public void Render(Region region, RenderContext context) {
                int y = 0;
                if (!string.IsNullOrEmpty(_pane._model.Description)) {
                    UiParagraph description = new UiParagraph(_pane._model.Description!, ThemeToken.Muted, 2);
                    Size size = description.Measure(new Constraints(region.Width, 2), context);
                    description.Render(region.Sub(0, 0, region.Width, size.Height), context);
                    y = size.Height + 1;
                }
                List<string> captions = _pane.Captions();
                _pane._button = Math.Min(_pane._button, captions.Count - 1);
                ReviewDrawing.Draw(region.Sub(0, y, region.Width, Math.Max(1, region.Height - y)), context, _pane._model.Rows, _pane._model.Problems, _pane._model.Command, _pane._labels.EquivalentCommand, captions, _pane._button, _focused);
            }
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            List<string> captions = Captions();
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            bindings.Add(new KeyBinding(_labels.KeyMove, () => _button = (_button + captions.Count - 1) % captions.Count, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.UpArrow)) { KeyText = g.KeyLeftRight, Order = 10, Group = "Review" });
            bindings.Add(new KeyBinding(string.Empty, () => _button = (_button + 1) % captions.Count, new KeyChord(ConsoleKey.RightArrow), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false, Group = "Review" });
            bindings.Add(new KeyBinding(captions[Math.Min(_button, captions.Count - 1)].ToLowerInvariant(), () => Activate(captions[Math.Min(_button, captions.Count - 1)]), new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Review" });
            foreach (string caption in captions) {
                string c = caption;
                char key = char.ToLowerInvariant(c[0]);
                bindings.Add(new KeyBinding(c, () => Activate(c), new KeyChord(key)) { KeyText = key.ToString(), Order = 40, Group = "Review", InFooter = false });
            }
        }

        private void Activate(string caption) {
            if (caption == (_model.RunText ?? _labels.Run)) {
                if (_model.Problems.Count > 0) {
                    _toast(new Toast(_model.Problems[0], StatusKind.Warn, TimeSpan.FromSeconds(3)));
                    return;
                }
                _run(_model.Run!);
            } else if (caption == _labels.CopyCommand) {
                bool copied;
                try {
                    copied = _copy(_model.Command ?? string.Empty);
                } catch (Exception) {
                    copied = false;
                }
                _toast(copied ? new Toast(_labels.CommandCopied) : new Toast(_labels.CopyFailed, StatusKind.Warn));
            } else if (caption == _labels.SavePreset) {
                _savePreset?.Invoke();
            } else {
                _back();
            }
        }

        public bool HandleUnbound(InputEvent input) => false;

        public bool Poll(TimeSpan now) => false;

        public bool Back() => false;
    }

    /// <summary>The menu's saved presets: Enter applies one (every selection and form value), Delete removes it.</summary>
    internal sealed class PresetsPane : IPane {
        private readonly MenuItem _item;
        private readonly PromptLabels _labels;
        private readonly Func<IReadOnlyList<string>> _names;
        private readonly Action<string> _apply;
        private readonly Action<string> _delete;
        private ChoiceList _list;
        private ChoicePane _pane;
        private string _signature = string.Empty;

        public PresetsPane(MenuItem item, PromptLabels labels, Func<IReadOnlyList<string>> names, Action<string> apply, Action<string> delete) {
            _item = item;
            _labels = labels;
            _names = names;
            _apply = apply;
            _delete = delete;
            _list = new ChoiceList(new Choice[0], false) { Labels = labels };
            _pane = new ChoicePane(_list, new CaretSlot()) { Fill = true };
        }

        public string Title => _item.Title;

        public string? Caption => null;

        public bool IsAnimating => false;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        private void Refresh() {
            IReadOnlyList<string> names = _names();
            string signature = string.Join("\u0001", names);
            if (signature == _signature) {
                return;
            }
            _signature = signature;
            List<Choice> choices = new List<Choice>();
            foreach (string name in names) {
                choices.Add(new Choice(name, name));
            }
            _list = new ChoiceList(choices, false) { Labels = _labels };
            _pane = new ChoicePane(_list, new CaretSlot()) { Fill = true };
        }

        public IWidget Build(ViewFrame frame, bool focused) {
            Refresh();
            _list.Focused = focused;
            if (_list.Choices.Count == 0) {
                return new UiParagraph(_labels.NoPresets, ThemeToken.Muted, 4);
            }
            Rows rows = new Rows();
            if (!string.IsNullOrEmpty(_item.Description)) {
                rows.Add(new UiParagraph(_item.Description!, ThemeToken.Muted, 2));
                rows.Add(UiLine.Empty);
            }
            rows.Add(new CaretBridge(_pane, frame.Caret, focused), LayoutSize.Fraction());
            return rows;
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_list.Choices.Count == 0) {
                return;
            }
            _list.AddBindings(bindings, frame.Context);
            bindings.Add(new KeyBinding(_labels.KeySelect, () => {
                if (_list.FocusedChoice?.Value is string name) {
                    _apply(name);
                }
            }, new KeyChord(ConsoleKey.Enter)) { Order = 20 });
            bindings.Add(new KeyBinding("delete", () => {
                if (_list.FocusedChoice?.Value is string name) {
                    _delete(name);
                }
            }, new KeyChord(ConsoleKey.Delete)) { KeyText = "del", Order = 30 });
        }

        public bool HandleUnbound(InputEvent input) => _list.HandleText(input, out _);

        public bool Poll(TimeSpan now) => false;

        public bool Back() => _list.ClearFilter();
    }
}
