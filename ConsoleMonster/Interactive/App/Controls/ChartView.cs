using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A chart you can explore inside an app. A crosshair follows ←/→ or a click and the row above the plot reads every
    /// shown series at that point (the app footer repeats it for screen readers). A number key or a click on a legend
    /// entry hides or shows a series; + and - or the wheel zoom along X, Shift+←/→ or a drag pans, and Home shows
    /// everything again. The value is the index of the point under the crosshair. Give a <see cref="DataGrid"/> the same
    /// <see cref="SelectionKey"/> to link the two through app state: selecting a row moves the crosshair to that point and
    /// moving the crosshair selects the row. Drawing the chart outside an app is unchanged.
    /// </summary>
    public sealed class ChartView : InputControl, ILinkedSelection {
        private readonly ChartPlot _plot;
        private readonly HashSet<int> _hidden = new HashSet<int>();
        private readonly List<string> _labels = new List<string>();
        private int _cursor = -1;
        private int _level;
        private double _pan;
        private int _height = 10;
        private ChartPlotLayout? _layout;
        private int? _linked;
        // A press on the plot (drag to pan, release in place to move the crosshair) or on a legend entry (release to toggle).
        private int _pressColumn = -1;
        private int _pressLegend = -1;
        private double _pressPan;
        private bool _dragged;

        /// <summary>Explores a chart: a <see cref="LineChart"/> (converted implicitly), or any other <see cref="ChartPlot"/>.</summary>
        /// <param name="key">The application-state key holding the index of the point under the crosshair.</param>
        /// <param name="chart">The chart; a <see cref="LineChart"/> keeps drawing the same outside an app.</param>
        public ChartView(string key, ChartPlot chart) : base(key) { _plot = chart ?? throw new ArgumentNullException(nameof(chart)); }

        /// <summary>The chart explored.</summary>
        public ChartPlot Chart => _plot;

        /// <summary>Rows for the readout and the plot, legend included (default 10).</summary>
        public int Height { get => Read(() => _height); set => Change(() => _height = Math.Max(4, value)); }

        /// <summary>Labels for the points along X (a time, a build), read out with their values; missing labels show the point number.</summary>
        public IList<string> Labels => _labels;

        /// <summary>The app-state key shared with linked controls such as a <see cref="DataGrid"/> (see <see cref="ILinkedSelection"/>): the point under the crosshair.</summary>
        public string? SelectionKey { get; set; }

        /// <summary>The index of the point under the crosshair, or -1 before there is data. Assignments notify like a click.</summary>
        public int Cursor { get => Read(CursorCore); set => Change(() => { _cursor = Clamp(value); Reveal(_cursor); }); }

        /// <summary>The zoom level: 0 shows the whole X range and each level shows a narrower window.</summary>
        public int ZoomLevel { get => Read(() => Math.Min(_level, _plot.Levels - 1)); set => Change(() => { _level = Math.Max(0, Math.Min(_plot.Levels - 1, value)); Center(CursorCore()); }); }

        /// <summary>Where the zoomed window sits: 0 at the start of the X range, 1 at its end.</summary>
        public double Pan { get => Read(() => _pan); set => Change(() => _pan = Math.Max(0, Math.Min(1, value))); }

        /// <summary>Whether a series (by its position, from 0) is hidden.</summary>
        /// <param name="series">The series position.</param>
        /// <returns>True when hidden.</returns>
        public bool IsHidden(int series) => Read(() => _hidden.Contains(series));

        /// <summary>Hides or shows a series by its position, from 0.</summary>
        /// <param name="series">The series position.</param>
        /// <param name="hidden">Whether to hide it.</param>
        public void SetHidden(int series, bool hidden) => Change(() => { if (_plot.CanHide(series)) { if (hidden) { _hidden.Add(series); } else { _hidden.Remove(series); } } });


        /// <inheritdoc />
        protected override object? ReadValueCore() => CursorCore();

        private int CursorCore() {
            int count = _plot.Count;
            if (count == 0) { return -1; }
            return _cursor < 0 || _cursor >= count ? count - 1 : _cursor;
        }

        private int Clamp(int index) => _plot.Count == 0 ? -1 : Math.Max(0, Math.Min(_plot.Count - 1, index));
        private int Level => Math.Max(0, Math.Min(_level, _plot.Levels - 1));

        // Keeps a point inside the window, panning as little as possible.
        private void Reveal(int index) {
            if (index < 0) { return; }
            double window = _plot.WindowFraction(Level);
            if (window >= 1) { return; }
            double start = _plot.WindowStart(Level, _pan), at = _plot.Position(index);
            if (at < start) { _pan = at / (1 - window); }
            else if (at > start + window) { _pan = (at - window) / (1 - window); }
            _pan = Math.Max(0, Math.Min(1, _pan));
        }

        private void Center(int index) {
            double window = _plot.WindowFraction(Level);
            _pan = window >= 1 || index < 0 ? 0 : Math.Max(0, Math.Min(1, (_plot.Position(index) - window / 2) / (1 - window)));
        }

        // Pans by a fraction of the window, then keeps the crosshair on a point that is still shown.
        private void PanBy(double windows) {
            double window = _plot.WindowFraction(Level);
            if (window >= 1) { return; }
            _pan = Math.Max(0, Math.Min(1, _pan + windows * window / (1 - window)));
            int cursor = CursorCore();
            if (cursor < 0) { return; }
            double start = _plot.WindowStart(Level, _pan), at = _plot.Position(cursor);
            if (at >= start - 1e-9 && at <= start + window + 1e-9) { return; }
            int best = cursor;
            double distance = double.MaxValue;
            for (int i = 0; i < _plot.Count; i++) {
                double p = _plot.Position(i);
                if (p < start - 1e-9 || p > start + window + 1e-9) { continue; }
                double d = Math.Abs(p - at);
                if (d < distance) { distance = d; best = i; }
            }
            _cursor = best;
        }

        private bool Zoom(int delta) {
            int level = Math.Max(0, Math.Min(_plot.Levels - 1, Level + delta));
            if (level == Level) { return false; }
            _level = level;
            Center(CursorCore());
            return true;
        }

        private bool Toggle(int series) {
            if (!_plot.CanHide(series)) { return false; }
            if (!_hidden.Remove(series)) { _hidden.Add(series); }
            return true;
        }

        private bool Move(int delta) {
            int cursor = CursorCore();
            if (cursor < 0) { return false; }
            _cursor = Clamp(cursor + delta);
            Reveal(_cursor);
            return true;
        }

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (input.Is(ConsoleKey.LeftArrow)) { return Move(-1); }
            if (input.Is(ConsoleKey.RightArrow)) { return Move(1); }
            if (input.Is(ConsoleKey.LeftArrow, KeyModifiers.Shift)) { PanBy(-0.25); return true; }
            if (input.Is(ConsoleKey.RightArrow, KeyModifiers.Shift)) { PanBy(0.25); return true; }
            if (input.Is(ConsoleKey.Home)) { _level = 0; _pan = 0; return true; }
            if (input.IsChar('+') || input.IsChar('=') || input.Is(ConsoleKey.Add)) { Zoom(1); return true; }
            if (input.IsChar('-') || input.Is(ConsoleKey.Subtract)) { Zoom(-1); return true; }
            for (int n = 1; n <= 9 && n <= _plot.SeriesCount; n++) {
                if (input.IsChar((char)('0' + n)) || input.Is(ConsoleKey.NumPad0 + n)) { return Toggle(n - 1); }
            }
            return false;
        }

        internal override bool Takes(KeyChord chord, RenderContext context) {
            if (chord.IsCharacter) {
                char c = chord.Char;
                return c == '+' || c == '=' || c == '-' || (c >= '1' && c <= '9' && Read(() => _plot.CanHide(c - '1')));
            }
            if (chord.Key == ConsoleKey.LeftArrow || chord.Key == ConsoleKey.RightArrow) { return chord.Modifiers == KeyModifiers.None || chord.Modifiers == KeyModifiers.Shift; }
            return chord.Modifiers == KeyModifiers.None && (chord.Key == ConsoleKey.Home || chord.Key == ConsoleKey.Add || chord.Key == ConsoleKey.Subtract);
        }

        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            PromptLabels labels = PromptLabels.For(context.Profile.Language);
            int series = Read(() => _plot.SeriesCount > 0 && _plot.CanHide(0) ? _plot.SeriesCount : 0);
            keys.Add(new KeyBinding(labels.KeyPoint, () => { }, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.RightArrow)) { KeyText = "←/→", Order = 1 });
            keys.Add(new KeyBinding(labels.KeyZoom, () => { }, new KeyChord('+'), new KeyChord('-')) { KeyText = "+/-", Order = 2 });
            keys.Add(new KeyBinding(labels.KeyPan, () => { }, new KeyChord(ConsoleKey.LeftArrow, KeyModifiers.Shift), new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Shift)) { KeyText = "shift+←/→", Order = 3 });
            if (series > 1) {
                List<KeyChord> digits = new List<KeyChord>();
                for (int n = 1; n <= 9 && n <= series; n++) { digits.Add(new KeyChord((char)('0' + n))); }
                keys.Add(new KeyBinding(labels.KeySeries, () => { }, digits.ToArray()) { KeyText = digits.Count == 1 ? "1" : "1-" + digits.Count.ToString(CultureInfo.InvariantCulture), Order = 4 });
            }
            keys.Add(new KeyBinding(labels.KeyResetView, () => { }, new KeyChord(ConsoleKey.Home)) { Order = 5 });
        }

        internal override bool TakesWheel => true;

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            ChartPlotLayout? layout = _layout;
            if (layout == null) { return false; }
            Rect plot = layout.Plot;
            bool inPlot = input.Column >= plot.X && input.Column < plot.Right && input.Row >= plot.Y && input.Row < plot.Bottom;
            if (input.MouseAction == MouseAction.Wheel) {
                if (!inPlot || input.WheelDelta == 0) { return false; }
                int? at = layout.IndexAt(input.Column);
                if (at.HasValue) { _cursor = Clamp(at.Value); }
                Zoom(input.WheelDelta > 0 ? 1 : -1);
                return true;
            }
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                _pressLegend = LegendAt(layout, input.Column, input.Row);
                _pressColumn = _pressLegend < 0 && inPlot ? input.Column : -1;
                _pressPan = _pan;
                _dragged = false;
                return _pressLegend >= 0 || _pressColumn >= 0;
            }
            if (input.MouseAction == MouseAction.Move && (input.HeldButtons & 1) != 0 && _pressColumn >= 0) {
                int moved = input.Column - _pressColumn;
                if (moved == 0 && !_dragged) { return false; }
                _dragged = true;
                double window = _plot.WindowFraction(Level);
                if (window < 1 && plot.Width > 0) { _pan = Math.Max(0, Math.Min(1, _pressPan - (double)moved / plot.Width * window / (1 - window))); }
                return true;
            }
            if (input.MouseAction == MouseAction.Up && input.MouseButton == 1) {
                bool handled = false;
                if (_pressLegend >= 0 && LegendAt(layout, input.Column, input.Row) == _pressLegend) { handled = Toggle(_pressLegend); }
                else if (_pressColumn >= 0 && !_dragged) {
                    int? at = inPlot ? layout.IndexAt(input.Column) : null;
                    if (at.HasValue) { _cursor = Clamp(at.Value); handled = true; }
                }
                else if (_dragged) { handled = true; }
                CancelPointer();
                return handled;
            }
            return false;
        }

        internal override void CancelPointer() { _pressColumn = -1; _pressLegend = -1; _dragged = false; }

        private static int LegendAt(ChartPlotLayout layout, int column, int row) {
            foreach (KeyValuePair<int, Rect> entry in layout.Legend) {
                Rect r = entry.Value;
                if (column >= r.X && column < r.Right && row >= r.Y && row < r.Bottom) { return entry.Key; }
            }
            return -1;
        }

        // The shared selection: the point index the user put the crosshair on, or the one another control selected.
        int? ILinkedSelection.SelectedIndex => Read(() => { int cursor = CursorCore(); return cursor < 0 ? _linked : cursor; });
        void ILinkedSelection.FollowSelection(int? index) => Change(() => {
            _linked = index;
            if (index.HasValue && index.Value >= 0 && index.Value < _plot.Count) { _cursor = index.Value; Reveal(_cursor); }
        });

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) {
            if (context.Profile.IsAccessible) {
                Size text = _plot.Measure(constraints.Height(constraints.IsHeightUnbounded ? Constraints.Unbounded : Math.Max(0, constraints.MaxHeight - 1)), context);
                return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, text.Height + 1));
            }
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, _height));
        }

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) { _layout = null; return; }
            int cursor = CursorCore();
            region.Write(0, 0, TextWrapper.Truncate(Readout(cursor, context, true), region.Width, context.Ellipsis));
            if (region.Height < 2) { _layout = null; return; }
            Region body = region.Sub(0, 1, region.Width, region.Height - 1);
            ChartPlotLayout layout = _plot.Render(body, context, Level, _pan, _hidden);
            _layout = layout;
            int? column = cursor < 0 ? null : layout.ColumnOf(cursor);
            if (column.HasValue && !context.Profile.IsAccessible) { DrawCrosshair(region.Buffer, layout.Plot, column.Value, context); }
        }

        // A vertical rule through the point: blank cells take the rule, drawn cells are reversed where colour allows, and
        // the series' own marks stay visible without colour.
        private static void DrawCrosshair(CellBuffer buffer, Rect plot, int column, RenderContext context) {
            Style rule = context.Style(ThemeToken.Accent);
            string glyph = context.Glyphs.GetBox(Glyphs.BoxStyle.Single).Vertical;
            for (int y = plot.Y; y < plot.Bottom; y++) {
                Cell cell = buffer[column, y];
                if (cell.IsContinuation) { continue; }
                if (string.IsNullOrEmpty(cell.Glyph) || cell.Glyph == " ") {
                    buffer.Set(column, y, glyph, cell.Style.WithColor(rule.Color));
                } else if (context.HasColor) {
                    buffer.SetStyle(column, y, cell.Style.With(TextDecoration.Reverse));
                }
            }
        }

        // The point and every shown series' value there; the plot row is coloured, the footer gets plain text.
        private StyledLine Readout(int cursor, RenderContext context, bool styled) {
            List<TextSpan> spans = new List<TextSpan>();
            Style muted = context.Style(ThemeToken.Muted);
            if (cursor < 0) {
                spans.Add(new TextSpan(NumberFormat.Missing(context.Glyphs.Tier), muted));
                return new StyledLine(spans);
            }
            spans.Add(new TextSpan(PointLabel(cursor, context), styled ? context.Style(ThemeToken.Text).With(TextDecoration.Bold) : Style.Plain));
            string separator = styled ? "  " : ", ";
            bool first = true;
            for (int series = 0; series < _plot.SeriesCount; series++) {
                if (_hidden.Contains(series)) { continue; }
                double? value = _plot.Value(series, cursor);
                string text = value.HasValue ? _plot.FormatValue(value.Value, context) : NumberFormat.Missing(context.Glyphs.Tier);
                spans.Add(new TextSpan(first ? (styled ? "  " : ": ") : separator, styled ? muted : Style.Plain));
                first = false;
                string name = Glyphs.SymbolFallback.Constrain(_plot.SeriesName(series), context.Glyphs.Tier);
                if (styled) {
                    if (_plot.SeriesCount > 1) { spans.Add(new TextSpan((series + 1).ToString(CultureInfo.InvariantCulture) + " ", _plot.SeriesStyle(series, context))); }
                    spans.Add(new TextSpan(name + " ", muted));
                    spans.Add(new TextSpan(text, context.Style(ThemeToken.Text)));
                } else {
                    spans.Add(new TextSpan(name + " " + text, Style.Plain));
                }
            }
            int level = Level;
            if (level > 0) {
                string zoom = (context.Glyphs.Tier == GlyphTier.Ascii ? "x" : "×") + _plot.Magnification(level).ToString("0.#", context.Culture);
                spans.Add(new TextSpan(styled ? "  " + zoom : ", " + zoom, styled ? muted : Style.Plain));
            }
            return new StyledLine(spans);
        }

        private string PointLabel(int index, RenderContext context) {
            if (index < _labels.Count && !string.IsNullOrEmpty(_labels[index])) { return Glyphs.SymbolFallback.Constrain(_labels[index], context.Glyphs.Tier); }
            return _plot.XLabel(index, context) ?? "#" + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        internal override StyledLine? FooterStatus(RenderContext context) => Read(() => Readout(CursorCore(), context, false));
    }
}
