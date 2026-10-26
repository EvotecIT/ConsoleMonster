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
    new CalendarMark(new DateTime(2026, 10, 23), StatusKind.Pending, "Backup scheduled"),
    new CalendarMark(new DateTime(2026, 10, 28), StatusKind.Info, "Change window"),
    new CalendarMark(new DateTime(2026, 10, 14), StatusKind.Fail, "Backup verification failed"),
    new CalendarMark(new DateTime(2026, 10, 26), StatusKind.Pending, "Restore drill scheduled"),
    new CalendarMark(new DateTime(2026, 10, 30), StatusKind.Pending, "Backup scheduled"),
};

// Behaviour: group dates while keeping each event's own status.
// Pin this English fixture's weekday labels: ICU and Windows NLS have different shortest names.
var calendarCulture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("en-US").Clone();
calendarCulture.DateTimeFormat.ShortestDayNames = new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
var calendar = new Calendar(2026, 10, days) { FirstDayOfWeek = DayOfWeek.Monday, Today = today, Culture = calendarCulture };
var upcoming = days.Where(day => day.Date >= today && day.Date <= today.AddDays(14)).GroupBy(day => day.Date).OrderBy(group => group.Key);

// Layout: a month overview beside the next two weeks from the same rows.
var events = new Rows();
foreach (var group in upcoming) {
    events.Add(TextBlock.Plain(group.Key.ToString("ddd dd MMM", System.Globalization.CultureInfo.GetCultureInfo("en-US"))));
    var items = new StatusList();
    foreach (var day in group) { items.Add(day.Label, day.Status); }
    events.Add(new Padding(items, 2, 0, 0, 1));
}
Terminal.Write(new Columns(new Panel(calendar, "Operations"), new Panel(events, "Upcoming · next 14 days")));
