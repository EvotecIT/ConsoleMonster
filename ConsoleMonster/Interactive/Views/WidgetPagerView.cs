using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A reading viewport that reflows at the current width and clamps scrolling after resize.</summary>
    internal sealed class WidgetPagerView : InteractiveView {
        private readonly IWidget _content;
        private readonly string _title;
        private readonly int? _contentWidth;
        private CellBuffer? _document;
        private int _top;
        private int _pageRows = 1;

        internal WidgetPagerView(IWidget content, string title, PromptLabels labels, int? contentWidth = null) : base(labels) {
            _content = content;
            _title = title ?? string.Empty;
            _contentWidth = contentWidth;
        }

        protected override IWidget Build(ViewFrame frame) {
            int width = Math.Min(frame.Width, _contentWidth ?? frame.Width);
            if (_document == null || _document.Width != width) {
                _document = FrameComposer.RenderBlock(_content, width, frame.Context);
            }
            _pageRows = Math.Max(1, frame.Height - 2);
            Move(0);
            Rows rows = new Rows();
            rows.Add(new HeaderBar(Text.Markup.Escape(_title)) {
                Right = (_top + 1).ToString(CultureInfo.InvariantCulture) + "–"
                    + Math.Min(_document.Height, _top + _pageRows).ToString(CultureInfo.InvariantCulture)
                    + " / " + _document.Height.ToString(CultureInfo.InvariantCulture)
            }, LayoutSize.Fixed(1));
            rows.Add(new WidgetPagerBody(_document, _top), LayoutSize.Fraction());
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings), LayoutSize.Fixed(1));
            return rows;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(Labels.KeyMove, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = "↑/↓", Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow), new KeyChord(ConsoleKey.Enter)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyPage, () => Move(_pageRows), new KeyChord(ConsoleKey.PageDown), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "pgup/pgdn", Order = 20 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(-_pageRows), new KeyChord(ConsoleKey.PageUp)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => _top = 0, new KeyChord(ConsoleKey.Home)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(int.MaxValue), new KeyChord(ConsoleKey.End)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyClose, () => IsDone = true, new KeyChord(ConsoleKey.Escape), new KeyChord('q')) { KeyText = "esc/q", Order = 40 });
            bindings.Add(HelpBinding());
        }

        private void Move(int delta) {
            int max = Math.Max(0, (_document?.Height ?? 0) - _pageRows);
            _top = (int)Math.Max(0L, Math.Min(max, (long)_top + delta));
        }
    }
}
