using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.PowerShell {
    public static partial class ConsoleHostViews {
        /// <summary>Adapts ordinary PowerShell records into the core calendar's immutable marked-day model.</summary>
        /// <param name="inputObject">Rows retaining their source values for day details.</param>
        /// <param name="options">Period, display settings and member selectors.</param>
        /// <returns>A passive month or year calendar.</returns>
        public static Calendar Calendar(IEnumerable<object?> inputObject, ConsoleCalendarOptions options) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            ValidateCalendarOptions(options);
            var marks = new List<CalendarMark>();
            foreach (object? item in inputObject) { marks.Add(ReadCalendarMark(item, options)); }
            return BuildCalendar(new CalendarData(marks), options);
        }

        /// <summary>Displays a previously captured immutable calendar model with the same shared options.</summary>
        /// <param name="inputObject">The marked-day snapshot.</param>
        /// <param name="options">Period, display settings and member selectors.</param>
        /// <returns>A passive month or year calendar.</returns>
        public static Calendar Calendar(CalendarData inputObject, ConsoleCalendarOptions options) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            ValidateCalendarOptions(options);
            return BuildCalendar(inputObject, options);
        }

        private static Calendar BuildCalendar(CalendarData data, ConsoleCalendarOptions options) =>
            new Calendar(options.Year, options.Month == 0 ? 1 : options.Month, data) {
                View = options.View, FirstDayOfWeek = options.FirstDayOfWeek, Culture = options.Culture,
                ShowLegend = options.ShowLegend, Today = options.Today
            };

        private static void ValidateCalendarOptions(ConsoleCalendarOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.View == CalendarView.Month && options.Month == 0) { throw new ArgumentException("Month view requires Month.", nameof(options)); }
            if (options.View == CalendarView.Year && options.Month != 0) { throw new ArgumentException("Use Month for Month view or View Year without Month.", nameof(options)); }
            foreach (string member in new[] { options.DateProperty, options.StatusProperty, options.LabelProperty }) {
                if (string.IsNullOrWhiteSpace(member)) { throw new ArgumentException("Calendar property names must not be empty.", nameof(options)); }
            }
        }

        // Capture primitive fields before requesting another pipeline row; Source retains the caller's original row.
        internal static CalendarMark ReadCalendarMark(object? inputObject, ConsoleCalendarOptions options) {
            PSObject? row = inputObject == null ? null : PSObject.AsPSObject(inputObject);
            object? value = row?.Properties[options.DateProperty]?.Value;
            if (value == null) { throw new ArgumentException("A calendar row needs a date in " + options.DateProperty + ".", nameof(inputObject)); }
            DateTime date;
            if (value is DateTimeOffset offset) { date = offset.DateTime; }
            else if (value is string text) {
                if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)) {
                    throw new ArgumentException("A calendar date must be a date value or a valid invariant date string.", nameof(inputObject));
                }
                date = parsed.DateTime;
            } else { date = (DateTime)LanguagePrimitives.ConvertTo(value, typeof(DateTime), CultureInfo.InvariantCulture); }
            string statusText = Convert.ToString(row!.Properties[options.StatusProperty]?.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (!StatusKinds.TryParse(statusText, out StatusKind status)) { throw new ArgumentException("Unknown calendar status: " + statusText, nameof(inputObject)); }
            return new CalendarMark(date, status, Convert.ToString(row.Properties[options.LabelProperty]?.Value, CultureInfo.CurrentCulture), row);
        }
    }
}
