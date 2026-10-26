using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Dates;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>The calendar period to display.</summary>
    public enum CalendarView {
        /// <summary>One month.</summary>
        Month,
        /// <summary>Twelve months, wrapping to the available width.</summary>
        Year
    }

    /// <summary>A Gregorian month or year with dated outcomes, ISO weeks and a status legend. Never reads input.</summary>
    public sealed class Calendar : IWidget {
        private const int MonthWidth = 36;
        private const int MonthHeight = 10;
        private static readonly StatusKind[] Statuses = { StatusKind.Fail, StatusKind.Warn, StatusKind.Running, StatusKind.Pending, StatusKind.Pass, StatusKind.Info, StatusKind.Skip };
        private static readonly string[] Symbols = { "x", "!", ">", "?", "+", "i", "-" };
        private int _year;
        private int _month;
        private DayOfWeek? _firstDayOfWeek;

        /// <summary>Creates a calendar for an explicit month and a snapshot of its marked days.</summary>
        public Calendar(int year, int month, IEnumerable<CalendarMark>? marks = null) : this(year, month, new CalendarData(marks)) { }

        /// <summary>Creates a display over a shared date model, suitable for reuse by a picker.</summary>
        public Calendar(int year, int month, CalendarData data) {
            _ = new DateTime(year, month, 1);
            _year = year;
            _month = month;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>The Gregorian year.</summary>
        public int Year { get => _year; set { _ = new DateTime(value, _month, 1); _year = value; } }
        /// <summary>The month used in Month view.</summary>
        public int Month { get => _month; set { _ = new DateTime(_year, value, 1); _month = value; } }
        /// <summary>Month or year presentation.</summary>
        public CalendarView View { get; set; }
        /// <summary>The immutable grouped date model.</summary>
        public CalendarData Data { get; private set; }

        /// <summary>Replaces the marked-day snapshot before displaying the calendar.</summary>
        /// <param name="marks">The primary marked-day records.</param><returns>This calendar.</returns>
        public Calendar Marks(IEnumerable<CalendarMark> marks) { Data = new CalendarData(marks); return this; }
        /// <summary>Override for the culture's first day of week.</summary>
        public DayOfWeek? FirstDayOfWeek {
            get => _firstDayOfWeek;
            set {
                if (value.HasValue && !Enum.IsDefined(typeof(DayOfWeek), value.Value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _firstDayOfWeek = value;
            }
        }
        /// <summary>Explicit display culture; null uses the render context. Arithmetic and displayed years remain Gregorian.</summary>
        public CultureInfo? Culture { get; set; }
        /// <summary>Whether the legend follows the displayed period. Defaults to true.</summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>The civil date to emphasize as today; null leaves all dates neutral.</summary>
        public DateTime? Today { get; set; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int columns = Columns(constraints.MaxWidth);
            int rows = View == CalendarView.Year ? (12 + columns - 1) / columns : 1;
            int width = Math.Min(constraints.MaxWidth, columns * (MonthWidth + 2) - 2);
            return new Size(width, Math.Min(constraints.MaxHeight, rows * HeightPerMonth(context) + (ShowLegend ? Legend(width, context).Count : 0)));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (!Enum.IsDefined(typeof(CalendarView), View)) { throw new InvalidOperationException("Unknown calendar view."); }
            int columns = Columns(region.Width);
            int count = View == CalendarView.Year ? 12 : 1;
            CultureInfo culture = Culture ?? context.Culture;
            DayOfWeek first = FirstDayOfWeek ?? culture.DateTimeFormat.FirstDayOfWeek;
            int monthHeight = HeightPerMonth(context);
            for (int index = 0; index < count; index++) {
                RenderMonth(region.Sub((index % columns) * (MonthWidth + 2), (index / columns) * monthHeight, MonthWidth, monthHeight),
                    View == CalendarView.Year ? index + 1 : Month, first, culture, context);
            }
            if (ShowLegend) {
                int y = ((count + columns - 1) / columns) * monthHeight;
                foreach (string line in Legend(region.Width, context)) { region.Write(0, y++, line, context.Style(ThemeToken.Muted)); }
            }
        }

        private int HeightPerMonth(RenderContext context) {
            if (View == CalendarView.Year) { return MonthHeight; }
            DayOfWeek first = FirstDayOfWeek ?? (Culture ?? context.Culture).DateTimeFormat.FirstDayOfWeek;
            int offset = ((int)new DateTime(Year, Month, 1).DayOfWeek - (int)first + 7) % 7;
            return 3 + (offset + DateTime.DaysInMonth(Year, Month) + 6) / 7;
        }

        private int Columns(int width) => View == CalendarView.Year ? Math.Max(1, (Math.Min(width, 112) + 2) / (MonthWidth + 2)) : 1;

        private void RenderMonth(Region region, int month, DayOfWeek first, CultureInfo culture, RenderContext context) {
            string title = GregorianDateFormat(culture).GetMonthName(month) + " " + Year.ToString(CultureInfo.InvariantCulture);
            region.Write(0, 0, title, context.Style(ThemeToken.Title));
            region.Write(0, 2, "Week", context.Style(ThemeToken.Muted));
            for (int col = 0; col < 7; col++) {
                string label = culture.DateTimeFormat.GetShortestDayName((DayOfWeek)(((int)first + col) % 7));
                region.Write(8 + col * 4, 2, label, context.Style(ThemeToken.Header), 3);
            }
            DateTime start = new DateTime(Year, month, 1);
            int offset = ((int)start.DayOfWeek - (int)first + 7) % 7;
            int days = DateTime.DaysInMonth(Year, month);
            for (int row = 0; row < 6; row++) {
                int firstNumber = 1 - offset + row * 7;
                if (firstNumber > days) { break; }
                int monday = firstNumber + ((int)DayOfWeek.Monday - (int)first + 7) % 7;
                // Only the week label may refer to an adjacent month; avoid crossing DateTime's supported limits.
                long ticks = start.Ticks + (monday - 1L) * TimeSpan.TicksPerDay;
                if (ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks) {
                    int week = CalendarDate.IsoWeek(new DateTime(ticks), out int weekYear);
                    string weekLabel = (weekYear == Year ? "W" : weekYear.ToString("0000", CultureInfo.InvariantCulture) + "-") + week.ToString("00", CultureInfo.InvariantCulture);
                    region.Write(0, row + 3, weekLabel, context.Style(ThemeToken.Muted));
                }
                for (int col = 0; col < 7; col++) {
                    int number = firstNumber + col;
                    if (number < 1 || number > days) { continue; }
                    DateTime date = new DateTime(Year, month, number);
                    StatusKind? status = Data.GetStatus(date);
                    bool weekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                    Style style = context.Style(weekend ? ThemeToken.Muted : ThemeToken.Text);
                    string marker = " ";
                    if (status.HasValue) {
                        style = context.Style(status.Value.ToToken());
                        if (context.HasColor && context.Glyphs.Tier != GlyphTier.Ascii) { style = style.With(TextDecoration.Reverse | TextDecoration.Bold); }
                        else { marker = Symbol(status.Value, context); }
                    }
                    if (Today.HasValue && Today.Value.Date == date) { style = style.With(TextDecoration.Underline | TextDecoration.Bold); if (!status.HasValue) { style = context.Style(ThemeToken.Focus).With(TextDecoration.Bold | TextDecoration.Underline); if (!context.HasColor || context.Glyphs.Tier == GlyphTier.Ascii) { marker = "@"; } } }
                    region.Write(8 + col * 4, row + 3, number.ToString("00", CultureInfo.InvariantCulture), style);
                    region.Write(10 + col * 4, row + 3, marker, context.Style(status.HasValue ? status.Value.ToToken() : ThemeToken.Text));
                }
            }
        }

        private List<string> Legend(int width, RenderContext context) {
            List<string> lines = new List<string>();
            string current = string.Empty;
            foreach (StatusKind status in Statuses) {
                if (!HasStatus(status)) { continue; }
                string item = Symbol(status, context) + " " + status;
                if (current.Length > 0 && ConsoleMonster.Text.DisplayWidth.Measure(current) + 2 + ConsoleMonster.Text.DisplayWidth.Measure(item) > width) { lines.Add(current); current = string.Empty; }
                current += (current.Length == 0 ? "" : "  ") + item;
            }
            if (Today.HasValue && Today.Value.Year == Year && (View == CalendarView.Year || Today.Value.Month == Month)) {
                string item = (context.HasColor && context.Glyphs.Tier != GlyphTier.Ascii ? "Today " : "@ Today ") + Today.Value.ToString("dd MMM", GregorianDateFormat(Culture ?? context.Culture));
                if (current.Length > 0 && ConsoleMonster.Text.DisplayWidth.Measure(current) + 2 + ConsoleMonster.Text.DisplayWidth.Measure(item) > width) { lines.Add(current); current = string.Empty; }
                current += (current.Length == 0 ? "" : "  ") + item;
            }
            if (current.Length > 0) { lines.Add(current); }
            return lines;
        }

        private bool HasStatus(StatusKind status) {
            int firstMonth = View == CalendarView.Year ? 1 : Month;
            int lastMonth = View == CalendarView.Year ? 12 : Month;
            for (int month = firstMonth; month <= lastMonth; month++) {
                for (int day = 1; day <= DateTime.DaysInMonth(Year, month); day++) {
                    if (Data.GetStatus(new DateTime(Year, month, day)) == status) { return true; }
                }
            }
            return false;
        }

        private static string Symbol(StatusKind status, RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? Symbols[Array.IndexOf(Statuses, status)] : context.Glyphs.StatusMarker(status);

        // The grid and Today legend stay Gregorian even when the culture's default calendar is not.
        private static DateTimeFormatInfo GregorianDateFormat(CultureInfo culture) {
            GregorianCalendar? selected = null;
            foreach (System.Globalization.Calendar calendar in culture.OptionalCalendars) {
                if (calendar is GregorianCalendar gregorian) {
                    if (selected == null) { selected = gregorian; }
                    // Classic .NET can list US English before the culture's localized Gregorian variant.
                    if (gregorian.CalendarType == GregorianCalendarTypes.Localized) { selected = gregorian; break; }
                }
            }
            if (selected == null) { return CultureInfo.InvariantCulture.DateTimeFormat; }
            DateTimeFormatInfo format = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
            format.Calendar = selected;
            return format;
        }
    }
}
