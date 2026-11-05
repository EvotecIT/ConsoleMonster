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
        private readonly FindBar _find = new FindBar();
        private List<string>? _lines;

        internal WidgetPagerView(IWidget content, string title, PromptLabels labels, int? contentWidth = null) : base(labels) {
            _content = content;
            _title = title ?? string.Empty;
            _contentWidth = contentWidth;
        }

        protected override IWidget Build(ViewFrame frame) {
            int width = Math.Min(frame.Width, _contentWidth ?? frame.Width);
            if (_document == null || _document.Width != width) {
                _document = FrameComposer.RenderBlock(_content, width, frame.Context);
                _lines = null;
                if (_find.Active) { _find.Search(Lines(), _top); }
            }
            _pageRows = Math.Max(1, frame.Height - 2);
            Move(0);
            Rows rows = new Rows();
            rows.Add(new HeaderBar(Text.Markup.Escape(_title)) {
                HeaderRight = (_top + 1).ToString(CultureInfo.InvariantCulture) + "–"
                    + Math.Min(_document.Height, _top + _pageRows).ToString(CultureInfo.InvariantCulture)
                    + " / " + _document.Height.ToString(CultureInfo.InvariantCulture)
            }, LayoutSize.Fixed(1));
            rows.Add(new WidgetPagerBody(_document, _top, _find.Active ? _find : null), LayoutSize.Fraction());
            if (_find.Open) {
                rows.Add(_find.Widget(Labels, screenBottom: true), LayoutSize.Fixed(1));
                return rows;
            }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings), LayoutSize.Fixed(1));
            return rows;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_find.Open) { FindBar.AddKeys(bindings, Labels); }
            else bindings.Add(new KeyBinding(Labels.KeySearch, OpenFind, new KeyChord('/'), new KeyChord(ConsoleKey.F, KeyModifiers.Control)) { KeyText = "/", Order = 30, InFooter = false });
            if (_find.Active && !_find.Open) {
                bindings.Add(new KeyBinding(Labels.KeyNext, () => Step(1), new KeyChord('n')) { Order = 31 });
                bindings.Add(new KeyBinding(Labels.KeyPrevious, () => Step(-1), new KeyChord('N')) { Order = 32 });
                bindings.Add(new KeyBinding(string.Empty, () => _find.Close(false), new KeyChord(ConsoleKey.Escape)) { InFooter = false });
            }
            bindings.Add(new KeyBinding(Labels.KeyMove, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = "↑/↓", Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow), new KeyChord(ConsoleKey.Enter)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyPage, () => Move(_pageRows), new KeyChord(ConsoleKey.PageDown), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "pgup/pgdn", Order = 20 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(-_pageRows), new KeyChord(ConsoleKey.PageUp)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => _top = 0, new KeyChord(ConsoleKey.Home)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(int.MaxValue), new KeyChord(ConsoleKey.End)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyClose, () => IsDone = true, new KeyChord(ConsoleKey.Escape), new KeyChord('q')) { KeyText = "esc/q", Order = 40 });
            bindings.Add(HelpBinding());
        }

        // An open find bar takes the keys before the pager's bindings: typing, Enter and Shift+Enter, Escape.
        protected override bool HandlePriorityInput(InputEvent input) {
            if (!_find.Open) { return false; }
            switch (_find.Handle(input)) {
                case FindKey.Edited: _find.Search(Lines(), _top); Reveal(); return true;
                case FindKey.Next: Step(1); return true;
                case FindKey.Previous: Step(-1); return true;
                case FindKey.Closed: _find.Close(true); return true;
                case FindKey.Kept: return true;
                default: return input.Kind == InputEventKind.Key && !input.Is(ConsoleKey.UpArrow) && !input.Is(ConsoleKey.DownArrow) && !input.Is(ConsoleKey.PageUp) && !input.Is(ConsoleKey.PageDown);
            }
        }

        private void OpenFind() { _find.Start(); _find.Search(Lines(), _top); }

        private void Step(int delta) { if (_find.Step(delta)) { Reveal(); } }

        // Keeps the current match on screen, about a third of the way down the page.
        private void Reveal() {
            if (_find.Current < 0) { return; }
            int line = _find.Matches[_find.Current].Line;
            if (line >= _top && line < _top + _pageRows) { return; }
            _top = 0;
            Move(line - _pageRows / 3);
        }

        // The document's rows as text, which is what the reader sees and searches.
        private List<string> Lines() {
            if (_lines != null || _document == null) { return _lines ?? new List<string>(); }
            _lines = new List<string>(_document.Height);
            System.Text.StringBuilder row = new System.Text.StringBuilder();
            for (int y = 0; y < _document.Height; y++) {
                row.Clear();
                for (int x = 0; x < _document.Width; x++) {
                    Cell cell = _document[x, y];
                    if (!cell.IsContinuation) { row.Append(cell.Glyph.Length == 0 ? " " : cell.Glyph); }
                }
                _lines.Add(row.ToString());
            }
            return _lines;
        }

        private void Move(int delta) {
            int max = Math.Max(0, (_document?.Height ?? 0) - _pageRows);
            _top = (int)Math.Max(0L, Math.Min(max, (long)_top + delta));
        }
    }
}
