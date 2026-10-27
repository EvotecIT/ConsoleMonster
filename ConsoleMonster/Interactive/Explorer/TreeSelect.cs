using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>An embeddable, budgeted object tree using the explorer's lazy nodes, cycle guards and expansion limits.</summary>
    public sealed class TreeSelect : InputControl {
        private readonly ObjectExplorer _explorer;
        private readonly HashSet<ExplorerNode> _expanded = new HashSet<ExplorerNode>();
        private readonly List<(ExplorerNode Node, int Depth)> _visible = new List<(ExplorerNode, int)>();
        private readonly Stack<(ExplorerNode Root, int Cursor)> _history = new Stack<(ExplorerNode, int)>();
        private readonly LineEditor _search = new LineEditor();
        private ExplorerNode _root;
        private int _cursor;
        private int _top;
        private int _pageRows = 12;
        private Rect _body;

        /// <summary>Creates a retained tree over an existing lazy explorer model.</summary>
        public TreeSelect(string key, ObjectExplorer explorer) : base(key) {
            _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
            _root = explorer.Root; Label = explorer.Title; Refresh();
        }
        /// <summary>The subtree currently being browsed.</summary>
        public ExplorerNode Root => _root;
        /// <summary>The selected original node and its path.</summary>
        public ExplorerNode Selected => Read(() => _visible.Count == 0 ? _root : _visible[Math.Min(_cursor, _visible.Count - 1)].Node);
        /// <summary>The number of currently visible nodes, bounded by InspectorOptions.MaxNodes.</summary>
        public int VisibleCount => Read(() => _visible.Count);
        internal bool HasHistory => _history.Count > 0;
        /// <summary>Natural viewport rows.</summary>
        public int Height { get; set; } = 20;
        /// <summary>The substring search over node names and bounded display values.</summary>
        public string Filter { get => Read(() => _search.Text); set => Change(() => { _search.Text = value ?? string.Empty; _cursor = 0; Refresh(); }); }
        /// <summary>Moves within the flattened visible nodes.</summary>
        public void Move(int amount) => Change(() => MoveCore(amount));
        /// <summary>Expands the selected node within the model's budget.</summary>
        public void Expand() => Change(ExpandCore);
        /// <summary>Collapses the selected node or its expanded parent.</summary>
        public void Collapse() => Change(CollapseCore);
        /// <summary>Browses the selected node as a new subtree.</summary>
        public void Drill() => Change(DrillCore);
        /// <summary>Returns to the previous subtree and cursor.</summary>
        public void Back() => Change(BackCore);
        /// <inheritdoc />
        protected override object? ReadValueCore() => _visible.Count == 0 ? null : _visible[Math.Min(_cursor, _visible.Count - 1)].Node;
        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Math.Max(1, Height)));
        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) => RenderBody(region, context);
        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (input.Is(ConsoleKey.UpArrow)) { MoveCore(-1); }
            else if (input.Is(ConsoleKey.DownArrow)) { MoveCore(1); }
            else if (input.Is(ConsoleKey.PageUp)) { MoveCore(-_pageRows); }
            else if (input.Is(ConsoleKey.PageDown)) { MoveCore(_pageRows); }
            else if (input.Is(ConsoleKey.RightArrow)) { ExpandCore(); }
            else if (input.Is(ConsoleKey.LeftArrow)) { CollapseCore(); }
            else if (input.Is(ConsoleKey.Enter)) { DrillCore(); }
            else if (input.Is(ConsoleKey.Backspace) && _search.IsEmpty) { BackCore(); }
            else if (input.Is(ConsoleKey.Escape) && !_search.IsEmpty) { _search.Text = string.Empty; Refresh(); }
            else if (_search.Handle(input)) { _cursor = 0; Refresh(); }
            else { return false; }
            return true;
        }
        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            if (input.Column < _body.X || input.Column >= _body.Right || input.Row < _body.Y || input.Row >= _body.Bottom) { return false; }
            if (input.MouseAction == MouseAction.Wheel) { MoveCore(-Math.Sign(input.WheelDelta) * 3); return true; }
            int row = input.Row - _body.Y + _top;
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && row < _visible.Count) { _cursor = row; return true; }
            return false;
        }
        private void MoveCore(int amount) { _cursor = Math.Max(0, Math.Min(_visible.Count - 1, _cursor + amount)); }
        private void ExpandCore() { if (Selected.CanExpand) { _expanded.Add(Selected); Refresh(); } }
        private void CollapseCore() {
            ExplorerNode selected = Selected;
            if (!_expanded.Remove(selected) && selected.Parent != null && selected.Parent != _root) {
                _expanded.Remove(selected.Parent);
                Refresh();
                _cursor = Math.Max(0, _visible.FindIndex(n => ReferenceEquals(n.Node, selected.Parent)));
            }
            Refresh();
        }
        private void DrillCore() {
            if (_visible.Count == 0 || !Selected.CanExpand) { return; }
            _history.Push((_root, _cursor));
            _root = Selected;
            _cursor = 0;
            _search.Text = string.Empty;
            Refresh();
        }
        private void BackCore() {
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

        internal void RenderBody(Region region, RenderContext context) {
            _body = region.Bounds;
                bool limited = _root.HasMore;
                int memberRows = Math.Max(0, region.Height - (limited ? 1 : 0));
                _pageRows = Math.Max(1, memberRows);
                Refresh();
                if (_visible.Count == 0) { region.Write(0, 0, TextBlockLine(limited ? "… member limit reached" : "No matching members", context, ThemeToken.Muted)); return; }
                int keyWidth = 0;
                foreach ((ExplorerNode node, int depth) in _visible) {
                    keyWidth = Math.Max(keyWidth, 4 + depth * 2 + DisplayWidth.Measure(node.Name));
                }
                keyWidth = Math.Min(keyWidth, Math.Max(1, region.Width * 2 / 3));
                for (int row = 0; row < memberRows && row + _top < _visible.Count; row++) {
                    int index = row + _top;
                    (ExplorerNode node, int depth) = _visible[index];
                    bool focused = Focused && index == _cursor;
                    string marker = node.CanExpand ? (_expanded.Contains(node) ? "- " : "+ ") : "  ";
                    string key = (focused ? "> " : "  ") + new string(' ', depth * 2) + marker + node.Name;
                    Style style = context.Style(focused ? ThemeToken.Focus : node.Error != null ? ThemeToken.Fail : ThemeToken.Text);
                    (string value, bool descriptor) = node.DisplayValue();
                    Style valueStyle = context.Style(node.Error != null ? ThemeToken.Fail : descriptor ? ThemeToken.Muted : ThemeToken.Text);
                    if (focused) { valueStyle = valueStyle.WithBackgroundColor(style.BackgroundColor); }
                    StyledLine keyLine = TextWrapper.Truncate(StyledLine.From(key, style), keyWidth, context.Ellipsis);
                    List<TextSpan> spans = new List<TextSpan>(keyLine) {
                        new TextSpan(new string(' ', keyWidth - keyLine.Width + 2), style),
                        new TextSpan(value, valueStyle)
                    };
                    if (focused) { region.Fill(0, row, region.Width, 1, " ", style); }
                    region.Write(0, row, TextWrapper.Truncate(new StyledLine(spans), region.Width, context.Ellipsis));
                }
                if (_root.HasMore && region.Height > 1) { region.Write(0, region.Height - 1, TextBlockLine("… more items (limit)", context, ThemeToken.Muted)); }
        }
        private static StyledLine TextBlockLine(string text, RenderContext context, ThemeToken token) => new StyledLine(new[] { new TextSpan(text, context.Style(token)) });
    }
}
