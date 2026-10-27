using System;
using System.Globalization;
using ConsoleMonster.Dates;

namespace ConsoleMonster.Interactive {
    /// <summary>Shared calendar data, civil-date bounds and culture for pickers and date questions.</summary>
    public sealed class DateSelectionOptions {
        /// <summary>Earliest permitted date.</summary>
        public DateTime? Minimum { get; set; }
        /// <summary>Latest permitted date.</summary>
        public DateTime? Maximum { get; set; }
        /// <summary>Rejects individual dates, including dates inside a range.</summary>
        public Func<DateTime, bool>? Unavailable { get; set; }
        /// <summary>Display and line-entry culture.</summary>
        public CultureInfo? Culture { get; set; }
        /// <summary>First weekday; null follows Culture.</summary>
        public DayOfWeek? FirstDayOfWeek { get; set; }
        /// <summary>Injectable civil date for today and the initial cursor.</summary>
        public DateTime Today { get; set; } = DateTime.Today;
        /// <summary>Grouped status marks and labels.</summary>
        public CalendarData Data { get; set; } = new CalendarData(null);
        /// <summary>Creates a persistent control over this shared configuration.</summary>
        public DatePicker CreatePicker(string key, bool range = false, object? value = null) => new DatePicker(key, Today) {
            Range = range, Minimum = Minimum, Maximum = Maximum, Unavailable = Unavailable,
            Culture = Culture, FirstDayOfWeek = FirstDayOfWeek, Data = Data, Value = value
        };
    }
}
