using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));

int page = step;
TextInput folder = new TextInput("Folder", "./toolbox-agent") { Label = "Install folder" };
Select component = new Select("Component", new[] { new Choice("Core"), new Choice("Core + reports") }) {
    Label = "Component", Value = "Core"
};
Button back = new Button("Back", "Back");
Button next = new Button("Next", "Next / preview");
back.Clicked += (_, _) => page = Math.Max(0, page - 1);
next.Clicked += (_, _) => page = Math.Min(2, page + 1);
Rows layout = new Rows(new TextBlock("[bold]Install Toolbox Agent[/] [muted]· preview only[/]"),
    folder, component, back, next, new AppBinding(state => {
        string title = new[] { "1 / 3 · Location", "2 / 3 · Components", "3 / 3 · Review" }[page];
        IWidget content = page == 0 ? new TextBlock("Choose a writable folder. No directory will be created.")
            : page == 1 ? new TextBlock("Core runs local jobs. Reports adds an HTML summary.")
            : new Rows(new TextBlock("Folder: " + ConsoleMonster.Text.Markup.Escape(state.Get<string>("Folder", ""))),
                new TextBlock("Component: " + ConsoleMonster.Text.Markup.Escape(state.Get<string>("Component", ""))),
                new TextBlock("[pass]Preview ready. Nothing has been installed.[/]"));
        return new Panel(content, title);
    })) { Gap = 1 };
ConsoleApp.Run(layout, options: new ConsoleAppOptions { StaticSnapshot = snapshot });
