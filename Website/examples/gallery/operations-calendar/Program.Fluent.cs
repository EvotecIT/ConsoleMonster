using ConsoleMonster;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: the same civil dates and fixed report clock as the PowerShell twin.
var today = new DateTime(2026, 10, 20);
var days = new[]
{
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
var calendar = Ui.Calendar().Year(2026).Month(10).Marks(days).FirstDayOfWeek(DayOfWeek.Monday).Today(today);
var upcoming = days.Where(day => day.Date >= today && day.Date <= today.AddDays(14))
    .GroupBy(day => day.Date)
    .OrderBy(group => group.Key);
// Layout: a month overview beside the next two weeks from the same rows.
var events = Ui.Rows();
foreach (var group in upcoming)
{
    events.Add(Ui.Text(group.Key.ToString(
                "ddd dd MMM",
                System.Globalization.CultureInfo.GetCultureInfo("en-US")
            )));
    var items = Ui.StatusList();
    foreach (var day in group)
    {
        items.Add(day.Label, day.Status);
    }

    events.Add(Ui.Padding().Left(2).Top(0).Right(0).Bottom(1).Content(c =>
    {
        c.Add(items);
    }));
}

Ui.Columns().Content(c2 =>
{
    c2.Panel(p => p.Title("Operations").Content(c =>
    {
        c.Add(calendar);
    }));
    c2.Panel(p => p.Title("Upcoming · next 14 days").Content(c =>
    {
        c.Add(events);
    }));
}).Show();
