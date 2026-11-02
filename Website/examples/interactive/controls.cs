using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// A fixed "today", so the calendar opens on the same month every time.
var dates = new DateSelectionOptions {
    Today = new DateTime(2026, 11, 18),
    FirstDayOfWeek = DayOfWeek.Monday
};
var schedule = new Rows(
    new Toggle("Enabled", true) { Label = "Run nightly checks" },
    dates.CreatePicker("Window", true));
var notify = new Rows(
    new Toggle("Email") { Label = "Email the report" },
    new Toggle("Teams") { Label = "Post to Teams" });
var tabs = new Tabs().Add("Schedule", schedule).Add("Notify", notify);

ConsoleApp.Run(new Rows(new HeaderBar("Maintenance"), tabs), new AppState());
