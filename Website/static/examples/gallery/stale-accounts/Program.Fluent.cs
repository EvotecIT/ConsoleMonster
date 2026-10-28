// Find stale Active Directory accounts and see which passwords expire this month.
using System.Globalization;
using ConsoleMonster;
using ConsoleMonster.Dates;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional accounts and a fixed report date; no domain controller is queried.
var today = new DateTime(2026, 11, 2);
var accounts = new Account[]
{
    new("amy.lee", "Finance", new(2026, 10, 30), new(2026, 8, 20), new DateTime(2026, 11, 18)),
    new("ben.ortiz", "IT", new(2026, 11, 1), new(2026, 8, 6), new DateTime(2026, 11, 4)),
    new("svc-backup", "Service", new(2026, 11, 2), new(2025, 3, 1), null),
    new("cara.diaz", "HR", new(2026, 7, 12), new(2026, 5, 30), new DateTime(2026, 8, 28)),
    new("dan.reyes", "Sales", new(2026, 8, 25), new(2026, 8, 11), new DateTime(2026, 11, 9)),
    new("contractor-07", "External", new(2026, 5, 2), new(2026, 4, 20), new DateTime(2026, 7, 19)),
    new("eve.chen", "Marketing", new(2026, 10, 28), new(2026, 8, 14), new DateTime(2026, 11, 12)),
    new("finn.park", "IT", new(2026, 10, 31), new(2026, 8, 28), new DateTime(2026, 11, 26)),
    new("gia.rossi", "Finance", new(2026, 9, 1), new(2026, 8, 24), new DateTime(2026, 11, 22)),
    new("training-01", "Shared", new(2026, 2, 14), new(2025, 11, 3), null),
    new("hal.novak", "Legal", new(2026, 11, 2), new(2026, 8, 10), new DateTime(2026, 11, 8))
};

// Behaviour: stale means no logon for 60 days or a password older than a year.
var stale = new List<StaleRow>();
foreach (var account in accounts)
{
    int inactive = (today - account.LastLogon).Days;
    int age = (today - account.PasswordSet).Days;
    if (inactive > 60 || age > 365)
    {
        string status = inactive > 90 || age > 365 ? "Fail" : "Warn";
        stale.Add(new StaleRow(account.Name, account.Department,
            account.LastLogon.ToString("yyyy-MM-dd"), inactive, age, status));
    }
}

var expiring = accounts.Where(account => account.PasswordExpires >= today
    && account.PasswordExpires <= today.AddDays(30));
var marks = expiring.Select(account => new CalendarMark(account.PasswordExpires!.Value,
    account.PasswordExpires <= today.AddDays(7) ? StatusKind.Warn : StatusKind.Info,
    account.Name + " password"))
    .Append(new CalendarMark(
        new DateTime(2026, 11, 16),
        StatusKind.Fail,
        $"Disable {stale.Count} accounts"))
    .OrderBy(mark => mark.Date)
    .ToArray();

// Layout: the accounts to act on, then the month of expiries beside the dated list.
var table = Ui.Table(stale)
    .Column(row => row.Account)
    .Column(row => row.Department)
    .Column(row => row.LastLogon)
    .Column(row => row.Inactive)
    .Column(row => row.PasswordAge)
    .Column(row => row.Status, column => column.Presenter(CellPresenters.StatusWord))
    .Expand();
var calendar = Ui.Calendar()
    .Year(2026)
    .Month(11)
    .Marks(marks)
    .FirstDayOfWeek(DayOfWeek.Monday)
    .Today(today);
var upcoming = Ui.StatusList();
foreach (var mark in marks)
    upcoming.Add(mark.Date.ToString("ddd dd MMM", CultureInfo.GetCultureInfo("en-US")), mark.Status,
        mark.Label);
var report = Ui.Rows().Gap(1).Content(c3 =>
{
    c3.Rule("[bold]Account hygiene[/] [muted]· adatum.example.test · as of 2026-11-02[/]");
    c3.Add(table);
    c3.Columns(columns => columns.Content(c2 =>
    {
        c2.Panel(p => p.Title("November").Content(c =>
        {
            c.Add(calendar);
        }));
        c2.Panel(p => p.Title("Coming up").Content(c =>
        {
            c.Add(upcoming);
        }));
    }));
});

// Behaviour: print once; confirm each stale account with its manager before disabling it.
report.Show();

record Account(string Name, string Department, DateTime LastLogon, DateTime PasswordSet,
    DateTime? PasswordExpires);

record StaleRow(string Account, string Department, string LastLogon, int Inactive, int PasswordAge,
    string Status);
