using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>A named, swappable layout slot. Keep its inputs stable for the lifetime of an app.</summary>
    public sealed class LayoutRegion : IContainer {
        /// <summary>Creates a named slot.</summary>
        public LayoutRegion(string key, IWidget content) {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("A region needs a key.", nameof(key)); }
            Key = key; Content = content ?? throw new ArgumentNullException(nameof(content));
        }
        /// <summary>The region identity.</summary>
        public string Key { get; }
        /// <summary>The current subtree. Swap existing subtrees while an app runs; configure new inputs before starting.</summary>
        public IWidget Content { get; set; }
        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Content.Measure(constraints, context);
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Content.Render(region, context);
    }

    /// <summary>Equal layout regions that show only the active region below a configurable width. Focus and retained control state follow swapped content.</summary>
    public sealed class LayoutRegions : IContainer {
        private readonly List<LayoutRegion> _regions = new List<LayoutRegion>();
        /// <summary>The minimum width for showing regions side by side.</summary>
        public int NarrowBelow { get; set; } = 80;
        /// <summary>The region shown in narrow mode when no region owns focus.</summary>
        public string? ActiveRegion { get; set; }
        /// <summary>The gap in cells between regions.</summary>
        public int Gap { get; set; }
        /// <summary>The configured slots.</summary>
        public IReadOnlyList<LayoutRegion> Regions => _regions.AsReadOnly();
        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _regions;
        /// <summary>Adds a stable named region.</summary>
        public LayoutRegions Add(string key, IWidget content) {
            if (_regions.Exists(r => r.Key == key)) { throw new ArgumentException("Region keys must be unique.", nameof(key)); }
            _regions.Add(new LayoutRegion(key, content)); ActiveRegion ??= key; return this;
        }
        /// <summary>Swaps two existing subtrees without reconstructing their controls.</summary>
        public void Swap(string first, string second) {
            LayoutRegion a = Find(first), b = Find(second);
            IWidget content = a.Content; a.Content = b.Content; b.Content = content;
        }
        /// <summary>Shows a region and focuses its first enabled input.</summary>
        public void Activate(string key) {
            LayoutRegion region = Find(key); ActiveRegion = key;
            foreach (InputControl input in FocusManager.Discover(region)) { if (!input.Disabled) { input.Focus(); break; } }
        }
        private LayoutRegion Find(string key) => _regions.Find(r => r.Key == key) ?? throw new ArgumentException("Unknown region: " + key, nameof(key));
        private IWidget Layout(int width) {
            if (_regions.Count == 0) { return new Rows(); }
            if (width < NarrowBelow) {
                InputControl? focus = InputArrangement.CurrentFocus;
                foreach (LayoutRegion region in _regions) {
                    foreach (InputControl input in FocusManager.Discover(region)) { if (input == focus) { ActiveRegion = region.Key; return region; } }
                }
                return _regions.Find(r => r.Key == ActiveRegion) ?? _regions[0];
            }
            var columns = new Columns { Gap = Gap, CollapseBelow = 0 };
            foreach (LayoutRegion region in _regions) { columns.Add(region, LayoutSize.Fraction()); }
            return columns;
        }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Layout(constraints.MaxWidth).Measure(constraints, context);
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Layout(region.Width).Render(region, context);
    }
}
