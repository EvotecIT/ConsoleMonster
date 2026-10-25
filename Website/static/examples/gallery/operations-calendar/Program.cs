// Review synthetic backups, change windows and certificate expiries by date.
using ConsoleMonster;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Calendar = ConsoleMonster.Widgets.Calendar;

// Data: fixed dates and fictional outcomes, without reading local services or stores.
var days = new[] {
    new CalendarMark(new DateTime(2026, 10, 2), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 10, 8), StatusKind.Info, "Change window"),
    new CalendarMark(new DateTime(2026, 10, 23), StatusKind.Warn, "Certificate expires"),
    new CalendarMark(new DateTime(2026, 10, 23), StatusKind.Pass, "Backup verified")
};

// Layout: one month with an ISO week gutter and a status legend.
Calendar calendar = new Calendar(2026, 10, days) { FirstDayOfWeek = DayOfWeek.Monday };

// Behaviour: print once; convert source timestamps to the business time zone before binding.
Rows report = new Rows(calendar);
foreach (var day in days) { report.Add(TextBlock.Plain(day.Date.ToString("dd", System.Globalization.CultureInfo.InvariantCulture) + "  " + day.Label)); }
Terminal.Write(report);
