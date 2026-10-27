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
        private readonly TreeSelect _tree;
        private readonly LineEditor _search = new LineEditor();
        private bool _searching;
        private int _pageRows = 12;
        private ExplorerNode _root => _tree.Root;

        public ExplorerView(ObjectExplorer explorer, PromptLabels labels) : base(labels) {
            _explorer = explorer;
            _tree = new TreeSelect("Explorer", explorer) { Focused = true };
            Refresh();
        }

        public bool Accepted { get; private set; }

        public ExplorerNode Selected => _tree.Selected;

        public IWidget Snapshot() => new ExplorerSnapshot(this);

        protected override IWidget Build(ViewFrame frame) {
            _pageRows = Math.Max(1, frame.Height - (_searching ? 6 : 5));
            return Compose(frame);
        }

        private IWidget Compose(ViewFrame? frame) {
            Refresh();
            Rows rows = new Rows();
            rows.Add(new HeaderBar(_explorer.Title) { HeaderRight = _tree.VisibleCount + " members" }, LayoutSize.Fixed(1));
            rows.Add(TextBlock.Plain(_root.Path, ThemeToken.Accent), LayoutSize.Fixed(1));
            if (_searching) { rows.Add(TextBlock.Plain("/ " + _search.Text, ThemeToken.Muted), LayoutSize.Fixed(1)); }
            IWidget body = new ExplorerBody(this);
            int available = Math.Max(1, (frame?.Height ?? 20) - (_searching ? 6 : 5));
            int gridRows = Math.Min(6, Math.Max(0, available - 2));
            Table? grid = gridRows >= 3 ? InlineGrid() : null;
            if (grid != null) {
                if (gridRows >= 3) {
                    int members = Math.Min(Math.Max(1, _tree.VisibleCount + (_root.HasMore ? 1 : 0)), available - gridRows - 1);
                    body = new Rows(new Sized(body, LayoutSize.Fixed(members)), new Sized(grid, LayoutSize.Fixed(gridRows))) { Gap = 1 };
                }
            }
            rows.Add(new Panel(body, "Members") { BorderRole = ThemeToken.Accent }, LayoutSize.Fraction());
            List<KeyBinding> bindings = new List<KeyBinding>();
            if (frame != null) { AddBindings(bindings, frame); }
            rows.Add(frame == null ? TextBlock.Plain("↑↓ move · → expand · ← collapse · Enter drill · Backspace up · / search · F10 choose · Esc quit", ThemeToken.Muted) : (IWidget)new KeyFooter(bindings), LayoutSize.Fixed(1));
            return rows;
        }

        private Table? InlineGrid() {
            if (!(_root.Value is System.Collections.IEnumerable) || _root.Value is System.Collections.IDictionary || _tree.VisibleCount == 0 || _search.Text.Length > 0) { return null; }
            if (_root.Children.Count == 0) { return null; }
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
            bindings.Add(HelpBinding());
        }

        protected override bool HandleUnbound(InputEvent input) {
            if (!_searching) { return false; }
            bool changed = _search.Handle(input);
            if (changed) { _tree.Filter = _search.Text; }
            return changed;
        }

        private void Accept() { if (_tree.VisibleCount == 0) { return; } Accepted = true; IsDone = true; }
        private void Move(int amount) => _tree.Move(amount);
        private void Expand() => _tree.Expand();
        private void Collapse() => _tree.Collapse();
        private void Drill() { if (_tree.VisibleCount == 0 || !_tree.Selected.CanExpand) { return; } _tree.Drill(); _search.Text = string.Empty; }
        private void Back() { if (!_tree.HasHistory) { return; } _tree.Back(); _search.Text = string.Empty; }
        private void Refresh() { if (_tree.Filter != _search.Text) { _tree.Filter = _search.Text; } }

        private sealed class ExplorerBody : IWidget {
            private readonly ExplorerView _view;
            public ExplorerBody(ExplorerView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? Math.Max(1, _view._tree.VisibleCount) : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) => _view._tree.RenderBody(region, context);
        }

        private sealed class ExplorerSnapshot : IWidget {
            private readonly ExplorerView _view;
            public ExplorerSnapshot(ExplorerView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Math.Max(9, _view._tree.VisibleCount + 7)));
            public void Render(Region region, RenderContext context) => _view.Compose(new ViewFrame(context, region.Width, region.Height, false, TimeSpan.Zero)).Render(region, context);
        }
    }
}
