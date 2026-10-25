using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const int MonthHeight = 9;
        private static readonly StatusKind[] Statuses = { StatusKind.Fail, StatusKind.Warn, StatusKind.Running, StatusKind.Pending, StatusKind.Pass, StatusKind.Info, StatusKind.Skip };
        private static readonly string[] Symbols = { "x", "!", ">", "?", "+", "i", "-" };
        private readonly int _year;
        private readonly int _month;
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
        public int Year => _year;
        /// <summary>The month used in Month view.</summary>
        public int Month => _month;
        /// <summary>Month or year presentation.</summary>
        public CalendarView View { get; set; }
        /// <summary>The immutable grouped date model.</summary>
        public CalendarData Data { get; }
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

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int columns = Columns(constraints.MaxWidth);
            int rows = View == CalendarView.Year ? (12 + columns - 1) / columns : 1;
            int width = Math.Min(constraints.MaxWidth, columns * (MonthWidth + 2) - 2);
            return new Size(width, Math.Min(constraints.MaxHeight, rows * MonthHeight + (ShowLegend ? Legend(width).Count : 0)));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (!Enum.IsDefined(typeof(CalendarView), View)) { throw new InvalidOperationException("Unknown calendar view."); }
            int columns = Columns(region.Width);
            int count = View == CalendarView.Year ? 12 : 1;
            CultureInfo culture = Culture ?? context.Culture;
            DayOfWeek first = FirstDayOfWeek ?? culture.DateTimeFormat.FirstDayOfWeek;
            for (int index = 0; index < count; index++) {
                RenderMonth(region.Sub((index % columns) * (MonthWidth + 2), (index / columns) * MonthHeight, MonthWidth, MonthHeight),
                    View == CalendarView.Year ? index + 1 : Month, first, culture, context);
            }
            if (ShowLegend) {
                int y = ((count + columns - 1) / columns) * MonthHeight;
                foreach (string line in Legend(region.Width)) { region.Write(0, y++, line, context.Style(ThemeToken.Muted)); }
            }
        }

        private int Columns(int width) => View == CalendarView.Year ? Math.Max(1, (Math.Min(width, 112) + 2) / (MonthWidth + 2)) : 1;

        private void RenderMonth(Region region, int month, DayOfWeek first, CultureInfo culture, RenderContext context) {
            string title = GregorianMonthName(culture, month) + " " + Year.ToString(CultureInfo.InvariantCulture);
            region.Write(0, 0, title, context.Style(ThemeToken.Title));
            region.Write(0, 1, "ISO wk", context.Style(ThemeToken.Muted));
            for (int col = 0; col < 7; col++) {
                string label = culture.DateTimeFormat.GetShortestDayName((DayOfWeek)(((int)first + col) % 7));
                region.Write(8 + col * 4, 1, label, context.Style(ThemeToken.Header), 3);
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
                    region.Write(0, row + 2, weekYear.ToString("0000", CultureInfo.InvariantCulture) + "-" + week.ToString("00", CultureInfo.InvariantCulture), context.Style(ThemeToken.Muted));
                }
                for (int col = 0; col < 7; col++) {
                    int number = firstNumber + col;
                    if (number < 1 || number > days) { continue; }
                    StatusKind? status = Data.GetStatus(new DateTime(Year, month, number));
                    string text = number.ToString("00", CultureInfo.InvariantCulture) + (status.HasValue ? Symbol(status.Value) : " ");
                    region.Write(8 + col * 4, row + 2, text, context.Style(status.HasValue ? status.Value.ToToken() : ThemeToken.Text));
                }
            }
        }

        private List<string> Legend(int width) {
            List<string> lines = new List<string>();
            string current = string.Empty;
            foreach (StatusKind status in Statuses) {
                string item = Symbol(status) + " " + status;
                if (current.Length > 0 && current.Length + 2 + item.Length > width) { lines.Add(current); current = string.Empty; }
                current += (current.Length == 0 ? "" : "  ") + item;
            }
            if (current.Length > 0) { lines.Add(current); }
            return lines;
        }

        private static string Symbol(StatusKind status) => Symbols[Array.IndexOf(Statuses, status)];

        // Month numbers in this widget are Gregorian even when the culture's default calendar is not.
        private static string GregorianMonthName(CultureInfo culture, int month) {
            foreach (System.Globalization.Calendar calendar in culture.OptionalCalendars) {
                if (calendar is GregorianCalendar) {
                    DateTimeFormatInfo format = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
                    format.Calendar = calendar;
                    return format.GetMonthName(month);
                }
            }
            return CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
        }
    }
}
