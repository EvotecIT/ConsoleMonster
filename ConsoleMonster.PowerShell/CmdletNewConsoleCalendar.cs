using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a month or year calendar with dated operational outcomes.</para>
    /// <para type="description">Returns a passive widget. Pipeline rows provide Date, Status and Label properties, with explicit property overrides. Duplicate dates retain every row; the displayed status uses Fail, Warn, Running, Pending, Pass, Info, Skip precedence. Dates retain calendar components without time-zone conversion. ISO week labels describe the Monday in each row, including with a Sunday-first display.</para>
    /// </summary>
    /// <example><para>Display backup outcomes for October.</para><code>$calendar = $backups | New-ConsoleCalendar -Year 2026 -Month 10; $calendar | Show-Console</code></example>
    /// <example><para>Build a year view with Monday-first weeks.</para><code>New-ConsoleCalendar -Year 2026 -View Year -FirstDayOfWeek Monday</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleCalendar")]
    [OutputType(typeof(Calendar))]
    public sealed class CmdletNewConsoleCalendar : PSCmdlet {
        private readonly List<CalendarMark> _marks = new List<CalendarMark>();
        /// <summary><para type="description">Gregorian year to display.</para></summary>
        [Parameter(Mandatory = true)] [ValidateRange(1, 9999)] public int Year { get; set; }
        /// <summary><para type="description">Gregorian month, required in Month view.</para></summary>
        [Parameter] [ValidateRange(1, 12)] public int Month { get; set; }
        /// <summary><para type="description">Month (default) or Year.</para></summary>
        [Parameter] public CalendarView View { get; set; }
        /// <summary><para type="description">Rows to mark, retaining their original values for day details.</para></summary>
        [Parameter(ValueFromPipeline = true)] public PSObject[]? InputObject { get; set; }
        /// <summary><para type="description">Date member (default Date). Convert timestamps to your business time zone before binding.</para></summary>
        [Parameter] [ValidateNotNullOrEmpty] public string DateProperty { get; set; } = "Date";
        /// <summary><para type="description">Semantic outcome member (default Status).</para></summary>
        [Parameter] [ValidateNotNullOrEmpty] public string StatusProperty { get; set; } = "Status";
        /// <summary><para type="description">Description member (default Label).</para></summary>
        [Parameter] [ValidateNotNullOrEmpty] public string LabelProperty { get; set; } = "Label";
        /// <summary><para type="description">Override the culture's first weekday.</para></summary>
        [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }
        /// <summary><para type="description">Culture for month and weekday labels and the default week start.</para></summary>
        [Parameter] public CultureInfo? Culture { get; set; }
        /// <summary><para type="description">Omit the status symbol legend.</para></summary>
        [Parameter] public SwitchParameter NoLegend { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject == null) { return; }
            foreach (PSObject row in InputObject) {
                object? value = row?.Properties[DateProperty]?.Value;
                if (value == null) { throw new ArgumentException("A calendar row needs a date in " + DateProperty + "."); }
                DateTime date;
                if (value is DateTimeOffset offset) { date = offset.DateTime; }
                else if (value is string text) {
                    if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)) {
                        throw new ArgumentException("A calendar date must be a date value or a valid invariant date string.");
                    }
                    date = parsed.DateTime;
                } else { date = (DateTime)LanguagePrimitives.ConvertTo(value, typeof(DateTime), CultureInfo.InvariantCulture); }
                string statusText = Convert.ToString(row!.Properties[StatusProperty]?.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!StatusKinds.TryParse(statusText, out StatusKind status)) { throw new ArgumentException("Unknown calendar status: " + statusText); }
                _marks.Add(new CalendarMark(date, status, Convert.ToString(row.Properties[LabelProperty]?.Value, CultureInfo.CurrentCulture), row));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (View == CalendarView.Month && Month == 0) { throw new ArgumentException("Month view requires -Month."); }
            if (View == CalendarView.Year && MyInvocation.BoundParameters.ContainsKey(nameof(Month))) { throw new ArgumentException("Use -Month for Month view or -View Year without -Month."); }
            WriteObject(new Calendar(Year, Month == 0 ? 1 : Month, _marks) { View = View, FirstDayOfWeek = FirstDayOfWeek, Culture = Culture, ShowLegend = !NoLegend });
        }
    }
}
