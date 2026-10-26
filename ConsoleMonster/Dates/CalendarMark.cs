using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Dates {
    /// <summary>A dated operational result. Dates retain their calendar components without time-zone conversion.</summary>
    public sealed class CalendarMark {
        /// <summary>Creates a marked day, retaining its optional source row for a future detail view.</summary>
        public CalendarMark(DateTime date, StatusKind status, string? label = null, object? source = null) {
            if (!Enum.IsDefined(typeof(StatusKind), status)) { throw new ArgumentOutOfRangeException(nameof(status)); }
            Date = CalendarDate.Normalize(date);
            Status = status;
            Label = label ?? string.Empty;
            Source = source;
        }
        /// <summary>The date at midnight, with unspecified kind.</summary>
        public DateTime Date { get; }
        /// <summary>The semantic outcome.</summary>
        public StatusKind Status { get; }
        /// <summary>The visible description.</summary>
        public string Label { get; }
        /// <summary>The caller's original row, when supplied.</summary>
        public object? Source { get; }
    }
}
