using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A section that folds: a focusable header row with ▸/▾ (<c>+</c>/<c>-</c> in ASCII) above its content. Collapsed, the
    /// header shows <see cref="Summary"/> ("Examples (6)") and the content is not mounted, so its inputs leave the focus
    /// order. Enter, Space, ←/→ and a click on the header fold and unfold it; the value is whether it is expanded.
    /// </summary>
    public sealed class Collapsible : InputControl, IContainer, IWidget {
        private const int Indent = 2;
        private IWidget? _content;
        private bool _expanded;
        private bool _pressed;
        private string? _summary;

        /// <summary>Creates a section.</summary>
        /// <param name="key">The application-state key holding whether the section is expanded.</param>
        /// <param name="content">The folded content.</param>
        public Collapsible(string key, IWidget? content = null) : base(key, false) { _content = content; }

        /// <summary>The content shown under the header while expanded.</summary>
        public IWidget? Content { get => Read(() => _content); set => Change(() => _content = value); }

        /// <summary>Whether the content is shown. Assignments notify only when the value changes.</summary>
        public bool Expanded { get => Read(() => _expanded); set => Change(() => _expanded = value); }

        /// <summary>Muted text after the label while collapsed, such as a count; null shows the label alone.</summary>
        public string? Summary { get => Read(() => _summary); set => Change(() => _summary = value); }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children {
            get { IWidget? content = Content; return content == null ? Array.Empty<IWidget>() : new[] { content }; }
        }

        // The accordion this section belongs to; it adds the expand-all and collapse-all keys while a header is focused.
        internal Accordion? Group { get; set; }

        /// <inheritdoc />
        protected override object ReadValueCore() => _expanded;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (input.Is(ConsoleKey.Enter) || input.Is(ConsoleKey.Spacebar)) { _expanded = !_expanded; return true; }
            if (input.Is(ConsoleKey.RightArrow) && !_expanded) { _expanded = true; return true; }
            if (input.Is(ConsoleKey.LeftArrow) && _expanded) { _expanded = false; return true; }
            return false;
        }

        internal override bool Takes(KeyChord chord, RenderContext context) => chord.Modifiers == KeyModifiers.None
            && (chord.Key == ConsoleKey.Enter || chord.Key == ConsoleKey.Spacebar || chord.Key == ConsoleKey.LeftArrow || chord.Key == ConsoleKey.RightArrow);

        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            PromptLabels labels = PromptLabels.For(context.Profile.Language);
            keys.Add(new KeyBinding(labels.KeyExpand, () => { }, new KeyChord(ConsoleKey.Enter), new KeyChord(ConsoleKey.Spacebar)) { Order = 1 });
        }

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            Rect header = Bounds;
            bool inside = input.Column >= header.X && input.Column < header.Right && input.Row >= header.Y && input.Row < header.Bottom;
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) { _pressed = inside; return inside; }
            if (input.MouseAction == MouseAction.Up && input.MouseButton == 1) {
                bool toggle = _pressed && inside;
                _pressed = false;
                if (toggle) { _expanded = !_expanded; }
                return toggle;
            }
            return false;
        }

        internal override void CancelPointer() { _pressed = false; }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) =>
            new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) { return; }
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            bool focused = Focused && Enabled;
            Style marker = context.Style(focused ? ThemeToken.Focus : ThemeToken.Muted);
            if (focused) { region.Write(0, 0, glyphs.Pointer, marker); }
            string fold = context.Glyphs.Tier == GlyphTier.Ascii ? (_expanded ? "-" : "+") : (_expanded ? glyphs.Expanded : glyphs.Collapsed);
            int x = region.Write(Indent, 0, fold, context.Style(focused ? ThemeToken.Focus : ThemeToken.Accent));
            string title = Glyphs.SymbolFallback.Constrain(Label ?? Key, context.Glyphs.Tier);
            if (!Enabled) { title = PromptLabels.Format(PromptLabels.For(context.Profile.Language).Disabled, title); }
            Style titleStyle = context.Style(!Enabled ? ThemeToken.Muted : focused ? ThemeToken.Focus : ThemeToken.Text).With(TextDecoration.Bold);
            x = region.Write(x + 1, 0, TextWrapper.Truncate(title, Math.Max(0, region.Width - x - 1), context.Ellipsis), titleStyle);
            if (!_expanded && !string.IsNullOrEmpty(_summary) && x + 2 < region.Width) {
                string summary = Glyphs.SymbolFallback.Constrain(_summary!, context.Glyphs.Tier);
                x = region.Write(x + 2, 0, TextWrapper.Truncate(summary, region.Width - x - 2, context.Ellipsis), context.Style(ThemeToken.Muted));
            }
            string? problem = ValidationMessage;
            if (!string.IsNullOrEmpty(problem) && x + 2 < region.Width) {
                region.Write(x + 2, 0, TextWrapper.Truncate(ChoicePane.Problem(problem!, context), region.Width - x - 2, context.Ellipsis));
            }
        }

        // The header is the control (focus, clicks and the arranged bounds); the content below is laid out as a child, so a
        // click on an input inside it focuses that input rather than the section.
        Size IWidget.Measure(Constraints constraints, RenderContext context) {
            if (constraints.MaxHeight <= 0) { return new Size(constraints.MaxWidth, 0); }
            IWidget? content = Read(() => _expanded ? _content : null);
            if (content == null || constraints.MaxHeight == 1) { return new Size(constraints.MaxWidth, 1); }
            Size body = content.Measure(new Constraints(Math.Max(0, constraints.MaxWidth - Indent), constraints.IsHeightUnbounded ? Constraints.Unbounded : constraints.MaxHeight - 1), context);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, body.Height + 1));
        }

        void IWidget.Render(Region region, RenderContext context) {
            if (region.Height <= 0) { return; }
            Render(region.Sub(0, 0, region.Width, 1), context);
            IWidget? content = Read(() => _expanded ? _content : null);
            if (content != null && region.Height > 1 && region.Width > Indent) {
                content.Render(region.Sub(Indent, 1, region.Width - Indent, region.Height - 1), context);
            }
        }
    }
}
