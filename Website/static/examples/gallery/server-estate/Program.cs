// Summarize a server estate in one dashboard frame: facts, meters, problems first, then the details.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Frames;
using ConsoleMonster.Widgets;

// All values are synthetic. This example never connects to a host.
// Data: fictional inputs kept in memory.
string[][] sites =
{
    new[] { "London", "22", "21" },
    new[] { "Amsterdam", "16", "15" },
    new[] { "New York", "10", "8" }
};
string[][] backups =
{
    new[] { "Nightly SQL", "02:10", "84 GB" },
    new[] { "Weekly files", "Sun 01:00", "312 GB" }
};

// Layout: the frame places each part for the terminal's width.
DashboardFrame frame = new DashboardFrame("Server estate");
frame.Header.Facts.Add("contoso.example");
frame.Header.Facts.Add("48 servers");
frame.Header.Facts.Add("3 sites");
frame.Header.States.Add(FrameState.Of(HealthState.Failing, 1, "failing"));
frame.Header.States.Add(FrameState.Of(HealthState.Warning, 3, "warning"));
frame.Meters.Add(Meter.Composition("Servers healthy", 44, 3, 1));
frame.Meters.Add(Meter.Ratio("Backups current", 46, 48));
frame.Meters.Add(Meter.Fact("Oldest patch", "41 days", "FS-02", HealthState.Warning));
frame.Meters.Add(Meter.Count("Open alerts", 5));
frame.Attention.Noun = "server";
frame.Attention.OutOf = 48;
(HealthState State, string Identity, string Reason, string Value)[] problems =
{
    (HealthState.Failing, "SQL-03", "Disk D: 97% full", "97%"),
    (HealthState.Warning, "FS-02", "Patches 41 days old", "41 d"),
    (HealthState.Warning, "WEB-07", "Certificate expires in 9 days", "9 d")
};
for (int i = 0; i < problems.Length; i++)
{
    AttentionItem item = new AttentionItem(problems[i].State, problems[i].Identity,
        problems[i].Reason, i + 1.0);
    item.Values.Add(problems[i].Value);
    frame.Attention.Items.Add(item);
}
Table siteTable = new Table().AddColumn("Site").AddColumn("Servers").AddColumn("Healthy");
foreach (string[] site in sites) { siteTable.AddValues(site); }
FrameSection siteSection = new FrameSection("Sites", context => siteTable);
siteSection.Facts.Add("by location");
frame.Sections.Add(siteSection);
Table backupTable = new Table().AddColumn("Job").AddColumn("Last run").AddColumn("Size");
foreach (string[] backup in backups) { backupTable.AddValues(backup); }
frame.Sections.Add(new FrameSection(
    "Backups",
    context => backupTable) { Placement = FramePlacement.Detail });
frame.Legend.Add(HealthState.Healthy, "healthy");
frame.Legend.Add(HealthState.Warning, "warning");
frame.Legend.Add(HealthState.Failing, "failing");
frame.Footer.Counts.Add("48 servers");
frame.Footer.Hint = FrameHint.None;

// Behaviour: print the frame once and return.
Terminal.Write(frame);
