using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

Server[] servers =
{
    new("FS01", "Warsaw", "File"),
    // <more>
    new("FS02", "Warsaw", "File"),
    new("WEB01", "Warsaw", "Web"),
    new("SQL01", "Warsaw", "Database"),
    new("FS11", "Berlin", "File"),
    new("WEB11", "Berlin", "Web"),
    new("PRN11", "Berlin", "Print", "offline since 09:12"),
    new("BKP21", "Dublin", "Backup"),
    new("SQL21", "Dublin", "Database"),
    new("LAB01", "Lab", "Test"),
    new("LAB02", "Lab", "Test", "in maintenance until Friday")
// </more>
};
SelectPrompt<Server> prompt = new SelectPrompt<Server>(
    "Target server",
    servers,
    s => s.Name,
    s => s.Site)
{
    Description = "Type to filter; offline servers show why they cannot be picked."
};
foreach (Choice choice in prompt.Choices)
{
    Server server = (Server)choice.Value!;
    choice.Badge = RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted));
    choice.DisabledReason = server.Offline;
}

Server target = prompt.Show();
Terminal.WriteMarkupLine("Connecting to [bold]" + target.Name + "[/]");

record Server(string Name, string Site, string Role, string? Offline = null);
