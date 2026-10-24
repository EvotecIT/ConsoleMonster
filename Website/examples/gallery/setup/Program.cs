// Build a three-step installer preview with editable fields and buttons.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
string installFolder = "./toolbox-agent";
string[] components = { "Core", "Core + reports" };

// Run options: redirected input selects a deterministic snapshot.
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));

// Layout: construct the widgets shown on screen.
int page = step;
TextInput folder = new TextInput("Folder", installFolder) { Label = "Install folder" };
Select component = new Select("Component", components.Select(name => new Choice(name))) {
    Label = "Component", Value = "Core"
};
Button back = new Button("Back", "Back");
Button next = new Button("Next", "Next / preview");
back.Clicked += (_, _) => page = Math.Max(0, page - 1);
next.Clicked += (_, _) => page = Math.Min(2, page + 1);
Rows layout = new Rows(new TextBlock("[bold]Install Toolbox Agent[/] [muted]· Toolbox Agent[/]"),
    folder, component, back, next, new AppBinding(state => {
        string title = new[] { "1 / 3 · Location", "2 / 3 · Components", "3 / 3 · Review" }[page];
        IWidget content = page == 0 ? new TextBlock("Choose a writable folder for the agent.")
            : page == 1 ? new TextBlock("Core runs local jobs. Reports adds an HTML summary.")
            : new Rows(new TextBlock("Folder: " + ConsoleMonster.Text.Markup.Escape(state.Get<string>("Folder", ""))),
                new TextBlock("Component: " + ConsoleMonster.Text.Markup.Escape(state.Get<string>("Component", ""))),
                new TextBlock("[pass]Ready to install. Review the folder and components.[/]"));
        return new Panel(content, title);
    })) { Gap = 1 };
// Live updates: ConsoleApp handles input and reevaluates AppBinding as state changes.
// Escape exits; snapshot mode prints the selected state without reading keys.
ConsoleApp.Run(layout, options: new ConsoleAppOptions { StaticSnapshot = snapshot });
