using ConsoleMonster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Headless;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// The app under test: a list of servers and a line that follows the choice.
Rows layout = new Rows(
    new HeaderBar("Restart a server"),
    new Select("Server", new[] { new Choice("WEB01"), new Choice("SQL01"), new Choice("FS01") }),
    new AppBinding(state => TextBlock.Plain("Picked " + state.Get<string>("Server", "nothing"))));

// Run it on a headless terminal, drive it with a key and a click, and keep what it showed.
using HeadlessTerminal terminal = new HeadlessTerminal(56, 10);
Task<AppState> run = ConsoleApp.RunAsync(
    layout,
    default,
    new ConsoleAppOptions { Interactive = new InteractiveOptions { Target = terminal } });
terminal.WaitForText("WEB01", TimeSpan.FromSeconds(5));
terminal.Send("Down click:4,5");
terminal.WaitForText("Picked FS01", TimeSpan.FromSeconds(5));
CellBuffer screen = terminal.Snapshot();
terminal.Send("Escape");
await run;

// Print the kept screen: the same cells a real terminal would have shown.
Terminal.Write(new Panel(new KeptScreen(screen)) { Title = "headless 56×10" });

// Draws a kept screen cell by cell, so it prints like any other widget.
sealed class KeptScreen(CellBuffer cells) : IWidget
{
    public Size Measure(Constraints constraints,
        RenderContext context) => new Size(cells.Width, cells.Height);
    public void Render(Region region, RenderContext context)
    {
        for (int y = 0; y < Math.Min(cells.Height, region.Height); y++)
        {
            for (int x = 0; x < Math.Min(cells.Width, region.Width); x++)
            {
                Cell cell = cells[x, y];
                if (!cell.IsContinuation)
                {
                    region.Put(x, y, cell.Glyph ?? " ", cell.Style, cell.Width);
                }
            }
        }
    }
}
