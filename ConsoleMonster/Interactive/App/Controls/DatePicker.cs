using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Dates;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Interactive {
    /// <summary>A persistent Gregorian date or inclusive range editor, sharing the calendar's marks, ISO weeks and month geometry.</summary>
    public sealed class DatePicker : InputControl {
        private DateTime _cursor;
        private DateTime? _anchor;
        private object? _value;
        private Calendar? _calendar;
        private Rect _calendarBounds;
        private bool _dragging;
        private TimeSpan _lastEdgeNavigation = TimeSpan.MinValue;
        private readonly System.Diagnostics.Stopwatch _navigationClock = System.Diagnostics.Stopwatch.StartNew();
        /// <summary>Creates a picker with an injectable initial cursor.</summary>
        public DatePicker(string key, DateTime? today = null) : base(key) { Today = CalendarDate.Normalize(today ?? DateTime.Today); _cursor = Today; }
        /// <summary>Whether the committed value is a DateRange instead of a DateTime.</summary>
        public bool Range { get; set; }
        /// <summary>The committed civil date or DateRange; null means uncommitted. A scalar in range mode means Start=End.</summary>
        public object? Value {
            get => Read(() => _value);
            set => Change(() => {
                if (value == null) { _value = null; return; }
                object normalized = value is DateRange range ? (object)range : value is DateTime date ? CalendarDate.Normalize(date) : throw new ArgumentException("Use a DateTime or DateRange.", nameof(value));
                if (!Range && normalized is DateRange) { throw new ArgumentException("DateRange requires Range.", nameof(value)); }
                if (Range && normalized is DateTime scalar) { normalized = new DateRange(scalar, scalar); }
                string? error = Check(normalized);
                if (error != null) { throw new ArgumentException(error, nameof(value)); }
                _value = normalized; _cursor = normalized is DateRange dates ? dates.Start : (DateTime)normalized;
            });
        }
        /// <summary>The date with keyboard focus, independent of the committed value.</summary>
        public DateTime Cursor => Read(() => _cursor);
        /// <summary>The earliest committable date.</summary>
        public DateTime? Minimum { get; set; }
        /// <summary>The latest committable date.</summary>
        public DateTime? Maximum { get; set; }
        /// <summary>Returns true for a date that cannot be committed. Keep this callback short.</summary>
        public Func<DateTime, bool>? Unavailable { get; set; }
        /// <summary>The display culture; arithmetic remains Gregorian.</summary>
        public CultureInfo? Culture { get; set; }
        /// <summary>Overrides the culture's first weekday.</summary>
        public DayOfWeek? FirstDayOfWeek { get; set; }
        /// <summary>Today emphasis and deterministic initial cursor.</summary>
        public DateTime Today { get; set; }
        /// <summary>The shared immutable status/label model.</summary>
        public CalendarData Data { get; set; } = new CalendarData(null);

        internal string? Check(object value) {
            DateTime start = value is DateRange range ? range.Start : (DateTime)value;
            DateTime end = value is DateRange dates ? dates.End : start;
            if (Minimum.HasValue && Maximum.HasValue && CalendarDate.Normalize(Minimum.Value) > CalendarDate.Normalize(Maximum.Value)) { return "Minimum must not exceed Maximum."; }
            if (Minimum.HasValue && start < CalendarDate.Normalize(Minimum.Value)) { return "Date is before the minimum."; }
            if (Maximum.HasValue && end > CalendarDate.Normalize(Maximum.Value)) { return "Date is after the maximum."; }
            if (Unavailable != null) {
                for (DateTime date = start; ; date = date.AddDays(1)) {
                    if (Unavailable(date)) { return "The selection contains an unavailable date."; }
                    if (date == end) { break; }
                }
            }
            return null;
        }
        private object Candidate() {
            if (!Range) { return _cursor; }
            DateTime anchor = _anchor ?? _cursor;
            return new DateRange(anchor < _cursor ? anchor : _cursor, anchor < _cursor ? _cursor : anchor);
        }
        private bool Commit() {
            object candidate = Candidate(); ValidationMessage = Check(candidate);
            if (ValidationMessage != null) { return false; }
            _value = candidate; _anchor = null; return true;
        }
        /// <inheritdoc />
        protected override object? ReadValueCore() => _value;
        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            ValidationMessage = null;
            bool extend = Range && (input.Modifiers & KeyModifiers.Shift) != 0;
            if (extend && !_anchor.HasValue) { _anchor = _cursor; }
            try {
                if (input.Key == ConsoleKey.LeftArrow) { _cursor = _cursor.AddDays(-1); }
                else if (input.Key == ConsoleKey.RightArrow) { _cursor = _cursor.AddDays(1); }
                else if (input.Key == ConsoleKey.UpArrow) { _cursor = _cursor.AddDays(-7); }
                else if (input.Key == ConsoleKey.DownArrow) { _cursor = _cursor.AddDays(7); }
                else if (input.Key == ConsoleKey.PageUp) { _cursor = (input.Modifiers & KeyModifiers.Control) != 0 ? _cursor.AddYears(-1) : _cursor.AddMonths(-1); }
                else if (input.Key == ConsoleKey.PageDown) { _cursor = (input.Modifiers & KeyModifiers.Control) != 0 ? _cursor.AddYears(1) : _cursor.AddMonths(1); }
                else if (input.Key == ConsoleKey.Home || input.Key == ConsoleKey.End) {
                    DayOfWeek first = FirstDayOfWeek ?? (Culture ?? context.Culture).DateTimeFormat.FirstDayOfWeek;
                    int offset = ((int)_cursor.DayOfWeek - (int)first + 7) % 7;
                    _cursor = _cursor.AddDays(input.Key == ConsoleKey.Home ? -offset : 6 - offset);
                } else if (input.Is(ConsoleKey.Spacebar) && Range) { if (_anchor.HasValue) { Commit(); } else { _anchor = _cursor; } }
                else if (input.Is(ConsoleKey.Enter)) { Commit(); }
                else if (input.Is(ConsoleKey.Escape) && _anchor.HasValue) { _anchor = null; }
                else { return false; }
                if (extend) { ValidationMessage = Check(Candidate()); }
                return true;
            } catch (ArgumentOutOfRangeException) { return true; }
        }
        internal override bool Takes(KeyChord chord, RenderContext context) {
            switch (chord.Key) {
                case ConsoleKey.LeftArrow: case ConsoleKey.RightArrow: case ConsoleKey.UpArrow: case ConsoleKey.DownArrow:
                case ConsoleKey.PageUp: case ConsoleKey.PageDown: case ConsoleKey.Home: case ConsoleKey.End: case ConsoleKey.Enter:
                    return true;
                case ConsoleKey.Spacebar: return Range;
                case ConsoleKey.Escape: return Read(() => _anchor.HasValue);
            }
            return false;
        }
        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            if (_calendar == null) { return false; }
            if (input.MouseAction == MouseAction.Wheel) {
                try { _cursor = _cursor.AddMonths(-Math.Sign(input.WheelDelta)); } catch (ArgumentOutOfRangeException) { }
                return true;
            }
            if (!_calendar.TryDateAt(input.Column - _calendarBounds.X, input.Row - _calendarBounds.Y, out DateTime date)) {
                if (_dragging && input.MouseAction == MouseAction.Move && (input.HeldButtons & 1) != 0) {
                    int direction = input.Row < _calendarBounds.Y ? -1 : input.Row >= _calendarBounds.Bottom ? 1 : 0;
                    if (direction != 0 && (_lastEdgeNavigation == TimeSpan.MinValue || _navigationClock.Elapsed - _lastEdgeNavigation >= TimeSpan.FromMilliseconds(250))) {
                        try { _cursor = _cursor.AddMonths(direction); } catch (ArgumentOutOfRangeException) { }
                        _lastEdgeNavigation = _navigationClock.Elapsed;
                    }
                    return true;
                }
                if (input.MouseAction == MouseAction.Up && _dragging) { _dragging = false; Commit(); return true; }
                return false;
            }
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                _cursor = date; _anchor = Range ? date : (DateTime?)null; _dragging = true; _lastEdgeNavigation = TimeSpan.MinValue;
                if (!Range) { Commit(); } return true;
            }
            if (_dragging && input.MouseAction == MouseAction.Move && (input.HeldButtons & 1) != 0) { _cursor = date; return true; }
            if (_dragging && input.MouseAction == MouseAction.Up) { _cursor = date; _dragging = false; Commit(); return true; }
            return false;
        }
        internal override void CancelPointer() { _dragging = false; }
        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(Math.Min(36, constraints.MaxWidth), Math.Min(11, constraints.MaxHeight));
        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            _calendarBounds = region.Bounds;
            DateRange? selection = _anchor.HasValue && Range ? (DateRange)Candidate() : _value as DateRange;
            _calendar = new Calendar(_cursor.Year, _cursor.Month, Data) { Culture = Culture ?? context.Culture, FirstDayOfWeek = FirstDayOfWeek, Today = Today, ShowLegend = false };
            _calendar.DayStyle = date => {
                if (date == _cursor && Focused) { return context.Style(ThemeToken.Focus).With(TextDecoration.Reverse | TextDecoration.Bold); }
                if (selection != null && date >= selection.Start && date <= selection.End || _value is DateTime selected && date == selected) { return context.Style(ThemeToken.Accent).With(TextDecoration.Underline | TextDecoration.Bold); }
                return null;
            };
            _calendar.Render(region, context);
            if (region.Height > 9) { region.Write(0, region.Height - 1, Text.TextWrapper.Truncate(Range ? "Space anchor · Enter select range" : "Enter select date", region.Width, context.Ellipsis), context.Style(ThemeToken.Muted)); }
        }
    }
}
