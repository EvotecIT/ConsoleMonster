using ConsoleMonster;
using ConsoleMonster.Widgets;

LogRegion log = new LogRegion()
{
    PreferredHeight = 5
};
log.Write("Discovered 4 domain controllers in 2 domains", "info", TimeSpan.FromSeconds(2));
log.Write("DC02: replication partner DC05 last success 3h ago", "warn", TimeSpan.FromSeconds(31));
log.Write("DC02: USN rollback check failed (event 2095)", "err", TimeSpan.FromSeconds(44));
log.Write("Collected 186 rule inputs from DC01", "info", TimeSpan.FromSeconds(52));
log.Write("Using cached schema snapshot", "verbose", TimeSpan.FromSeconds(53));
Terminal.Write(log);
