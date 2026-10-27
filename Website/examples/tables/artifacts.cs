using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Relative dates are measured from a fixed time, so this example prints the same every day.
DateTimeOffset now = new DateTimeOffset(2026, 10, 16, 14, 30, 0, TimeSpan.FromHours(2));
const string Root = @"D:\Builds\ConsoleMonster\Packed\";
Table table = new Table()
{
    Title = "[bold]Build artifacts[/] [muted]· ConsoleMonster 2.0.0[/]",
    ShowTotals = true,
    MaxRows = 5,
    TotalRowCount = 1234,
    Presentation = new PresentationOptions
    {
        Now = now
    }
}.AddColumn("Artifact", c =>
{
    c.IsKey = true;
    c.Overflow = ColumnOverflow.Never;
}).AddColumn("Kind", c =>
{
    c.Priority = 3;
}).AddColumn("Size", c =>
{
    c.Priority = 9;
    c.Presenter = CellPresenters.Bytes;
    c.Aggregate = ColumnAggregate.Sum;
}).AddColumn("Built in", c =>
{
    c.Priority = 6;
    c.Presenter = CellPresenters.Duration;
    c.Aggregate = ColumnAggregate.Sum;
}).AddColumn("Created", c =>
{
    c.Priority = 4;
    c.Presenter = new DatePresenter(DateDisplay.Relative);
}).AddColumn("Path", c =>
{
    c.Priority = 1;
    c.Overflow = ColumnOverflow.TruncateMiddle;
    c.MinWidth = 20;
    c.MaxWidth = 40;
}).AddColumn("SHA256", c =>
{
    c.Priority = 0;
    c.Overflow = ColumnOverflow.Truncate;
    c.MaxWidth = 14;
});
table.AddValues("ConsoleMonster.2.0.0.nupkg", "NuGet", 1_482_113L, TimeSpan.FromSeconds(48.2),
    now.AddMinutes(-12), Root + "ConsoleMonster.2.0.0.nupkg", "9f2c4e81a7d03b6e");
// <more>
table.AddValues("ConsoleMonster.2.0.0.snupkg", "Symbols", 402_331L, TimeSpan.FromSeconds(3.1),
    now.AddMinutes(-12), Root + "ConsoleMonster.2.0.0.snupkg", "41ab09cd77e2f310");
table.AddValues("ConsoleMonster-2.0.0.zip", "Module", 2_904_576L, TimeSpan.FromSeconds(11.7),
    now.AddMinutes(-11), Root + "ConsoleMonster-2.0.0.zip", "c0ffee4251de9a07");
table.AddValues("ConsoleMonster.PowerShell.dll", "Binary", 188_416L, TimeSpan.FromSeconds(22.9),
    now.AddHours(-3), @"D:\Builds\ConsoleMonster\Release\net8.0\ConsoleMonster.PowerShell.dll",
    "77aa13f9b2c46e5d");
table.AddValues("en-US\\ConsoleMonster-help.xml", "Help", 96_210L, TimeSpan.FromSeconds(6.4),
    now.AddDays(-2), Root + @"en-US\ConsoleMonster-help.xml", "0be4d29c6a1f8873");
table.AddValues("Docs.zip", "Docs", 51_302L, TimeSpan.FromSeconds(1.2), now.AddDays(-9),
    Root + "Docs.zip", "5d6e7f8091a2b3c4");
// </more>
Terminal.Write(table);
