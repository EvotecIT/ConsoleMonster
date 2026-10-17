using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The shared view of typed answers (text, secret, number, path): the question with a hint, an input row, an inline
    /// error, and the footer. Validation runs on Enter, then again on every edit until the answer is valid.
    /// </summary>
    internal abstract class EntryView<T> : PromptView<T> {
        private bool _validateLive;

        protected EntryView(Prompt<T> prompt, PromptLabels labels, string? initial) : base(prompt, labels) {
            Editor = new LineEditor(initial);
        }

        public LineEditor Editor { get; }

        /// <summary>The text the Tab key fills in (the default), or null.</summary>
        protected virtual string? Fill => null;

        protected virtual string? Hint => null;

        protected virtual string? Placeholder => null;

        protected virtual string? Mask(InteractiveGlyphs glyphs) => null;

        /// <summary>Converts and validates the text; returns the error or null.</summary>
        protected abstract string? TryParse(string text, out T value);

        /// <summary>Extra rows under the input (suggestions).</summary>
        protected virtual IWidget? Extra(ViewFrame frame) => null;

        protected virtual void AddEntryBindings(List<KeyBinding> bindings, ViewFrame frame) {
        }

        protected override IWidget Build(ViewFrame frame) {
            Rows rows = new Rows();
            rows.Add(Header(Hint));
            if (!string.IsNullOrEmpty(Prompt.Description)) {
                rows.Add(new Padding(new UiParagraph(Prompt.Description!), 2, 0, 0, 0));
            }
            TextInputWidget input = new TextInputWidget(Editor, Caret) {
                Placeholder = Placeholder,
                Mask = Mask(InteractiveGlyphs.For(frame.Context))
            };
            rows.Add(new Columns(new Sized(new UiLine(context => new StyledLine(new[] { PromptMark(context) })) { Indent = 2 }, LayoutSize.Fixed(4)), input) { Gap = 0 });
            IWidget? extra = Extra(frame);
            if (extra != null) {
                rows.Add(extra);
            }
            if (!string.IsNullOrEmpty(Error)) {
                rows.Add(MessageLine(Error));
            }
            rows.Add(Footer(frame));
            return rows;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            AddEntryBindings(bindings, frame);
            bindings.Add(new KeyBinding(Labels.KeyConfirm, Submit, new KeyChord(ConsoleKey.Enter)) { Order = 30 });
            if (Fill != null && Editor.IsEmpty) {
                bindings.Add(new KeyBinding(Labels.KeyEdit, () => Editor.Text = Fill!, new KeyChord(ConsoleKey.Tab)) { Order = 35 });
            }
            bindings.Add(new KeyBinding(Labels.KeyCancel, Decline, new KeyChord(ConsoleKey.Escape)) { Order = 40 });
        }

        protected virtual void Submit() {
            string? error = TryParse(Editor.Text, out T value);
            if (error != null) {
                Error = error;
                _validateLive = true;
                return;
            }
            Error = null;
            Accept(value);
        }

        protected override bool HandleUnbound(InputEvent input) {
            string before = Editor.Text;
            bool changed = Editor.Handle(input);
            if (changed && _validateLive && !string.Equals(before, Editor.Text, StringComparison.Ordinal)) {
                Error = TryParse(Editor.Text, out _);
            }
            if (changed) {
                OnEdited();
            }
            return changed;
        }

        protected virtual void OnEdited() {
        }
    }
}
