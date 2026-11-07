using ConsoleMonster;
using ConsoleMonster.Headless;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// The app under test: a list of servers and a line that follows the choice.
Rows layout = new Rows(
    new HeaderBar("Restart a server"),
    new Select("Server", new[] { new Choice("WEB01"), new Choice("SQL01"), new Choice("FS01") }),
    new AppBinding(state => TextBlock.Plain("Picked " + state.Get<string>("Server",
        string.Empty))));

// Run it on a headless terminal, drive it with a key and a click, and keep what it showed.
using HeadlessTerminal terminal = new HeadlessTerminal(56, 10);
Task<AppState> run = ConsoleApp.RunAsync(
    layout,
    default,
    new ConsoleAppOptions { Interactive = new InteractiveOptions { Target = terminal } });
terminal.WaitForText("WEB01", TimeSpan.FromSeconds(5));
terminal.Send("Down click:4,5");
terminal.WaitForText("Picked FS01", TimeSpan.FromSeconds(5));
string screen = string.Join(
    "\n",
    terminal.Snapshot().ToPlainText().Replace("\r\n", "\n").Split('\n')
        .Select(row => row.TrimEnd())).TrimEnd('\n');
terminal.Send("Escape");
await run;

// Print the kept screen; a test compares it instead.
Terminal.Write(new Panel(TextBlock.Plain(screen)) { Title = "headless 56×10" });
