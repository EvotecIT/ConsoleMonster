using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A dialog in front of a view (a question, a name to type, the command palette). It owns the keys while open.</summary>
    internal abstract class Modal {
        public bool IsClosed { get; protected set; }

        /// <summary>Places the dialog near the top (palettes) instead of a third down.</summary>
        public virtual bool Top => false;

        public virtual int MaxWidth => 64;

        public abstract IWidget Build(ViewFrame frame, CaretSlot caret);

        public abstract void AddBindings(List<KeyBinding> bindings, ViewFrame frame);

        public virtual bool HandleUnbound(InputEvent input) => false;

        public virtual bool Poll(TimeSpan now) => false;

        public virtual bool IsAnimating => false;

        public void Close() => IsClosed = true;
    }

    /// <summary>The services a pane gets from the view that hosts it.</summary>
    internal interface IViewHost {
        PromptLabels Labels { get; }

        void ShowModal(Modal modal);

        void ShowToast(Toast toast);

        /// <summary>
        /// Queues a call for the view's own thread (it runs on the view's next tick); returns false when the view cannot
        /// take calls from other threads (or has closed).
        /// </summary>
        bool TryPost(Action call);
    }

    /// <summary>A dialog asking for one line of text (a preset name).</summary>
    internal sealed class TextDialog : Modal {
        private readonly string _title;
        private readonly string? _message;
        private readonly LineEditor _editor;
        private readonly Func<string, string?>? _validate;
        private readonly Action<string> _accept;
        private readonly PromptLabels _labels;
        private string? _error;

        public TextDialog(string title, string? message, string? initial, Func<string, string?>? validate, Action<string> accept, PromptLabels labels) {
            _title = title;
            _message = message;
            _editor = new LineEditor(initial);
            _validate = validate;
            _accept = accept;
            _labels = labels;
        }

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            Rows rows = new Rows();
            if (!string.IsNullOrEmpty(_message)) {
                rows.Add(new UiParagraph(_message!, ThemeToken.Muted));
            }
            rows.Add(new Columns(new Sized(new UiLine(context => new StyledLine(new[] { new TextSpan(InteractiveGlyphs.For(context).Pointer + " ", context.Style(ThemeToken.Accent)) })), LayoutSize.Fixed(2)), new TextInput(_editor, caret)) { Gap = 0 });
            rows.Add(_error == null ? UiLine.Empty : new UiLine(context => ChoicePane.Problem(_error, context)));
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return DialogChrome.Frame(rows, _title);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_labels.KeyConfirm, () => {
                string text = _editor.Text.Trim();
                _error = text.Length == 0 ? _labels.Required : _validate?.Invoke(text);
                if (_error == null) {
                    Close();
                    _accept(text);
                }
            }, new KeyChord(ConsoleKey.Enter)) { Order = 30 });
            bindings.Add(new KeyBinding(_labels.KeyCancel, Close, new KeyChord(ConsoleKey.Escape)) { Order = 40 });
        }

        public override bool HandleUnbound(InputEvent input) {
            _error = null;
            return _editor.Handle(input);
        }
    }

    /// <summary>A yes/no question in a dialog (discard changes, delete a preset).</summary>
    internal sealed class ConfirmDialog : Modal {
        private readonly string _title;
        private readonly string? _message;
        private readonly Action _yes;
        private readonly PromptLabels _labels;
        private readonly bool _destructive;
        private int _focus;

        public ConfirmDialog(string title, string? message, Action yes, PromptLabels labels, bool destructive = false) {
            _title = title;
            _message = message;
            _yes = yes;
            _labels = labels;
            _destructive = destructive;
            _focus = destructive ? 1 : 0;
        }

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            Rows rows = new Rows();
            if (!string.IsNullOrEmpty(_message)) {
                rows.Add(new UiParagraph(_message!, ThemeToken.Text, 6));
                rows.Add(UiLine.Empty);
            }
            rows.Add(new UiLine(context => new StyledLine(Buttons.Spans(new[] { _labels.Yes, _labels.No }, _focus, context))));
            return DialogChrome.Frame(rows, _title, _destructive ? ThemeToken.Warn : ThemeToken.Accent);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            char yes = char.ToLowerInvariant(_labels.Yes[0]);
            char no = char.ToLowerInvariant(_labels.No[0]);
            bindings.Add(new KeyBinding(_labels.KeyMove, () => _focus = 1 - _focus, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.RightArrow), new KeyChord(ConsoleKey.Tab)) { KeyText = InteractiveGlyphs.For(frame.Context).KeyLeftRight });
            bindings.Add(new KeyBinding(_labels.KeyConfirm, () => Answer(_focus == 0), new KeyChord(ConsoleKey.Enter)));
            bindings.Add(new KeyBinding(string.Empty, () => Answer(true), new KeyChord(yes)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Answer(false), new KeyChord(no)) { InFooter = false });
            bindings.Add(new KeyBinding(_labels.KeyCancel, Close, new KeyChord(ConsoleKey.Escape)));
        }

        private void Answer(bool yes) {
            Close();
            if (yes) {
                _yes();
            }
        }
    }
}
