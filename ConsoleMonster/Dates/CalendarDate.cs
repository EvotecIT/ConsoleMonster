using System;

namespace ConsoleMonster.Dates {
    /// <summary>Gregorian date arithmetic shared by calendar displays and date editors on every target framework.</summary>
    public static class CalendarDate {
        /// <summary>Preserves year, month and day; drops time and kind without a time-zone conversion.</summary>
        public static DateTime Normalize(DateTime date) => new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>Returns the ISO 8601 week number and its week-year, independently of the display culture.</summary>
        public static int IsoWeek(DateTime date, out int weekYear) {
            int weekday = ((int)date.DayOfWeek + 6) % 7 + 1;
            int week = (date.DayOfYear - weekday + 10) / 7;
            weekYear = date.Year;
            if (week < 1) { weekYear--; return WeeksInYear(weekYear); }
            if (week > WeeksInYear(weekYear)) { weekYear++; return 1; }
            return week;
        }

        private static int WeeksInYear(int year) {
            DayOfWeek first = new DateTime(year, 1, 1).DayOfWeek;
            return first == DayOfWeek.Thursday || (first == DayOfWeek.Wednesday && DateTime.IsLeapYear(year)) ? 53 : 52;
        }
    }
}
