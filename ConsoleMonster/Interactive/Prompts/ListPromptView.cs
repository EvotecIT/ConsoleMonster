using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>The shared view of the selection prompts.</summary>
    internal abstract class ListPromptView<T> : PromptView<T> {
        protected ListPromptView(Prompt<T> prompt, PromptLabels labels, IEnumerable<Choice> choices, bool multi, int pageSize, Func<Choice, IWidget?>? preview) : base(prompt, labels) {
            List = new ChoiceList(choices, multi) { Labels = labels };
            Pane = new ChoicePane(List, Caret) { PageSize = pageSize, Preview = preview };
        }

        public ChoiceList List { get; }

        public ChoicePane Pane { get; }

        protected abstract void Submit();

        protected abstract void Picked(Choice choice);

        protected override IWidget Build(ViewFrame frame) {
            Rows rows = new Rows();
            rows.Add(Header(null));
            if (!string.IsNullOrEmpty(Prompt.Description)) {
                rows.Add(new Padding(new UiParagraph(Prompt.Description!), 2, 0, 0, 0));
            }
            if (Error != null && List.Multi && List.ValidateSelection() == null) {
                Error = null;
            }
            Pane.Error = Error;
            Pane.Fill = !frame.Inline;
            rows.Add(new Padding(Pane, 2, 0, 0, 0), frame.Inline ? (LayoutSize?)null : LayoutSize.Fraction());
            rows.Add(Footer(frame));
            return rows;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            List.AddBindings(bindings, frame.Context);
            bindings.Add(new KeyBinding(List.Multi ? Labels.KeyConfirm : Labels.KeySelect, () => { Error = null; Submit(); }, new KeyChord(ConsoleKey.Enter)) { Order = 25 });
            bindings.Add(new KeyBinding(List.IsFiltering || List.FilterMode ? Labels.KeyClearFilter : Labels.KeyCancel, () => {
                if (!List.ClearFilter()) {
                    Decline();
                }
            }, new KeyChord(ConsoleKey.Escape)) { Order = 70 });
            bindings.Add(HelpBinding(false));
        }

        protected override bool HandleUnbound(InputEvent input) {
            if (List.HandleText(input, out Choice? picked)) {
                Error = null;
                Pane.InvalidatePreview();
                if (picked != null) {
                    Picked(picked);
                }
                return true;
            }
            return false;
        }
    }
}
