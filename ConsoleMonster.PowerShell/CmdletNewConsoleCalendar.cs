using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Dates;
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
        /// <summary><para type="description">Civil date to emphasize as today, useful with a fixed report clock.</para></summary>
        [Parameter] public DateTime? Today { get; set; }
        /// <summary><para type="description">Omit the status symbol legend.</para></summary>
        [Parameter] public SwitchParameter NoLegend { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject == null) { return; }
            ConsoleCalendarOptions options = Options();
            foreach (PSObject row in InputObject) { _marks.Add(ConsoleHostViews.ReadCalendarMark(row, options)); }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            WriteObject(ConsoleHostViews.Calendar(new CalendarData(_marks), Options()));
        }

        private ConsoleCalendarOptions Options() => new ConsoleCalendarOptions {
                Year = Year, Month = Month, View = View, DateProperty = DateProperty,
                StatusProperty = StatusProperty, LabelProperty = LabelProperty, FirstDayOfWeek = FirstDayOfWeek,
                Culture = Culture, Today = Today, ShowLegend = !NoLegend.IsPresent
        };
    }
}
