// Review certificate renewal deadlines with an owner and a concrete next action.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fixed dates and fictional certificates; no certificate store or private key is opened.
var certificates = new Certificate[]
{
    new("api.example.test", "2026-11-02", 9, "Platform", "Warn"),
    new("vpn.example.test", "2026-10-26", 2, "Network", "Fail"),
    new("packages.example.test", "2027-02-14", 113, "Build", "Pass"),
    new("portal.example.test", "2026-12-18", 55, "Web", "Pass")
};

// Layout: put urgent renewals beside the inventory, with thresholds visible.
var table = Table.For(certificates)
    .Column(row => row.Name)
    .Column(row => row.Expires)
    .Column(row => row.Days)
    .Column(row => row.Owner)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);
table.Expand = true;

StatusList renewals = new StatusList()
    .Add("vpn.example.test", StatusKind.Fail, "Network · renew before Monday")
    .Add("api.example.test", StatusKind.Warn, "Platform · schedule renewal this week");
Rows report = new Rows(
    new Rule("[bold]Certificate renewals[/] [muted]· as of 2026-10-24[/]"),
    new TextBlock("[fail]1 urgent[/]   [warn]1 due soon[/]   [pass]2 healthy[/]"),
    table,
    new Panel()
    {
        Title = "Renewal policy",
        Content = new Rows(
            renewals,
            new TextBlock("[muted]Urgent: 7 days or fewer · due soon: 30 days or fewer[/]"))
    })
{
    Gap = 1
};

// Behaviour: print once; connect your own inventory reader before rendering.
Terminal.Write(report);

record Certificate(string Name, string Expires, int Days, string Owner, string Status);
