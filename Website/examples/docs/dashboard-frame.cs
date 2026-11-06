using ConsoleMonster;
using ConsoleMonster.Frames;
using ConsoleMonster.Widgets;

// A fictional estate of 48 servers in three sites; nothing is queried.
DashboardFrame frame = new DashboardFrame("Server estate");
frame.Header.Facts.Add("contoso.example");
frame.Header.Facts.Add("48 servers");
frame.Header.Facts.Add("3 sites");
frame.Header.States.Add(FrameState.Of(HealthState.Failing, 1, "failing"));
frame.Header.States.Add(FrameState.Of(HealthState.Warning, 3, "warning"));

// Meters print in priority order; a narrow terminal keeps the first ones.
frame.Meters.Add(Meter.Composition("Servers healthy", 44, 3, 1));
frame.Meters.Add(Meter.Ratio("Backups current", 46, 48));
frame.Meters.Add(Meter.Fact("Oldest patch", "41 days", "FS-02", HealthState.Warning));
frame.Meters.Add(Meter.Count("Open alerts", 5));

// Needs attention lists the problems first, worst first.
frame.Attention.Noun = "server";
frame.Attention.OutOf = 48;
AttentionItem disk = new AttentionItem(HealthState.Failing, "SQL-03", "Disk D: 97% full", 1);
disk.Values.Add("97%");
frame.Attention.Items.Add(disk);
AttentionItem patch = new AttentionItem(HealthState.Warning, "FS-02", "Patches 41 days old", 2);
patch.Values.Add("41 d");
frame.Attention.Items.Add(patch);
AttentionItem certificate = new AttentionItem(HealthState.Warning, "WEB-07",
    "Certificate expires in 9 days", 3);
certificate.Values.Add("9 d");
frame.Attention.Items.Add(certificate);

// Summary sections pack beside the attention list; detail sections print below.
FrameSection sites = new FrameSection(
    "Sites",
    context => new Table()
        .AddColumn("Site")
        .AddColumn("Servers")
        .AddColumn("Healthy")
        .AddValues("London", "22", "21")
        .AddValues("Amsterdam", "16", "15")
        .AddValues("New York", "10", "8"));
sites.Facts.Add("by location");
frame.Sections.Add(sites);
FrameSection disks = new FrameSection(
    "Disk use",
    context => new Distribution()
        .Add("under 50%", 31, HealthState.Healthy)
        .Add("50-80%", 13, HealthState.Healthy)
        .Add("80-90%", 3, HealthState.Warning)
        .Add("over 90%", 1, HealthState.Failing))
{
    MinWidth = 40
};
frame.Sections.Add(disks);
FrameSection backups = new FrameSection(
    "Backups",
    context => new Table()
        .AddColumn("Job")
        .AddColumn("Last run")
        .AddColumn("Result")
        .AddValues("Nightly SQL", "02:10", "OK")
        .AddValues("Weekly files", "Sun 01:00", "OK"))
{
    Placement = FramePlacement.Detail
};
frame.Sections.Add(backups);
frame.Legend.Add(HealthState.Healthy, "healthy");
frame.Legend.Add(HealthState.Warning, "warning");
frame.Legend.Add(HealthState.Failing, "failing");
frame.Footer.Counts.Add("48 servers");
frame.Footer.Hint = FrameHint.None;
Terminal.Write(frame);
