using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A focusable, capped log window over the shared LogRegion renderer. Writers never accumulate an unbounded UI queue.</summary>
    public sealed class LogViewport : InputControl {
        private readonly LogRegion _log = new LogRegion();
        private int _capacity = 1000;
        private int _horizontalOffset;
        /// <summary>Creates a named log viewport.</summary>
        public LogViewport(string key) : base(key) { }
        /// <summary>Maximum retained entries.</summary>
        public int Capacity {
            get => _capacity;
            set { if (value < 1 || value > 100000) { throw new ArgumentOutOfRangeException(nameof(value)); } Change(() => { _capacity = value; _log.Capacity = value; }); }
        }
        /// <summary>Preferred visible content rows.</summary>
        public int Height { get; set; } = 8;
        /// <summary>Whether to show entry elapsed time.</summary>
        public bool ShowElapsed { get => _log.ShowElapsed; set => _log.ShowElapsed = value; }
        /// <summary>Rows skipped from the newest end; zero follows new output.</summary>
        public int ScrollOffset { get => Read(() => _log.ScrollOffset); set => Change(() => _log.ScrollOffset = Math.Max(0, Math.Min(Math.Max(0, _log.Entries.Count - 1), value))); }
        /// <summary>Horizontal scroll in display cells, bounded by the maximum retained message length.</summary>
        public int HorizontalOffset { get => Read(() => _horizontalOffset); set => Change(() => _horizontalOffset = Math.Max(0, Math.Min(32768, value))); }
        /// <summary>Returns retained literal messages for an app's copy or export action.</summary>
        public string CopyText() => Read(() => string.Join(System.Environment.NewLine, _log.Entries.Select(entry => entry.Text)));
        /// <summary>A detached entry snapshot.</summary>
        public IReadOnlyList<LogEntry> Entries => Read(() => (IReadOnlyList<LogEntry>)new List<LogEntry>(_log.Entries));
        /// <summary>Appends bounded literal text from any thread. Markup is never interpreted.</summary>
        public void Write(string text, string level = "info", TimeSpan time = default) => Change(() => {
            text = text ?? string.Empty;
            if (text.Length > 16384) { text = text.Substring(0, 16384); }
            int before = _log.Entries.Count;
            _log.Write(text, level, time);
            if (_log.ScrollOffset > 0) { _log.ScrollOffset = Math.Min(_log.Entries.Count - 1, _log.ScrollOffset + Math.Max(1, _log.Entries.Count - before)); }
        });
        /// <inheritdoc />
        protected override object ReadValueCore() => _log.ScrollOffset;
        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (input.Is(ConsoleKey.UpArrow)) { Scroll(1); }
            else if (input.Is(ConsoleKey.DownArrow)) { Scroll(-1); }
            else if (input.Is(ConsoleKey.PageUp)) { Scroll(Math.Max(1, Height - 1)); }
            else if (input.Is(ConsoleKey.PageDown)) { Scroll(-Math.Max(1, Height - 1)); }
            else if (input.Is(ConsoleKey.Home)) { _log.ScrollOffset = Math.Max(0, _log.Entries.Count - 1); }
            else if (input.Is(ConsoleKey.End)) { _log.ScrollOffset = 0; }
            else if (input.Is(ConsoleKey.LeftArrow)) { _horizontalOffset = Math.Max(0, _horizontalOffset - 4); }
            else if (input.Is(ConsoleKey.RightArrow)) { _horizontalOffset = Math.Min(32768, _horizontalOffset + 4); }
            else { return false; }
            return true;
        }
        private void Scroll(int delta) => _log.ScrollOffset = Math.Max(0, Math.Min(Math.Max(0, _log.Entries.Count - 1), _log.ScrollOffset + delta));
        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            if (input.MouseAction != MouseAction.Wheel) { return false; }
            Scroll(Math.Sign(input.WheelDelta) * 3); return true;
        }
        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(Math.Max(1, Height), constraints.MaxHeight));
        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (_horizontalOffset == 0) { _log.Render(region, context); return; }
            var content = new CellBuffer(region.Width + _horizontalOffset, region.Height);
            _log.Render(new Region(content), context);
            region.Buffer.Blit(content, _horizontalOffset, 0, region.Width, region.Height, region.Bounds.X, region.Bounds.Y);
        }
    }
}
