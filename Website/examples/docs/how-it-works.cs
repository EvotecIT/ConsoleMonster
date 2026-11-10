using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Invented services; nothing is queried.
var services = new Service[]
{
    new("Web", "web01.example.com", "Pass"),
    new("Mail", "mail01.example.com", "Warn"),
    new("Files", "fs01.example.com", "Pass")
};

// Build: widgets are ordinary objects; containers take their children.
var table = Table.For(services)
    .Column(row => row.Name)
    .Column(row => row.Server)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);

Rows report = new Rows(
    new Rule("[accent bold]Service check[/]"),
    new ViewFacts().Add(2, "healthy", ThemeToken.Pass).Add(1, "degraded", ThemeToken.Warn),
    new Panel(table, "Services"))
{
    Gap = 1
};

// Render: print the finished widget once.
Terminal.Write(report);

record Service(string Name, string Server, string Status);
