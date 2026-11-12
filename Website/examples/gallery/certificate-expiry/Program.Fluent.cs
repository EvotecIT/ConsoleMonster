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
var table = Ui.Table(certificates)
    .Column(row => row.Name)
    .Column(row => row.Expires)
    .Column(row => row.Days)
    .Column(row => row.Owner)
    .Column(row => row.Status, column => column.Presenter(CellPresenters.StatusWord))
    .Expand();
StatusList renewals = Ui.StatusList()
    .Add("vpn.example.test", StatusKind.Fail, "Network · renew before Monday")
    .Add("api.example.test", StatusKind.Warn, "Platform · schedule renewal this week");
Rows report = Ui.Rows().Gap(1).Content(rowsContent2 =>
{
    rowsContent2.Rule("[accent bold]Certificate renewals[/] [muted]· as of 2026-10-24[/]");
    rowsContent2.Facts(facts => facts.Add(1, "urgent", ThemeToken.Fail)
        .Add(1, "due soon", ThemeToken.Warn)
        .Add(2, "healthy", ThemeToken.Pass));
    rowsContent2.Add(table);
    rowsContent2.Panel(p => p.Title("Renewal policy").Content(panelContent =>
    {
        panelContent.Rows(r => r.Content(rowsContent =>
        {
            rowsContent.Add(renewals);
            rowsContent.Markup("[muted]Urgent: 7 days or fewer · due soon: 30 days or fewer[/]");
        }));
    }));
});

// Behaviour: print once; connect your own inventory reader before rendering.
report.Show();

record Certificate(string Name, string Expires, int Days, string Owner, string Status);
