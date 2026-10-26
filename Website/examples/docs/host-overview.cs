using ConsoleMonster;
using ConsoleMonster.Overview;
using ConsoleMonster.Styling;

var snapshot = new OverviewSnapshot(
    "demo-host",
    "Example OS 1.0",
    new TimeSpan(3, 4, 5, 0),
    new DateTimeOffset(2026, 10, 22, 12, 34, 0, TimeSpan.FromHours(2)),
    new[]
    {
        new OverviewSection(
            "Resources",
            "CPU / memory",
            new[]
            {
                new OverviewItem("CPU", "8 logical CPUs", 24),
                new OverviewItem("Memory", "12 / 32 GiB", 37.5)
            }),
        new OverviewSection(
            "Disks",
            "Disks",
            new[]
            {
                new OverviewItem("/", "54 / 100 GiB", 54),
                new OverviewItem("/data", "82 / 100 GiB", 82),
                new OverviewItem("/archive", "91 / 100 GiB", 91)
            }),
        new OverviewSection(
            "Network",
            "Network",
            new[]
            {
                new OverviewItem("eth0", "192.0.2.10"),
                new OverviewItem("eth1", "2001:db8::10")
            }),
        new OverviewSection(
            "Services",
            "Key services",
            new[] { new OverviewItem("Status", "All 3 running", null, ThemeToken.Pass) }),
        new OverviewSection(
            "Updates",
            "Updates / reboot",
            new[]
            {
                new OverviewItem("Pending", "4 updates"),
                new OverviewItem("Reboot", "Reboot required", null, ThemeToken.Warn)
            }),
        new OverviewSection(
            "Security",
            "Security",
            new[]
            {
                new OverviewItem("Failed logons (24 h)", "2", null, ThemeToken.Warn),
                new OverviewItem("12:00", "demo-user"),
                new OverviewItem("11:00", "test-user"),
                new OverviewItem("10:00", "sample-user")
            })
    });
Terminal.Write(new OverviewWidget(snapshot));
