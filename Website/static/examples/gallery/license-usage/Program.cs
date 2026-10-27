// Compare purchased and assigned Microsoft 365 licenses and price the unused seats.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: a fictional tenant with illustrative monthly prices; no Graph or admin API is called.
var skus = new Sku[]
{
    new("Microsoft 365 E3", 450, 431, 36),
    new("Microsoft 365 E5", 120, 74, 57),
    new("Office 365 E1", 300, 288, 10),
    new("Exchange Online Plan 1", 80, 63, 4),
    new("Power BI Pro", 150, 121, 14),
    new("Visio Plan 2", 40, 12, 15),
    new("Project Plan 3", 25, 24, 30)
};

// Behaviour: 90% assigned is healthy, under 70% is waste worth reclaiming; waste is unused seats × price.
var usage = skus.Select(sku =>
{
    int percent = (int)Math.Round(100.0 * sku.Assigned / sku.Purchased);
    StatusKind status = percent >= 90 ? StatusKind.Pass : percent >= 70 ? StatusKind.Warn : StatusKind.Fail;
    int unused = sku.Purchased - sku.Assigned;
    return new Usage(sku.Name, sku.Purchased, sku.Assigned, unused, percent, unused * sku.Price, status);
}).ToArray();
var reclaim = usage.Where(sku => sku.Status != StatusKind.Pass).OrderByDescending(sku => sku.Waste).Take(3);
int unusedSeats = usage.Sum(sku => sku.Unused);
int monthlyWaste = usage.Sum(sku => sku.Waste);

// Layout: utilisation bars beside the biggest savings, then the full ledger.
var bars = new BarChart { Maximum = 100, Unit = "%" };
foreach (var sku in usage)
    bars.Add(sku.Sku, sku.Used, sku.Status.ToToken());
var savings = new StatusList();
foreach (var sku in reclaim)
    savings.Add(sku.Sku, sku.Status, $"{sku.Unused} unused · ${sku.Waste:N0}/mo");

var table = Table.For(usage)
    .Column(row => row.Sku)
    .Column(row => row.Purchased)
    .Column(row => row.Assigned)
    .Column(row => row.Unused)
    .Column("Used %", row => row.Used)
    .Column("Waste $", row => row.Waste)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);
table.Expand = true;

var report = new Rows(
    new Rule("[bold]Microsoft 365 licenses[/] [muted]· adatum.example.test · October 2026[/]"),
    new Columns(new Panel(bars, "Assigned of purchased"), new Panel(savings, "Reclaim first")),
    table,
    new TextBlock($"[warn]${monthlyWaste:N0} a month[/] pays for {unusedSeats} unassigned seats. [muted]Reclaim before the renewal date.[/]"))
{
    Gap = 1
};

// Behaviour: print once; review the reclaim list with each license owner before removing seats.
Terminal.Write(report);

record Sku(string Name, int Purchased, int Assigned, int Price);
record Usage(string Sku, int Purchased, int Assigned, int Unused, int Used, int Waste, StatusKind Status);
