using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>A vertical viewport for ordinary widgets. Lists, grids and logs keep their own virtualized row windows.</summary>
    public sealed class ScrollView : IContainer, IContentOwner {
        private readonly object _gate = new object();
        private int _offset;
        private int _maximum;
        private InputControl? _lastFocus;
        private bool _revealRequested = true;
        private Size _lastSize;
        private bool _hasRendered;
        internal int ViewportHeight { get; set; } = Constraints.Unbounded;
        /// <summary>Creates an empty viewport for initializer or fluent content configuration.</summary>
        public ScrollView() : this(new Rows()) { }
        /// <summary>Creates a viewport over one stable content tree.</summary>
        /// <param name="content">The content to clip and scroll.</param>
        public ScrollView(IWidget content) { Content = content ?? throw new ArgumentNullException(nameof(content)); }
        /// <summary>The content tree.</summary>
        public IWidget Content { get; set; }
        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);
        /// <summary>Maximum rows composed for non-virtualized content. Use a list, grid or log for larger data.</summary>
        public int MaxContentHeight { get; set; } = 16384;
        /// <summary>The first visible content row, clamped after measurement.</summary>
        public int ScrollOffset { get { lock (_gate) { return _hasRendered ? _shown : _offset; } } set { lock (_gate) { _offset = Math.Max(0, value); _shown = _offset; _revealRequested = false; } } }
        private int _shown;
        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };
        /// <summary>Moves the viewport by a number of rows.</summary>
        /// <param name="rows">Positive values move down.</param>
        public void ScrollBy(int rows) { lock (_gate) { _offset = (int)Math.Max(0, Math.Min(_maximum, (long)Math.Min(_offset, _maximum) + rows)); _shown = _offset; } }
        internal void RevealFocus() { lock (_gate) { _revealRequested = true; } }
        [ThreadStatic] private static int _probing;
        // While a layout is measured before it is fitted, scroll views draw without changing what they show.
        internal static IDisposable Probing() => new ProbeScope();
        private sealed class ProbeScope : IDisposable {
            private bool _done;
            internal ProbeScope() { _probing++; }
            public void Dispose() { if (!_done) { _done = true; _probing--; } }
        }
        // Whether the last frame drew a scroll bar in the rightmost column (the content was taller than the view).
        internal bool HasScrollbar { get { lock (_gate) { return _maximum > 0; } } }
        // A press or drag on the bar: the top of the bar shows the start, the bottom the end.
        internal void ScrollToFraction(int row, int height) {
            lock (_gate) {
                _offset = height <= 1 ? 0 : (int)Math.Max(0, Math.Min(_maximum, Math.Round((double)Math.Max(0, Math.Min(height - 1, row)) * _maximum / (height - 1))));
                _shown = _offset;
                _revealRequested = false;
            }
        }
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
                // Taller content gives up the rightmost column to a scroll bar, which can be clicked and dragged.
                int width = region.Width;
                if (height > region.Height && region.Width > 2) {
                    width = region.Width - 1;
                    height = Math.Max(region.Height, Math.Min(MaxContentHeight, Content.Measure(new Constraints(width, MaxContentHeight), context).Height));
                }
                int maximum = height - region.Height;
                // A probe (the app measuring its layout before fitting it) draws without changing the view: the requested
                // offset, the bar range and the focus-reveal memory belong to the pass that is shown.
                bool probe = _probing > 0;
                if (!probe) { _maximum = maximum; }
                int offset = Math.Min(_offset, maximum);
                InputArrangement.Record(this, region.Bounds);
                Rect view = new Rect(region.Bounds.X, region.Bounds.Y, width, region.Height);
                CellBuffer content = new CellBuffer(width, height);
                int before = offset;
                int mark = PointerTargets.Mark();
                using (InputArrangement.Translate(region.Bounds.X, region.Bounds.Y - offset, view)) {
                    Content.Render(new Region(content), context);
                    Rect? focus = InputArrangement.FocusWithin(Content);
                    InputControl? current = InputArrangement.CurrentFocus;
                    bool reveal = !probe && (_revealRequested || _hasRendered && (current != _lastFocus || _lastSize.Width != region.Width || _lastSize.Height != region.Height));
                    if (!probe) {
                        _hasRendered = true;
                        _lastFocus = current; _lastSize = new Size(region.Width, region.Height); _revealRequested = false;
                    }
                    if (reveal && focus.HasValue) {
                        if (focus.Value.Y < offset) { offset = focus.Value.Y; }
                        else if (focus.Value.Bottom > offset + region.Height) { offset = focus.Value.Bottom - region.Height; }
                        offset = Math.Max(0, Math.Min(maximum, offset));
                        _offset = offset;
                    }
                }
                if (before != offset) {
                    content = new CellBuffer(width, height);
                    PointerTargets.Rewind(mark);
                    using (InputArrangement.Translate(region.Bounds.X, region.Bounds.Y - offset, view)) { Content.Render(new Region(content), context); }
                }
                if (!probe) { _shown = offset; }
                region.Buffer.Blit(content, 0, offset, width, region.Height, region.Bounds.X, region.Bounds.Y);
                if (width < region.Width) { DrawScrollbar(region, context, offset, maximum); }
            }
        }

        private static void DrawScrollbar(Region region, RenderContext context, int offset, int maximum) {
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            int height = region.Height;
            int total = height + maximum;
            int thumb = Math.Max(1, height * height / total);
            int top = (int)Math.Round((height - thumb) * (offset / (double)Math.Max(1, maximum)));
            for (int y = 0; y < height; y++) {
                bool onThumb = y >= top && y < top + thumb;
                region.Put(region.Width - 1, y, onThumb ? glyphs.ScrollThumb : glyphs.ScrollTrack, context.Style(onThumb ? Styling.ThemeToken.Muted : Styling.ThemeToken.Subtle));
            }
        }
        private void Validate() { if (MaxContentHeight < 1) { throw new InvalidOperationException("MaxContentHeight must be positive."); } }
    }
}
