using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Navigation and composition of the explorer screen.</summary>
    internal sealed class ExplorerView : InteractiveView {
        private readonly ObjectExplorer _explorer;
        private readonly HashSet<ExplorerNode> _expanded = new HashSet<ExplorerNode>();
        private readonly List<(ExplorerNode Node, int Depth)> _visible = new List<(ExplorerNode, int)>();
        private readonly Stack<(ExplorerNode Root, int Cursor)> _history = new Stack<(ExplorerNode, int)>();
        private readonly LineEditor _search = new LineEditor();
        private ExplorerNode _root;
        private bool _searching;
        private int _cursor;
        private int _top;
        private int _pageRows = 12;

        public ExplorerView(ObjectExplorer explorer, PromptLabels labels) : base(labels) {
            _explorer = explorer;
            _root = explorer.Root;
            Refresh();
        }

        public bool Accepted { get; private set; }

        public ExplorerNode Selected => _visible.Count == 0 ? _root : _visible[Math.Min(_cursor, _visible.Count - 1)].Node;

        public IWidget Snapshot() => new ExplorerSnapshot(this);

        protected override IWidget Build(ViewFrame frame) {
            _pageRows = Math.Max(1, frame.Height - (_searching ? 6 : 5));
            return Compose(frame);
        }

        private IWidget Compose(ViewFrame? frame) {
            Refresh();
            Rows rows = new Rows();
            rows.Add(new HeaderBar(_explorer.Title) { Right = _visible.Count + " members" }, LayoutSize.Fixed(1));
            rows.Add(TextBlock.Plain(_root.Path, ThemeToken.Accent), LayoutSize.Fixed(1));
            if (_searching) { rows.Add(TextBlock.Plain("/ " + _search.Text, ThemeToken.Muted), LayoutSize.Fixed(1)); }
            IWidget body = new ExplorerBody(this);
            int available = Math.Max(1, (frame?.Height ?? 20) - (_searching ? 6 : 5));
            int gridRows = Math.Min(6, Math.Max(0, available - 2));
            Table? grid = gridRows >= 3 ? InlineGrid() : null;
            if (grid != null) {
                if (gridRows >= 3) {
                    int members = Math.Min(Math.Max(1, _visible.Count + (_root.HasMore ? 1 : 0)), available - gridRows - 1);
                    body = new Rows(new Sized(body, LayoutSize.Fixed(members)), new Sized(grid, LayoutSize.Fixed(gridRows))) { Gap = 1 };
                }
            }
            rows.Add(new Panel(body, "Members") { BorderToken = ThemeToken.Accent }, LayoutSize.Fraction());
            List<KeyBinding> bindings = new List<KeyBinding>();
            if (frame != null) { AddBindings(bindings, frame); }
            rows.Add(frame == null ? TextBlock.Plain("↑↓ move · → expand · ← collapse · Enter drill · Backspace up · / search · F10 choose · Esc quit", ThemeToken.Muted) : (IWidget)new KeyFooter(bindings), LayoutSize.Fixed(1));
            return rows;
        }

        private Table? InlineGrid() {
            if (!(_root.Value is System.Collections.IEnumerable) || _root.Value is System.Collections.IDictionary || _visible.Count == 0) { return null; }
            IReadOnlyList<string> first = _root.Children[0].PreviewNames();
            if (first.Count == 0 || first.Count > 8) { return null; }
            int count = Math.Min(3, _root.Children.Count);
            int cost = 0;
            for (int i = 0; i < count; i++) {
                ExplorerNode node = _root.Children[i];
                IReadOnlyList<string> names = node.PreviewNames();
                if (names.Count != first.Count) { return null; }
                for (int c = 0; c < names.Count; c++) { if (names[c] != first[c]) { return null; } }
                cost += node.PreviewCost(names.Count);
            }
            if (cost > _root.RemainingNodes) { return null; }
            Table table = new Table { Border = TableBorder.Minimal, RecordViewRows = 0 };
            foreach (string name in first) { table.AddColumn(Markup.Escape(name)); }
            for (int i = 0; i < count; i++) {
                IReadOnlyList<ExplorerNode> members = _root.Children[i].Children;
                if (members.Count != first.Count) { return null; }
                object?[] values = new object?[members.Count];
                for (int c = 0; c < members.Count; c++) {
                    if (members[c].Name != first[c]) { return null; }
                    values[c] = members[c].Summary;
                }
                table.AddValues(values);
            }
            return table;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_searching) {
                bindings.Add(new KeyBinding("search done", () => _searching = false, new KeyChord(ConsoleKey.Enter), new KeyChord(ConsoleKey.Escape)));
                return;
            }
            bindings.Add(new KeyBinding("move", () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding("expand", Expand, new KeyChord(ConsoleKey.RightArrow)) { Order = 20 });
            bindings.Add(new KeyBinding("collapse", Collapse, new KeyChord(ConsoleKey.LeftArrow)) { Order = 21, InFooter = false });
            bindings.Add(new KeyBinding("drill", Drill, new KeyChord(ConsoleKey.Enter)) { Order = 25 });
            bindings.Add(new KeyBinding("up", Back, new KeyChord(ConsoleKey.Backspace)) { Order = 26, InFooter = false });
            bindings.Add(new KeyBinding("search", () => _searching = true, new KeyChord('/')) { Order = 30 });
            bindings.Add(new KeyBinding("choose", Accept, new KeyChord(ConsoleKey.F10), new KeyChord(ConsoleKey.Enter, KeyModifiers.Control)) { Order = 40 });
            bindings.Add(new KeyBinding("quit", Cancel, new KeyChord(ConsoleKey.Escape)) { Order = 50 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(-_pageRows), new KeyChord(ConsoleKey.PageUp)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(_pageRows), new KeyChord(ConsoleKey.PageDown)) { InFooter = false });
            bindings.Add(HelpBinding(false));
        }

        protected override bool HandleUnbound(InputEvent input) {
            if (!_searching) { return false; }
            bool changed = _search.Handle(input);
            if (changed) { _cursor = 0; Refresh(); }
            return changed;
        }

        private void Accept() { if (_visible.Count == 0) { return; } Accepted = true; IsDone = true; }
        private void Move(int amount) { _cursor = Math.Max(0, Math.Min(_visible.Count - 1, _cursor + amount)); }
        private void Expand() { if (Selected.CanExpand) { _expanded.Add(Selected); Refresh(); } }
        private void Collapse() {
            ExplorerNode selected = Selected;
            if (!_expanded.Remove(selected) && selected.Parent != null && selected.Parent != _root) {
                _expanded.Remove(selected.Parent);
                Refresh();
                _cursor = Math.Max(0, _visible.FindIndex(n => ReferenceEquals(n.Node, selected.Parent)));
            }
            Refresh();
        }
        private void Drill() {
            if (!Selected.CanExpand) { return; }
            _history.Push((_root, _cursor));
            _root = Selected;
            _cursor = 0;
            _search.Text = string.Empty;
            Refresh();
        }
        private void Back() {
            if (_history.Count == 0) { return; }
            (_root, _cursor) = _history.Pop();
            _search.Text = string.Empty;
            Refresh();
        }
        private void Refresh() {
            _visible.Clear();
            Add(_root, 0);
            if (_visible.Count == 0 && _search.Text.Length == 0 && !_root.CanExpand) { _visible.Add((_root, 0)); }
            _cursor = Math.Max(0, Math.Min(_cursor, _visible.Count - 1));
            if (_cursor < _top) { _top = _cursor; }
            if (_cursor >= _top + _pageRows) { _top = _cursor - _pageRows + 1; }
        }
        private void Add(ExplorerNode parent, int depth) {
            foreach (ExplorerNode node in parent.Children) {
                if (_visible.Count >= Math.Max(1, _explorer.Options.MaxNodes)) { break; }
                string filter = _search.Text;
                if (filter.Length == 0 || node.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || node.DisplayValue().Text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) { _visible.Add((node, depth)); }
                if (_expanded.Contains(node)) { Add(node, depth + 1); }
            }
        }

        private sealed class ExplorerBody : IWidget {
            private readonly ExplorerView _view;
            public ExplorerBody(ExplorerView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? Math.Max(1, _view._visible.Count) : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) {
                bool limited = _view._root.HasMore;
                int memberRows = Math.Max(0, region.Height - (limited ? 1 : 0));
                _view._pageRows = Math.Max(1, memberRows);
                _view.Refresh();
                if (_view._visible.Count == 0) { region.Write(0, 0, TextBlockLine(limited ? "… member limit reached" : "No matching members", context, ThemeToken.Muted)); return; }
                int keyWidth = 0;
                foreach ((ExplorerNode node, int depth) in _view._visible) {
                    keyWidth = Math.Max(keyWidth, 4 + depth * 2 + DisplayWidth.Measure(node.Name));
                }
                keyWidth = Math.Min(keyWidth, Math.Max(1, region.Width * 2 / 3));
                for (int row = 0; row < memberRows && row + _view._top < _view._visible.Count; row++) {
                    int index = row + _view._top;
                    (ExplorerNode node, int depth) = _view._visible[index];
                    bool focused = index == _view._cursor;
                    string marker = node.CanExpand ? (_view._expanded.Contains(node) ? "- " : "+ ") : "  ";
                    string key = (focused ? "> " : "  ") + new string(' ', depth * 2) + marker + node.Name;
                    Style style = context.Style(focused ? ThemeToken.Focus : node.Error != null ? ThemeToken.Fail : ThemeToken.Text);
                    (string value, bool descriptor) = node.DisplayValue();
                    Style valueStyle = context.Style(node.Error != null ? ThemeToken.Fail : descriptor ? ThemeToken.Muted : ThemeToken.Text);
                    if (focused) { valueStyle = valueStyle.WithBackground(style.Background); }
                    StyledLine keyLine = TextWrapper.Truncate(StyledLine.From(key, style), keyWidth, context.Ellipsis);
                    List<TextSpan> spans = new List<TextSpan>(keyLine) {
                        new TextSpan(new string(' ', keyWidth - keyLine.Width + 2), style),
                        new TextSpan(value, valueStyle)
                    };
                    if (focused) { region.Fill(0, row, region.Width, 1, " ", style); }
                    region.Write(0, row, TextWrapper.Truncate(new StyledLine(spans), region.Width, context.Ellipsis));
                }
                if (_view._root.HasMore && region.Height > 1) { region.Write(0, region.Height - 1, TextBlockLine("… more items (limit)", context, ThemeToken.Muted)); }
            }
            private static StyledLine TextBlockLine(string text, RenderContext context, ThemeToken token) => new StyledLine(new[] { new TextSpan(text, context.Style(token)) });
        }

        private sealed class ExplorerSnapshot : IWidget {
            private readonly ExplorerView _view;
            public ExplorerSnapshot(ExplorerView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Math.Max(9, _view._visible.Count + 7)));
            public void Render(Region region, RenderContext context) => _view.Compose(new ViewFrame(context, region.Width, region.Height, false, TimeSpan.Zero)).Render(region, context);
        }
    }
}
