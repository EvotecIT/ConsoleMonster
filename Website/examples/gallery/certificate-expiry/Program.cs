// Review certificate renewal deadlines with an owner and a concrete next action.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fixed dates and fictional certificates; no certificate store or private key is opened.
var certificates = new[]
{
    new
    {
        Name = "api.example.test",
        Expires = "2026-11-02",
        Days = 9,
        Owner = "Platform",
        Status = "Warn"
    },
    new
    {
        Name = "vpn.example.test",
        Expires = "2026-10-26",
        Days = 2,
        Owner = "Network",
        Status = "Fail"
    },
    new
    {
        Name = "packages.example.test",
        Expires = "2027-02-14",
        Days = 113,
        Owner = "Build",
        Status = "Pass"
    },
    new
    {
        Name = "portal.example.test",
        Expires = "2026-12-18",
        Days = 55,
        Owner = "Web",
        Status = "Pass"
    }
};
// Layout: put urgent renewals beside the inventory, with thresholds visible.
Table table = new Table
{
    Expand = true
};
table.AddColumn("Name").AddColumn("Expires").AddColumn("Days").AddColumn("Owner").AddColumn("Status", column => column.Presenter = CellPresenters.StatusWord);
foreach (var certificate in certificates)
{
    table.AddRow(certificate.Name, certificate.Expires, TableCell.FromValue(certificate.Days), certificate.Owner, TableCell.FromValue(certificate.Status));
}

StatusList renewals = new StatusList().Add("vpn.example.test", StatusKind.Fail, "Network · renew before Monday").Add("api.example.test", StatusKind.Warn, "Platform · schedule renewal this week");
Rows report = new Rows(new Rule("[bold]Certificate renewals[/] [muted]· as of 2026-10-24[/]"), new TextBlock("[fail]1 urgent[/]   [warn]1 due soon[/]   [pass]2 healthy[/]"), table, new Panel() { Title = "Renewal policy", Content = new Rows(renewals, new TextBlock("[muted]Urgent: 7 days or fewer · due soon: 30 days or fewer[/]")) })
{
    Gap = 1
};
// Behaviour: print once; connect your own inventory reader before rendering.
Terminal.Write(report);
