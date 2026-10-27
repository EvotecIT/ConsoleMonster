using System;
using System.Globalization;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Calendar display and ordinary-object member selectors shared with New-ConsoleCalendar.</summary>
    public sealed class ConsoleCalendarOptions {
        /// <summary>The Gregorian year.</summary>
        public int Year { get; set; }
        /// <summary>The month; required for Month view and omitted for Year view.</summary>
        public int Month { get; set; }
        /// <summary>Month or Year display.</summary>
        public CalendarView View { get; set; }
        /// <summary>Member containing a civil date.</summary>
        public string DateProperty { get; set; } = "Date";
        /// <summary>Member containing the semantic outcome.</summary>
        public string StatusProperty { get; set; } = "Status";
        /// <summary>Member containing the visible description.</summary>
        public string LabelProperty { get; set; } = "Label";
        /// <summary>Override for the culture's first weekday.</summary>
        public DayOfWeek? FirstDayOfWeek { get; set; }
        /// <summary>Culture for month and weekday labels.</summary>
        public CultureInfo? Culture { get; set; }
        /// <summary>The civil date to emphasize as today.</summary>
        public DateTime? Today { get; set; }
        /// <summary>Shows the semantic outcome legend.</summary>
        public bool ShowLegend { get; set; } = true;
    }
}
