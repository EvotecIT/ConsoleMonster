using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Server[] servers = {
    new("WEB01", "Web", "Warsaw"),
    new("SQL01", "Database", "Warsaw"),
    new("FS11", "File", "Berlin")
};

AppState state = new AppState();
Rows list = new Rows(new TextBlock("[muted]Type a number to open a server.[/]"));
for (int i = 0; i < servers.Length; i++) {
    Server row = servers[i];
    list.Add(new TextBlock($"[bold]{i + 1}[/]  {row.Name}  [muted]{row.Role}, {row.Site}[/]"));
}
AppScreens screens = new AppScreens();
screens.Add(new AppScreen("list", "Servers", list) {
    Bindings = {
        KeyBinding.Numbered("open", () => servers.Length, number => {
            state["Server"] = number;
            state["Screen"] = "server";
        })
    }
});
string[] owners = { "Operations", "Database team", "Help desk" };
Select owner = new Select("Owner", owners.Select(name => new Choice(name))) {
    Label = "Owner",
    Dropdown = true
};
AppBinding detail = new AppBinding(s => {
    Server server = servers[s.Get("Server", 1) - 1];
    return new Rows(new TextBlock(
        $"[bold]{server.Name}[/] is the {server.Role} server in {server.Site}."));
});
screens.Add(new AppScreen("server", "server", new Rows(detail, owner)) {
    TitleSource = s => servers[s.Get("Server", 1) - 1].Name
});

ConsoleApp.Run(new Rows(new HeaderBar("Inventory"), new Rows(screens).Fill()), state);

record Server(string Name, string Role, string Site);
