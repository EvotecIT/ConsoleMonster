using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>The command palette dialog: fuzzy search over every item, action and preset, plus the app's providers.</summary>
    internal sealed class PaletteModal : Modal {
        private readonly ChoiceList _list;
        private readonly ChoicePane _pane;
        private readonly CaretSlot _caret = new CaretSlot();
        private readonly Action<PaletteCommand> _run;
        private readonly PromptLabels _labels;

        public PaletteModal(IEnumerable<PaletteCommand> commands, Action<PaletteCommand> run, PromptLabels labels) {
            _run = run;
            _labels = labels;
            List<Choice> choices = new List<Choice>();
            foreach (PaletteCommand command in commands) {
                choices.Add(new Choice(command, command.Title) {
                    Description = command.Description,
                    Badge = command.Category == null ? null : RichText.Plain(command.Category + (command.Keys == null ? string.Empty : "  " + command.Keys))
                });
            }
            _list = new ChoiceList(choices, false) { Labels = labels };
            _pane = new ChoicePane(_list, _caret) { PageSize = 12 };
        }

        public override bool Top => true;

        public override int MaxWidth => 76;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            _pane.PageSize = Math.Max(4, Math.Min(12, frame.Height - 8));
            Rows rows = new Rows();
            rows.Add(new Relay(_pane, _caret, caret));
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return new Panel(rows, Markup.Escape(_labels.Commands)) { BorderToken = ThemeToken.Accent, Caption = Markup.Escape(_labels.SearchCommands) };
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            _list.AddBindings(bindings, frame.Context);
            bindings.Add(new KeyBinding(_labels.KeySelect, () => {
                if (_list.FocusedChoice?.Value is PaletteCommand command) {
                    Close();
                    _run(command);
                }
            }, new KeyChord(ConsoleKey.Enter)) { Order = 25 });
            bindings.Add(new KeyBinding(_labels.KeyCancel, () => {
                if (!_list.ClearFilter()) {
                    Close();
                }
            }, new KeyChord(ConsoleKey.Escape)) { Order = 70 });
        }

        public override bool HandleUnbound(InputEvent input) => _list.HandleText(input, out _);

        private sealed class Relay : IWidget {
            private readonly IWidget _child;
            private readonly CaretSlot _inner;
            private readonly CaretSlot _outer;

            public Relay(IWidget child, CaretSlot inner, CaretSlot outer) {
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
