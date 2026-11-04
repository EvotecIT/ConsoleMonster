using ConsoleMonster;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using Calendar = ConsoleMonster.Widgets.Calendar;

// Each date takes the most severe status of its events; today is outlined.
var calendar = new Calendar(2026, 11, new[] {
    new CalendarMark(new DateTime(2026, 11, 3), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 11, 10), StatusKind.Fail, "Backup failed"),
    new CalendarMark(new DateTime(2026, 11, 17), StatusKind.Pass, "Backup verified"),
    new CalendarMark(new DateTime(2026, 11, 19), StatusKind.Warn, "Certificate expires"),
    new CalendarMark(new DateTime(2026, 11, 24), StatusKind.Info, "Change window"),
    new CalendarMark(new DateTime(2026, 11, 27), StatusKind.Pending, "Restore drill")
}) {
    FirstDayOfWeek = DayOfWeek.Monday,
    Today = new DateTime(2026, 11, 18)
};
Terminal.Write(calendar);
