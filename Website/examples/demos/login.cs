using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// The illustrative -Sample data from Example-LoginOverview.ps1, without its Linux collection commands.
var facts = new KeyValueList();
facts.Add("OS", "Ubuntu 24.04.3 LTS");
facts.Add("Kernel", "6.8.0-85-generic");
facts.Add("Uptime", "41 days 6 h 12 min");
facts.Add("Load", "0.42 / 0.61 / 0.58 (8 CPUs)");
facts.Add("Memory", "19.4 of 32.0 GB (61%)");
facts.Add("Last login", "from 198.51.100.24, Oct 19 22:41");
var health = new StatusList();
health.Add("Memory 61%", StatusKind.Pass);
health.Add("Disk / 71%", StatusKind.Pass);
health.Add("Disk /var 88%", StatusKind.Warn);
health.Add("Disk /home 34%", StatusKind.Pass);
health.Add("Disk /srv/data 93%", StatusKind.Fail);
health.Add("Failed: backup-sync.service", StatusKind.Fail);
health.Add("Failed: certbot-renew.timer", StatusKind.Fail);
health.Add("14 updates (3 security)", StatusKind.Warn);
health.Add("Reboot required", StatusKind.Warn);
var disks = new BarChart
{
    Unit = "%",
    Warning = 80,
    Critical = 90
};
disks.Add("/", 71);
disks.Add("/var", 88);
disks.Add("/home", 34);
disks.Add("/srv/data", 93);
Terminal.Write(new Rule("[bold]app-web-03[/] [muted]· " + new DateTime(2026, 10, 21, 12, 0, 0).ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) + "[/]"));
var profile = Terminal.GetCurrentProfile().WithSize(96, Terminal.GetCurrentProfile().Height);
Terminal.Write(new Columns(new Panel(facts, "System"), new Panel(health, "Health")) { Gap = 2 }, profile);
Terminal.Write(new Panel(disks, "Disk use"), profile);
