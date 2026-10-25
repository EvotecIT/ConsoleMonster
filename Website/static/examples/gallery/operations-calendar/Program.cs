using ConsoleMonster;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Calendar = ConsoleMonster.Widgets.Calendar;

// Data: the same civil dates and fixed report clock as the PowerShell twin.
var today = new DateTime(2026, 10, 20);
var days = new[] {
    new CalendarMark(new DateTime(2026, 10, 2), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 10, 9), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 10, 16), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 10, 23), StatusKind.Warn, "Certificate expires"),
    new CalendarMark(new DateTime(2026, 10, 23), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 10, 28), StatusKind.Info, "Change window"),
    new CalendarMark(new DateTime(2026, 10, 14), StatusKind.Fail, "Backup verification failed"),
    new CalendarMark(new DateTime(2026, 10, 26), StatusKind.Pending, "Restore drill scheduled"),
    new CalendarMark(new DateTime(2026, 10, 30), StatusKind.Pass, "Backup verified"),
};

// Behaviour: group upcoming labels and use the shared status precedence.
var calendar = new Calendar(2026, 10, days) { FirstDayOfWeek = DayOfWeek.Monday, Today = today };
var upcoming = days.Where(day => day.Date >= today && day.Date <= today.AddDays(14)).GroupBy(day => day.Date).OrderBy(group => group.Key)
    .Select(group => new { Date = group.Key.ToString("ddd dd MMM", System.Globalization.CultureInfo.GetCultureInfo("en-US")), Status = calendar.Data.GetStatus(group.Key), Label = string.Join(" · ", group.Select(day => day.Label)) });

// Layout: a month overview beside the next two weeks from the same rows.
var events = new Rows();
foreach (var day in upcoming) {
    events.Add(new StatusList().Add(day.Date, day.Status!.Value));
    events.Add(new Padding(TextBlock.Plain(day.Label), 2, 0, 0, 1));
}
Terminal.Write(new Columns(new Panel(calendar, "Operations · October"), new Panel(events, "Upcoming · next 14 days")));
