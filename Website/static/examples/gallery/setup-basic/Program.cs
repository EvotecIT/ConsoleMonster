// Build a three-step installer preview with editable fields and buttons.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
string installFolder = "./toolbox-agent";
string[] components = { "Core", "Core + reports" };

// Layout: construct the widgets shown on screen.
int page = 0;
TextInput folder = new TextInput("Folder", installFolder)
{
    Label = "Install folder"
};
Select component = new Select("Component", components.Select(name => new Choice(name)))
{
    Label = "Component",
    Value = "Core"
};
Button back = new Button("Back", "Back");
Button next = new Button("Next", "Next / preview");
back.Clicked += (_, _) => page = Math.Max(0, page - 1);
next.Clicked += (_, _) => page = Math.Min(2, page + 1);
Rows layout = new Rows(
    TextBlock.FromMarkup("[accent bold]Install Toolbox Agent[/]"),
    folder,
    component,
    new Sized(new Columns()
        .Add(back, LayoutSize.Fixed(12))
        .Add(next, LayoutSize.Fixed(24)), LayoutSize.Fixed(1)),
    new AppBinding(state =>
    {
        string title = new[] { "1 / 3 · Location", "2 / 3 · Components", "3 / 3 · Review" }[page];
        IWidget content = page == 0
            ? new TextBlock("Choose a writable folder for the agent.")
            : page == 1
                ? new TextBlock("Core runs local jobs. Reports adds an HTML summary.")
                : new Rows(
                    TextBlock.FromMarkup("Folder: "
                        + ConsoleMonster.Text.Markup.Escape(state.Get<string>(
                            "Folder", ""))),
                    TextBlock.FromMarkup("Component: "
                        + ConsoleMonster.Text.Markup.Escape(state.Get<string>(
                            "Component", ""))),
                    TextBlock.FromMarkup(
                        "[pass]Ready to install. Review the folder and components.[/]"));
        return new Panel()
        {
            Title = title,
            Content = content
        };
    }))
{
    Gap = 1
};

// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);
