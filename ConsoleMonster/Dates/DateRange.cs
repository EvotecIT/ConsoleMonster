using System;

namespace ConsoleMonster.Dates {
    /// <summary>One inclusive civil-date range. Dates have unspecified kind and midnight time; no time-zone conversion occurs.</summary>
    public sealed class DateRange : IEquatable<DateRange> {
        /// <summary>Creates an ordered, inclusive range.</summary>
        public DateRange(DateTime start, DateTime end) {
            Start = CalendarDate.Normalize(start); End = CalendarDate.Normalize(end);
            if (End < Start) { throw new ArgumentException("The range end precedes its start.", nameof(end)); }
        }
        /// <summary>The first included date.</summary>
        public DateTime Start { get; }
        /// <summary>The last included date.</summary>
        public DateTime End { get; }
        /// <inheritdoc />
        public bool Equals(DateRange? other) => other != null && Start == other.Start && End == other.End;
        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as DateRange);
        /// <inheritdoc />
        public override int GetHashCode() => Start.GetHashCode() * 397 ^ End.GetHashCode();
        /// <inheritdoc />
        public override string ToString() => Start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + " – " + End.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }
}
