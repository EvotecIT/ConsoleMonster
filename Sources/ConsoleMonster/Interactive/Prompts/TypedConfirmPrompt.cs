using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The confirmation for destructive actions: the user types a given text (a name, a domain) before Enter is accepted.
    /// Returns true when confirmed and false when the user backs out with Esc.
    /// </summary>
    public sealed class TypedConfirmPrompt : Prompt<bool> {
        /// <summary>Creates a typed confirmation.</summary>
        /// <param name="title">The question ("Remove 3 servers from the inventory?").</param>
        /// <param name="expected">The text to type.</param>
        public TypedConfirmPrompt(string title, string expected) : base(title) {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
        }

        /// <summary>The text to type.</summary>
        public string Expected { get; }

        /// <summary>Whether case must match too.</summary>
        public bool CaseSensitive { get; set; }

        /// <summary>A warning under the question (default: none).</summary>
        public string? Warning { get; set; }

        /// <inheritdoc />
        public override string Describe(bool value) => value ? PromptLabels.Default.Yes : PromptLabels.Default.No;

        internal bool Matches(string text) => string.Equals(text.Trim(), Expected, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        internal override PromptView<bool> CreateView(PromptLabels labels) => new TypedView(this, labels);

        internal override bool AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            if (!string.IsNullOrEmpty(Warning)) {
                lines.Say(Warning!);
            }
            string answer = lines.Text(Title + " " + PromptLabels.Format(lines.Labels.TypeToConfirm, "'" + Expected + "'"), Description, string.Empty, null, false, cancellationToken);
            return Matches(answer);
        }

        private sealed class TypedView : PromptView<bool> {
            private readonly TypedConfirmPrompt _prompt;
            private readonly LineEditor _editor = new LineEditor();

            public TypedView(TypedConfirmPrompt prompt, PromptLabels labels) : base(prompt, labels) {
                _prompt = prompt;
            }

            protected override IWidget Build(ViewFrame frame) {
                Rows rows = new Rows();
                rows.Add(Header(null, ThemeToken.Fail, "!"));
                string? text = _prompt.Warning ?? _prompt.Description;
                if (!string.IsNullOrEmpty(text)) {
                    rows.Add(new Padding(new UiParagraph(text!, ThemeToken.Warn), 2, 0, 0, 0));
                }
                rows.Add(new UiLine(Confirmation) { Indent = 2 });
                TextInputWidget input = new TextInputWidget(_editor, Caret) { Token = _prompt.Matches(_editor.Text) ? ThemeToken.Pass : ThemeToken.Text };
                rows.Add(new Columns(new Sized(new UiLine(context => new StyledLine(new[] { PromptMark(context) })) { Indent = 2 }, LayoutSize.Fixed(4)), input) { Gap = 0 });
                if (!string.IsNullOrEmpty(Error)) {
                    rows.Add(MessageLine(Error));
                }
                rows.Add(Footer(frame));
                return rows;
            }

            private StyledLine Confirmation(Canvas.RenderContext context) {
                string format = Labels.TypeToConfirm;
                int at = format.IndexOf("{0}", StringComparison.Ordinal);
                List<TextSpan> spans = new List<TextSpan>();
                if (at < 0) {
                    spans.Add(new TextSpan(format + " ", context.Style(ThemeToken.Muted)));
                    spans.Add(new TextSpan(_prompt.Expected, context.Style(ThemeToken.Highlight)));
                } else {
                    spans.Add(new TextSpan(format.Substring(0, at), context.Style(ThemeToken.Muted)));
                    spans.Add(new TextSpan(_prompt.Expected, context.Style(ThemeToken.Highlight)));
                    spans.Add(new TextSpan(format.Substring(at + 3), context.Style(ThemeToken.Muted)));
                }
                return new StyledLine(spans);
            }

            public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
                bindings.Add(new KeyBinding(Labels.KeyConfirm, () => {
                    if (_prompt.Matches(_editor.Text)) {
                        Accept(true);
                    } else {
                        Error = PromptLabels.Format(Labels.TypedMismatch, _prompt.Expected);
                    }
                }, new KeyChord(ConsoleKey.Enter)) { Order = 30 });
                bindings.Add(new KeyBinding(Labels.KeyCancel, () => Accept(false), new KeyChord(ConsoleKey.Escape)) { Order = 40 });
            }

            protected override bool HandleUnbound(InputEvent input) {
                Error = null;
                return _editor.Handle(input);
            }
        }
    }
}
