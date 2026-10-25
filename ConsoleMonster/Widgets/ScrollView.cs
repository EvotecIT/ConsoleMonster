using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>A vertical viewport for ordinary widgets. Lists, grids and logs keep their own virtualized row windows.</summary>
    public sealed class ScrollView : IContainer {
        private readonly object _gate = new object();
        private int _offset;
        private int _maximum;
        private InputControl? _lastFocus;
        private bool _revealRequested = true;
        private Size _lastSize;
        internal int ViewportHeight { get; set; } = Constraints.Unbounded;
        /// <summary>Creates a viewport over one stable content tree.</summary>
        /// <param name="content">The content to clip and scroll.</param>
        public ScrollView(IWidget content) { Content = content ?? throw new ArgumentNullException(nameof(content)); }
        /// <summary>The content tree.</summary>
        public IWidget Content { get; }
        /// <summary>Maximum rows composed for non-virtualized content. Use a list, grid or log for larger data.</summary>
        public int MaxContentHeight { get; set; } = 16384;
        /// <summary>The first visible content row, clamped after measurement.</summary>
        public int ScrollOffset { get { lock (_gate) { return _offset; } } set { lock (_gate) { _offset = Math.Max(0, value); } } }
        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };
        /// <summary>Moves the viewport by a number of rows.</summary>
        /// <param name="rows">Positive values move down.</param>
        public void ScrollBy(int rows) { lock (_gate) { _offset = (int)Math.Max(0, Math.Min(_maximum, (long)_offset + rows)); } }
        internal void RevealFocus() { lock (_gate) { _revealRequested = true; } }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            Validate();
            Size natural = Content.Measure(new Constraints(constraints.MaxWidth, MaxContentHeight), context);
            return new Size(Math.Min(constraints.MaxWidth, natural.Width), Math.Min(Math.Min(constraints.MaxHeight, ViewportHeight), natural.Height));
        }
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            lock (_gate) {
                Validate();
                int height = Math.Max(region.Height, Math.Min(MaxContentHeight, Content.Measure(new Constraints(region.Width, MaxContentHeight), context).Height));
                _maximum = height - region.Height;
                _offset = Math.Min(_offset, _maximum);
                InputArrangement.Record(this, region.Bounds);
                CellBuffer content = new CellBuffer(region.Width, height);
                int before = _offset;
                using (InputArrangement.Translate(region.Bounds.X, region.Bounds.Y - _offset, region.Bounds)) {
                    Content.Render(new Region(content), context);
                    Rect? focus = InputArrangement.FocusWithin(Content);
                    InputControl? current = InputArrangement.CurrentFocus;
                    bool reveal = _revealRequested || current != _lastFocus || _lastSize.Width != region.Width || _lastSize.Height != region.Height;
                    _lastFocus = current; _lastSize = new Size(region.Width, region.Height); _revealRequested = false;
                    if (reveal && focus.HasValue) {
                        if (focus.Value.Y < _offset) { _offset = focus.Value.Y; }
                        else if (focus.Value.Bottom > _offset + region.Height) { _offset = focus.Value.Bottom - region.Height; }
                        _offset = Math.Max(0, Math.Min(_maximum, _offset));
                    }
                }
                if (before != _offset) {
                    content = new CellBuffer(region.Width, height);
                    using (InputArrangement.Translate(region.Bounds.X, region.Bounds.Y - _offset, region.Bounds)) { Content.Render(new Region(content), context); }
                }
                region.Buffer.Blit(content, 0, _offset, region.Width, region.Height, region.Bounds.X, region.Bounds.Y);
            }
        }
        private void Validate() { if (MaxContentHeight < 1) { throw new InvalidOperationException("MaxContentHeight must be positive."); } }
    }
}
