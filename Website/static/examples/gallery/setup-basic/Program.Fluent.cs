// Build a three-step installer preview with editable fields and buttons.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Data: fictional inputs kept in memory.
string installFolder = "./toolbox-agent";
string[] components =
{
    "Core",
    "Core + reports"
};
// Layout: construct the widgets shown on screen.
int page = 0;
TextInput folder = ((TextInput)Ui.TextInput().Key("Folder").Value(installFolder).Label("Install folder").Build());
Select component = ((Select)Ui.Select().Key("Component").AddChoices(components.Select(name => new Choice(name))).Label("Component").Value("Core").Build());
Button back = ((Button)Ui.Button().Key("Back").Label("Back").Build());
Button next = ((Button)Ui.Button().Key("Next").Label("Next / preview").Build());
back.Clicked += (_, _) => page = Math.Max(0, page - 1);
next.Clicked += (_, _) => page = Math.Min(2, page + 1);
Rows layout = Ui.Rows().Gap(1).Content(c2 =>
{
    c2.Markup("[bold]Install Toolbox Agent[/] [muted]· Toolbox Agent[/]");
    c2.Add(folder);
    c2.Add(component);
    c2.Add(back);
    c2.Add(next);
    c2.Binding(cmBinding => cmBinding.Configure(cmDefinition => cmDefinition.Content = state =>
    {
        string title = new[]
        {
            "1 / 3 · Location",
            "2 / 3 · Components",
            "3 / 3 · Review"
        }[page];
        IWidget content = page == 0 ? Ui.Markup("Choose a writable folder for the agent.") : page == 1 ? Ui.Markup("Core runs local jobs. Reports adds an HTML summary.") : Ui.Rows().Content(c =>
        {
            c.Markup("Folder: " + ConsoleMonster.Text.Markup.Escape(state.Get<string>("Folder", "")));
            c.Markup("Component: " + ConsoleMonster.Text.Markup.Escape(state.Get<string>("Component", "")));
            c.Markup("[pass]Ready to install. Review the folder and components.[/]");
        });
        return Ui.Panel().Title(title).Content(c =>
        {
            c.Add(content);
        });
    }));
});
// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);
