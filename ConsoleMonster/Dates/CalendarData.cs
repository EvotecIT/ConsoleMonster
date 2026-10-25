using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Dates {
    /// <summary>A snapshot of marked dates, retaining duplicate results and their labels.</summary>
    public sealed class CalendarData {
        private readonly Dictionary<DateTime, IReadOnlyList<CalendarMark>> _days = new Dictionary<DateTime, IReadOnlyList<CalendarMark>>();
        private static readonly IReadOnlyList<CalendarMark> Empty = Array.Empty<CalendarMark>();

        /// <summary>Groups the supplied marks by date. Later changes to the source collection do not change the snapshot.</summary>
        public CalendarData(IEnumerable<CalendarMark>? marks = null) {
            Dictionary<DateTime, List<CalendarMark>> groups = new Dictionary<DateTime, List<CalendarMark>>();
            if (marks != null) {
                foreach (CalendarMark mark in marks) {
                    if (mark == null) { throw new ArgumentException("A calendar mark cannot be null.", nameof(marks)); }
                    if (!groups.TryGetValue(mark.Date, out List<CalendarMark>? day)) { groups.Add(mark.Date, day = new List<CalendarMark>()); }
                    day.Add(mark);
                }
            }
            foreach (KeyValuePair<DateTime, List<CalendarMark>> group in groups) { _days.Add(group.Key, new ReadOnlyCollection<CalendarMark>(group.Value)); }
        }

        /// <summary>All results on a date, in source order. An unmarked date returns an empty collection.</summary>
        public IReadOnlyList<CalendarMark> GetMarks(DateTime date) => _days.TryGetValue(CalendarDate.Normalize(date), out IReadOnlyList<CalendarMark>? marks) ? marks : Empty;

        /// <summary>The day's highest-priority outcome: Fail, Warn, Running, Pending, Pass, Info, Skip. Unmarked days are neutral.</summary>
        public StatusKind? GetStatus(DateTime date) {
            IReadOnlyList<CalendarMark> marks = GetMarks(date);
            if (marks.Count == 0) { return null; }
            StatusKind status = marks[0].Status;
            for (int i = 1; i < marks.Count; i++) { if (Priority(marks[i].Status) < Priority(status)) { status = marks[i].Status; } }
            return status;
        }

        private static int Priority(StatusKind status) {
            switch (status) {
                case StatusKind.Fail: return 0;
                case StatusKind.Warn: return 1;
                case StatusKind.Running: return 2;
                case StatusKind.Pending: return 3;
                case StatusKind.Pass: return 4;
                case StatusKind.Info: return 5;
                default: return 6;
            }
        }
    }
}
