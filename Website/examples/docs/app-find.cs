using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

(string Level, string Message)[] lines =
{
    ("info", "Deploy 4.12.2 started"),
    ("info", "web-01 drained"),
    ("warn", "web-01 health check timeout, retrying"),
    ("info", "web-01 updated"),
    ("warn", "web-02 health check timeout, retrying"),
    ("info", "web-02 updated"),
    ("pass", "Deploy complete")
};
LogViewport log = new LogViewport("deploy.log") { ShowElapsed = false, Height = 8 };
foreach ((string level, string message) in lines) { log.Write(message, level); }
ConsoleApp.Run(new Rows(new TextBlock("[bold]Deploy 4.12.2[/] [muted]· / finds text[/]"), log));
