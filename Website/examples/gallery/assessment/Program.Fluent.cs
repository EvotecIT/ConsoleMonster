// Forest assessment: twelve targets, six workers, 332 rules and a complete findings dashboard.
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Data: fictional targets and rules, generated entirely in memory. No network checks or files are written.
SyntheticRule[] DcRules = new SyntheticRule[]
{
    new("Domain controllers", "LDAP signing required",
        "LDAPServerIntegrity is 1 (signing not required)", "12 unsigned binds in the last 24h",
        1.0),
    new("Domain controllers", "LDAP channel binding", "LdapEnforceChannelBinding is 0",
        "channel binding set to 'when supported'", 1.0),
    new("Domain controllers", "SMB signing enforced", "RequireSecuritySignature is disabled",
        "SMB1 client still installed", 1.0),
    new("Domain controllers", "Print Spooler disabled", "Spooler service is running",
        "Spooler set to manual start", 1.0),
    new("Domain controllers", "LSA protection enabled", "RunAsPPL is not set",
        "RunAsPPL set without UEFI lock", 1.0),
    new("Domain controllers", "Credential Guard posture", "virtualization-based security is off",
        "Credential Guard configured but not running", 1.4),
    new("Domain controllers", "Secure Boot certificate rollout", "2023 CA certificate not applied",
        "update staged, reboot pending", 1.0),
    new("Domain controllers", "Windows Update compliance", "last cumulative update 94 days ago",
        "update installed, restart pending", 2.2),
    new("Domain controllers", "Operating system support",
        "Windows Server 2012 R2 is out of support", "support ends in 11 months", 1.0),
    new("Domain controllers", "NTLM audit configuration", "NTLM auditing is disabled",
        "audit enabled for incoming traffic only", 1.0),
    new("Replication", "Replication latency per partner",
        "no successful inbound replication from DC02 for 4h", "latency to DC05 is 42 minutes", 2.6),
    new("Replication", "USN rollback detection", "event 2095 logged (USN rollback suspected)",
        "invocation ID changed after restore", 1.8),
    new("Replication", "SYSVOL DFSR state", "DFSR state is 'In Error' (5)",
        "DFSR backlog of 37 files", 1.5),
    new("Replication", "Tombstone lifetime", "tombstone lifetime is 60 days",
        "tombstone lifetime not set explicitly", 1.0),
    new("Replication", "Lingering objects", "3 lingering objects in the configuration partition",
        "strict replication consistency is off", 2.0),
    new("DNS", "DNS forwarders consistency", "forwarders differ from the domain baseline",
        "one forwarder does not answer", 1.0),
    new("DNS", "DNS scavenging", "scavenging disabled on all zones",
        "no-refresh interval longer than 7 days", 1.0),
    new("DNS", "Root hints reachable", "4 of 13 root servers unreachable",
        "root hints file older than 2 years", 1.6),
    new("DNS", "DC locator SRV records", "_ldap._tcp.dc._msdcs record missing",
        "stale SRV record for a retired DC", 1.0),
    new("Time", "Time source hierarchy", "PDC emulator syncs from the local CMOS clock",
        "NTP server configured by IP address", 1.0),
    new("Time", "Clock skew", "clock skew of 7 minutes against the PDC emulator",
        "clock skew of 48 seconds", 1.0),
    new("Kerberos", "Kerberos AES-only encryption", "RC4 still allowed for Kerberos",
        "3 accounts without AES keys", 1.0),
    new("Kerberos", "krbtgt password age", "krbtgt password not changed for 812 days",
        "krbtgt password is 190 days old", 1.0),
    new("Kerberos", "Unconstrained delegation", "2 servers trusted for unconstrained delegation",
        "1 service account trusted for delegation", 1.0),
    new("Security", "Audit policy baseline", "advanced audit policy not applied",
        "logon auditing set to success only", 1.3),
    new("Security", "Event log sizes", "Security log is 20 MB and overwrites in 2 hours",
        "Directory Service log under 64 MB", 1.0),
    new("Security", "Local administrators", "4 unexpected members in BUILTIN\\Administrators",
        "nested group in BUILTIN\\Administrators", 1.0),
    new("Security", "Firewall profile", "domain firewall profile is disabled",
        "inbound rule allows any remote address", 1.0),
    new("Security", "Remote Desktop exposure", "RDP allowed without network level authentication",
        "RDP open to all internal subnets", 1.0),
    new("Security", "PowerShell logging", "script block logging is disabled",
        "transcription not configured", 1.0)
};
SyntheticRule[] DomainRules = new SyntheticRule[]
{
    new("Group Policy", "GPO permissions baseline", "Authenticated Users can edit 2 GPOs",
        "a GPO without Authenticated Users read", 2.4),
    new("Group Policy", "Orphaned GPO folders", "5 SYSVOL folders without a GPO object",
        "1 GPO object without a SYSVOL folder", 1.0),
    new("Group Policy", "Empty GPOs", "9 GPOs with no settings are linked", "3 unlinked empty GPOs",
        1.0),
    new("Group Policy", "LAPS policy", "no LAPS policy applies to member servers",
        "legacy LAPS still deployed alongside Windows LAPS", 1.0),
    new("Accounts", "Stale computer accounts", "214 computers inactive for over 180 days",
        "41 computers inactive for over 90 days", 1.8),
    new("Accounts", "Password never expires", "18 user accounts never expire their password",
        "2 service accounts never expire their password", 1.0),
    new("Accounts", "Privileged group membership", "Domain Admins has 14 members",
        "a disabled account is still in Enterprise Admins", 1.5),
    new("Accounts", "AdminSDHolder protected objects", "6 orphaned adminCount=1 accounts",
        "AdminSDHolder ACL differs from the default", 1.0),
    new("Accounts", "Service account SPN hygiene", "duplicate SPN HTTP/intranet registered twice",
        "3 SPNs on user accounts with old passwords", 1.4),
    new("PKI", "CA weak key size", "issuing CA uses a 1024-bit RSA key",
        "issuing CA certificate expires in 9 months", 1.0),
    new("PKI", "Certificate template exposure", "template allows SAN from the requester (ESC1)",
        "template enrollable by Domain Users", 1.7),
    new("Kerberos", "Constrained delegation overview", "protocol transition allowed to DC services",
        "delegation to CIFS on 2 servers", 1.0),
    new("Forest", "Recycle Bin enabled", "AD Recycle Bin is not enabled",
        "deleted object lifetime is 60 days", 1.0),
    new("Forest", "Site link costs", "site link with cost 0 and 15-minute schedule gaps",
        "2 site links use default cost 100", 1.0),
    new("Forest", "Subnets without sites", "11 client subnets are not mapped to a site",
        "3 overlapping subnet definitions", 1.0),
    new("Forest", "Functional level", "domain functional level is Windows Server 2008 R2",
        "forest functional level is behind the domains", 1.0)
};
const int Workers = 6;
IReadOnlyList<KeyValuePair<string, string>> Targets() => [
    new("dom-ad", "ad.adatum.com (domain)"),
    new("dom-eu", "eu.ad.adatum.com (domain)"),
    new("dc1", "DC01.ad.adatum.com"),
    new("dc2", "DC02.ad.adatum.com"),
    new("dc3", "DC03.ad.adatum.com"),
    new("dc4", "DC04.ad.adatum.com"),
    new("dc5", "DC05.ad.adatum.com"),
    new("dc6", "DC06.ad.adatum.com"),
    new("dc7", "DC07.eu.ad.adatum.com"),
    new("dc8", "DC08.eu.ad.adatum.com"),
    new("dc9", "DC09.eu.ad.adatum.com"),
    new("dc10", "DC10.eu.ad.adatum.com"),
];
int TotalRules = (2 * DomainRules.Length) + (10 * DcRules.Length);
List<LiveEvent> Generate(int seed = 2026)
{
    Random random = new Random(seed);
    List<LiveEvent> events = new List<LiveEvent>();
    IReadOnlyList<KeyValuePair<string, string>> targets = Targets();
    Add(events, 0, "dashboard", "status", default, "discovering forest", "running");
    Add(events, 0, "phases", "state", "Discover", default, "running");
    foreach (KeyValuePair<string, string> target in targets)
    {
        int rules = target.Key.StartsWith("dom", StringComparison.Ordinal)
            ? DomainRules.Length
            : DcRules.Length;
        Add(events, 0, "lanes", "add", target.Key, target.Value, default, 0, rules);
    }

    Add(events, 400, "log", "write", default,
        "Connecting to ad.adatum.com via LDAP (DC01.ad.adatum.com)", "info");
    Add(events, 1300, "log", "write", default,
        "Forest ad.adatum.com: 2 domains, 10 domain controllers, 4 sites", "info");
    Add(events, 2100, "log", "write", default,
        "DC10.eu.ad.adatum.com responded slowly (812 ms LDAP bind)", "warn");
    Add(events, 2800, "phases", "state", "Discover", default, "pass");
    Add(events, 2800, "phases", "state", "Collect", default, "running");
    Add(events, 2800, "dashboard", "status", default, "collecting rule inputs", "running");
    Add(events, 3600, "log", "write", default, "Collecting 18 data sources per domain controller",
        "info");
    Add(events, 5200, "log", "write", default, "Using cached schema snapshot from 2026-10-15",
        "verbose");
    Add(events, 6800, "phases", "state", "Collect", default, "pass");
    Add(events, 6800, "phases", "state", "Evaluate", default, "running");
    Add(events, 6800, "dashboard", "status", default,
        "evaluating " + TotalRules.ToString(CultureInfo.InvariantCulture) + " rules on "
            + Workers.ToString(CultureInfo.InvariantCulture) + " workers",
        "running");
    double[] workerClock = new double[Workers];
    for (int w = 0; w < Workers; w++)
    {
        workerClock[w] = 7000 + (w * 180);
    }

    List<double> completions = new List<double>();
    List<double> failures = new List<double>();
    List<KeyValuePair<double, bool>> targetFinishes = new List<KeyValuePair<double, bool>>();
    int done = 0;
    int passed = 0;
    int failed = 0;
    int warned = 0;
    int skipped = 0;
    foreach (KeyValuePair<string, string> target in targets)
    {
        int worker = 0;
        for (int w = 1; w < Workers; w++)
        {
            if (workerClock[w] < workerClock[worker])
            {
                worker = w;
            }
        }

        double t = workerClock[worker];
        bool domain = target.Key.StartsWith("dom", StringComparison.Ordinal);
        SyntheticRule[] rules = domain ? DomainRules : DcRules;
        bool troubled = target.Key == "dc7";
        bool laneFailed = false;
        Add(events, t, "lanes", "state", target.Key, rules[0].Name, "running");
        Add(events, t, "log", "write", default,
            target.Value + ": evaluating " + rules.Length.ToString(CultureInfo.InvariantCulture)
                + " rules",
            "info");
        for (int r = 0; r < rules.Length; r++)
        {
            SyntheticRule rule = rules[r];

            // Native transcendental functions can differ by one ULP across operating systems. Keep the
            // synthetic recording stable at nanosecond precision, well below its millisecond timestamps.
            double seconds = Math.Round(
                Math.Exp(-0.9 + (0.75 * Gaussian(random))) * rule.Weight * (domain ? 1.6 : 1.0), 9);
            t += seconds * 1000;
            string result = PickResult(random, troubled && rule.Category == "Replication");
            string? next = r + 1 < rules.Length ? rules[r + 1].Name : "finishing";
            Add(events, t, "lanes", "advance", target.Key, next, result, 1);
            Add(events, t, "durations", "add", default, default, default, seconds);
            done++;
            completions.Add(t);
            switch (result)
            {
                case "pass":
                    passed++;
                    Add(events, t, "results", "add", "Passed", default, default, 1);
                    break;
                case "warn":
                    warned++;
                    Add(events, t, "results", "add", "Warnings", default, default, 1);
                    if (Sample(random) < 0.5)
                    {
                        Add(events, t, "log", "write", default,
                            target.Value + ": " + rule.Name + " - " + rule.WarnReason, "warn");
                    }

                    break;
                case "fail":
                    failed++;
                    laneFailed = true;
                    failures.Add(t);
                    Add(events, t, "results", "add", "Failed", default, default, 1);
                    Add(events, t, "findings", "add", rule.Category, default, default, 1);
                    Add(events, t, "log", "write", default,
                        target.Value + ": " + rule.Name + " failed - " + rule.FailReason, "err");
                    break;
                default:
                    skipped++;
                    Add(events, t, "results", "add", "Skipped", default, default, 1);
                    break;
            }
        }

        t += 150;
        Add(events, t, "lanes", "complete", target.Key);
        targetFinishes.Add(new KeyValuePair<double, bool>(t, laneFailed));
        workerClock[worker] = t + 120;
    }

    double evaluateEnd = 0;
    foreach (double clock in workerClock)
    {
        evaluateEnd = Math.Max(evaluateEnd, clock);
    }

    AddThroughput(events, completions, failures, 7000, evaluateEnd);
    AddSummaryUpdates(events, completions, targetFinishes, evaluateEnd);
    Add(events, evaluateEnd, "phases", "state", "Evaluate", default, failed > 0 ? "warn" : "pass");
    Add(events, evaluateEnd, "phases", "state", "Report", default, "running");
    Add(events, evaluateEnd, "dashboard", "status", default, "writing report", "running");
    Add(events, evaluateEnd + 900, "log", "write", default,
        "Report written to Reports/Assessment-ad.adatum.com.html (4.2 MB)", "pass");
    Add(events, evaluateEnd + 1800, "phases", "state", "Report", default, "pass");
    string final = done.ToString(CultureInfo.InvariantCulture) + " rules · "
        + passed.ToString(CultureInfo.InvariantCulture) + " passed · "
        + failed.ToString(CultureInfo.InvariantCulture) + " failed · "
        + warned.ToString(CultureInfo.InvariantCulture) + " warnings · "
        + skipped.ToString(CultureInfo.InvariantCulture) + " skipped";
    Add(events, evaluateEnd + 1800, "dashboard", "status", default, final,
        failed > 0 ? "warn" : "pass");
    Add(events, evaluateEnd + 4500, "log", "write", default, "Done.", "info");

    // Stable ordering: events with the same timestamp keep the order they were produced in.
    return events.OrderBy(e => e.Time).ToList();
}

void AddThroughput(List<LiveEvent> events, List<double> completions, List<double> failures,
    double start, double end)
{
    completions.Sort();
    failures.Sort();
    int ci = 0;
    int fi = 0;
    for (double second = start + 1000; second <= end + 1000; second += 1000)
    {
        int count = 0;
        int failCount = 0;
        while (ci < completions.Count && completions[ci] < second)
        {
            count++;
            ci++;
        }

        while (fi < failures.Count && failures[fi] < second)
        {
            failCount++;
            fi++;
        }

        Add(events, second, "throughput", "add", "completed", default, default, count);
        Add(events, second, "throughput", "add", "failed", default, default, failCount);
    }
}

void AddSummaryUpdates(List<LiveEvent> events, List<double> completions,
    List<KeyValuePair<double, bool>> targetFinishes, double end)
{
    int total = TotalRules;
    for (double second = 7000; second <= end + 1000; second += 1000)
    {
        int done = 0;
        foreach (double c in completions)
        {
            if (c <= second)
            {
                done++;
            }
        }

        double rate = (second - 7000) > 0 ? done / ((second - 7000) / 1000.0) : 0;
        Add(events, second, "summary", "set", "Evaluated",
            done.ToString(CultureInfo.InvariantCulture) + " / "
                + total.ToString(CultureInfo.InvariantCulture));
        Add(events, second, "summary", "set", "Average rate",
            rate.ToString("0.0", CultureInfo.InvariantCulture) + " rules/s");
        double remaining = rate > 0 ? (total - done) / rate : 0;
        Add(events, second, "summary", "set", "Remaining",
            done >= total
                ? "-"
                : "~" + Math.Ceiling(remaining).ToString(CultureInfo.InvariantCulture) + "s");
        int targetsDone = 0;
        int targetsFailed = 0;
        foreach (KeyValuePair<double, bool> finish in targetFinishes)
        {
            if (finish.Key <= second)
            {
                targetsDone++;
                if (finish.Value)
                {
                    targetsFailed++;
                }
            }
        }

        Add(events, second, "summary", "set", "Targets done",
            targetsDone.ToString(CultureInfo.InvariantCulture) + " / 12");
        Add(events, second, "summary", "set", "Targets failing",
            targetsFailed.ToString(CultureInfo.InvariantCulture));
    }
}

string PickResult(Random random, bool troubled)
{
    double roll = Sample(random);
    if (troubled)
    {
        return roll < 0.45 ? "fail" : roll < 0.7 ? "warn" : "pass";
    }

    if (roll < 0.045)
    {
        return "fail";
    }

    if (roll < 0.15)
    {
        return "warn";
    }

    if (roll < 0.19)
    {
        return "skip";
    }

    return "pass";
}

// Match Get-Random -SetSeed's byte-based sampler so the native PowerShell and C# inputs agree.
double Sample(Random random)
{
    byte[] bytes = new byte[4];
    int value;
    do
    {
        random.NextBytes(bytes);
        value = BitConverter.ToInt32(bytes, 0);
    }
    while (value == int.MaxValue);
    if (value < 0)
        value += int.MaxValue;
    return value * (1.0 / int.MaxValue);
}

double Gaussian(Random random)
{
    double u1 = 1.0 - Sample(random);
    double u2 = Sample(random);
    return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
}

void Add(List<LiveEvent> events, double time, string target, string operation, string? key = null,
    string? text = null, string? state = null, double value = 0, double total = 0)
{
    events.Add(new LiveEvent(target, operation, key, text, state, value, total,
        Math.Max(1, (long)Math.Round(time))));
}

// Layout: retain the worker lanes, result split, rate chart, duration histogram and category findings.
var events = Generate();
Phases phases = Ui.Phases().Set("Discover").Set("Collect").Set("Evaluate").Set("Report");
StackedBar results = ((StackedBar)Ui.Chart()
    .Type(ChartType.Stacked)
    .WidgetId("results")
    .Build()).Add("Passed", 0, ThemeToken.Pass)
    .Add("Warnings", 0, ThemeToken.Warn)
    .Add("Failed", 0, ThemeToken.Fail)
    .Add("Skipped", 0, ThemeToken.Skip);
KeyValueList summary = Ui.KeyValue("summary")
    .Add("Evaluated", "0 / " + TotalRules)
    .Add("Average rate", "-")
    .Add("Remaining", "-")
    .Add("Targets done", "0 / 12")
    .Add("Targets failing", "0");
LineChart throughput = (LineChart)Ui.Chart()
    .Type(ChartType.Line)
    .WidgetId("throughput")
    .Height(7)
    .Build();
throughput.AddSeries("completed", ThemeToken.Accent);
throughput.AddSeries("failed", ThemeToken.Fail);
Histogram durations = (Histogram)Ui.Chart()
    .Type(ChartType.Histogram)
    .Edges(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 })
    .WidgetId("durations")
    .Height(6)
    .Unit("s")
    .Build();
BarChart findings = (BarChart)Ui.Chart()
    .Type(ChartType.Bar)
    .WidgetId("findings")
    .Role(ThemeToken.Fail)
    .Build();
foreach (string category in new[]
{
    "Domain controllers", "Replication", "DNS", "Kerberos", "Security", "Group Policy", "Accounts",
    "PKI"
}

)
    findings.Add(category, 0);
Lanes lanes = Ui.Lanes().MaxLabelWidth(28);
LogRegion log = Ui.LogRegion().PreferredHeight(6);
Rows body = Ui.Rows().Content(c4 =>
{
    c4.Padding(padding => padding.Bottom(1).Right(1).Top(0).Left(1).Content(c2 =>
    {
        c2.Add(phases);
    }));
    c4.Columns(columns => columns.Add(Ui.Panel().Title("Targets").Content(c2 =>
    {
        c2.Add(lanes);
    }).HeaderRight("6 workers"), LayoutSize.Fraction(2.3))
        .Add(Ui.Panel().Title("Results").Content(c3 =>
        {
            c3.Rows(r2 => r2.Content(c2 =>
            {
                c2.Add(results);
                c2.Rule();
                c2.Add(summary);
            }));
        })));
    c4.Columns(columns => columns.Add(Ui.Panel().Title("Throughput").Content(c2 =>
    {
        c2.Add(throughput);
    }).HeaderRight("rules/s"), LayoutSize.Fraction(1.2))
        .Add(Ui.Panel().Title("Rule duration").Content(c2 =>
        {
            c2.Add(durations);
        }))
        .Add(Ui.Panel().Title("Failures by category").Content(c2 =>
        {
            c2.Add(findings);
        })));
}).Add(Ui.Panel().Title("Log").Content(c2 =>
{
    c2.Add(log);
}), LayoutSize.Fraction());
Dashboard dashboard = Ui.Dashboard()
    .Content(c2 =>
    {
        c2.Add(body);
    })
    .Title("[bold]Nightly checks[/]")
    .Subtitle("ad.adatum.com forest assessment · 12 targets · 332 rules")
    .ShowElapsed(false)
    .Footer("[muted]Discover · Collect · Evaluate · Report    Ctrl+C stop[/]");

// Behaviour: post each synthetic event in a bounded live run, paced at 4x.
using LiveSession session = dashboard.CreateSession();
try
{
    session.Run(live =>
    {
        long previous = 0;
        foreach (LiveEvent update in events)
        {
            int delay = (int)((update.Time - previous) / 4);
            live.Cancellation.ThrowIfCancellationRequested();
            live.Post(update);
            live.Delay(TimeSpan.FromMilliseconds(delay));
            previous = update.Time;
        }
    });
}
catch (OperationCanceledException cancelled)
    when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}

record SyntheticRule(string Category, string Name, string FailReason, string WarnReason,
    double Weight = 1);
