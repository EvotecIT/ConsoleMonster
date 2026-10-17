using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A yes/no question with the default shown: <c>? Continue?  [ Yes ]  No</c>. Y and N answer at once, arrows move
    /// between the answers, Enter takes the focused one, Esc answers no.
    /// </summary>
    public sealed class ConfirmPrompt : Prompt<bool> {
        /// <summary>Creates a confirmation.</summary>
        /// <param name="title">The question.</param>
        public ConfirmPrompt(string title) : base(title) {
        }

        /// <summary>Creates a confirmation with a default.</summary>
        /// <param name="title">The question.</param>
        /// <param name="defaultValue">The default answer.</param>
        public ConfirmPrompt(string title, bool defaultValue) : base(title) {
            Default = defaultValue;
        }

        /// <summary>Marks the question as destructive: a warning mark, and No is focused unless the default says otherwise.</summary>
        public bool Destructive { get; set; }

        /// <summary>The caption of the positive answer; null uses the labels.</summary>
        public string? YesText { get; set; }

        /// <summary>The caption of the negative answer; null uses the labels.</summary>
        public string? NoText { get; set; }

        /// <inheritdoc />
        public override string Describe(bool value) => value ? YesText ?? PromptLabels.Default.Yes : NoText ?? PromptLabels.Default.No;

        internal override PromptView<bool> CreateView(PromptLabels labels) => new ConfirmView(this, labels);

        internal override bool AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            return lines.Confirm(Title, Description, HasDefault ? Default : (bool?)null, cancellationToken);
        }

        private sealed class ConfirmView : PromptView<bool> {
            private readonly ConfirmPrompt _prompt;
            private int _focus;

            public ConfirmView(ConfirmPrompt prompt, PromptLabels labels) : base(prompt, labels) {
                _prompt = prompt;
                _focus = prompt.HasDefault ? (prompt.Default ? 0 : 1) : prompt.Destructive ? 1 : 0;
            }

            private string Yes => _prompt.YesText ?? Labels.Yes;

            private string No => _prompt.NoText ?? Labels.No;

            protected override IWidget Build(ViewFrame frame) {
                Rows rows = new Rows();
                rows.Add(new UiLine(context => {
                    InteractiveGlyphs g = InteractiveGlyphs.For(context);
                    List<TextSpan> spans = new List<TextSpan> {
                        new TextSpan((_prompt.Destructive ? "!" : g.Question) + " ", context.Style(_prompt.Destructive ? ThemeToken.Warn : ThemeToken.Accent).With(TextDecoration.Bold)),
                        new TextSpan(_prompt.Title, context.Style(ThemeToken.Title)),
                        new TextSpan("  ", Style.Plain)
                    };
                    spans.AddRange(Buttons.Spans(new[] { Yes, No }, _focus, context));
                    return new StyledLine(spans);
                }));
                if (!string.IsNullOrEmpty(_prompt.Description)) {
                    rows.Add(new Padding(new UiParagraph(_prompt.Description!), 2, 0, 0, 0));
                }
                rows.Add(Footer(frame));
                return rows;
            }

            public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
                InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
                char yes = char.ToLowerInvariant(Yes[0]);
                char no = char.ToLowerInvariant(No[0]);
                bindings.Add(new KeyBinding(Labels.KeyMove, () => _focus = 1 - _focus, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.RightArrow), new KeyChord(ConsoleKey.Tab), new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)) { KeyText = g.KeyLeftRight, Order = 10 });
                bindings.Add(new KeyBinding(Labels.KeyYesNo, () => { }) { KeyText = yes + "/" + no, Order = 20 });
                bindings.Add(new KeyBinding(string.Empty, () => Accept(true), new KeyChord(yes), new KeyChord(char.ToUpperInvariant(yes))) { InFooter = false });
                bindings.Add(new KeyBinding(string.Empty, () => Accept(false), new KeyChord(no), new KeyChord(char.ToUpperInvariant(no))) { InFooter = false });
                bindings.Add(new KeyBinding(Labels.KeyConfirm, () => Accept(_focus == 0), new KeyChord(ConsoleKey.Enter)) { Order = 30 });
                bindings.Add(new KeyBinding(No.ToLowerInvariant(), () => Accept(false), new KeyChord(ConsoleKey.Escape)) { Order = 40 });
            }
        }
    }
}
