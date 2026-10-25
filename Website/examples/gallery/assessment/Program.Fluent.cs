// Forest assessment: twelve targets, six workers, 332 rules and a complete findings dashboard.
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional targets and rules, generated entirely in memory. No network checks or files are written.
SyntheticRule[] DcRules = [new(Category: "Domain controllers", Name: "LDAP signing required", FailReason: "LDAPServerIntegrity is 1 (signing not required)", WarnReason: "12 unsigned binds in the last 24h", Weight: 1.0), new(Category: "Domain controllers", Name: "LDAP channel binding", FailReason: "LdapEnforceChannelBinding is 0", WarnReason: "channel binding set to 'when supported'", Weight: 1.0), new(Category: "Domain controllers", Name: "SMB signing enforced", FailReason: "RequireSecuritySignature is disabled", WarnReason: "SMB1 client still installed", Weight: 1.0), new(Category: "Domain controllers", Name: "Print Spooler disabled", FailReason: "Spooler service is running", WarnReason: "Spooler set to manual start", Weight: 1.0), new(Category: "Domain controllers", Name: "LSA protection enabled", FailReason: "RunAsPPL is not set", WarnReason: "RunAsPPL set without UEFI lock", Weight: 1.0), new(Category: "Domain controllers", Name: "Credential Guard posture", FailReason: "virtualization-based security is off", WarnReason: "Credential Guard configured but not running", Weight: 1.4), new(Category: "Domain controllers", Name: "Secure Boot certificate rollout", FailReason: "2023 CA certificate not applied", WarnReason: "update staged, reboot pending", Weight: 1.0), new(Category: "Domain controllers", Name: "Windows Update compliance", FailReason: "last cumulative update 94 days ago", WarnReason: "update installed, restart pending", Weight: 2.2), new(Category: "Domain controllers", Name: "Operating system support", FailReason: "Windows Server 2012 R2 is out of support", WarnReason: "support ends in 11 months", Weight: 1.0), new(Category: "Domain controllers", Name: "NTLM audit configuration", FailReason: "NTLM auditing is disabled", WarnReason: "audit enabled for incoming traffic only", Weight: 1.0), new(Category: "Replication", Name: "Replication latency per partner", FailReason: "no successful inbound replication from DC02 for 4h", WarnReason: "latency to DC05 is 42 minutes", Weight: 2.6), new(Category: "Replication", Name: "USN rollback detection", FailReason: "event 2095 logged (USN rollback suspected)", WarnReason: "invocation ID changed after restore", Weight: 1.8), new(Category: "Replication", Name: "SYSVOL DFSR state", FailReason: "DFSR state is 'In Error' (5)", WarnReason: "DFSR backlog of 37 files", Weight: 1.5), new(Category: "Replication", Name: "Tombstone lifetime", FailReason: "tombstone lifetime is 60 days", WarnReason: "tombstone lifetime not set explicitly", Weight: 1.0), new(Category: "Replication", Name: "Lingering objects", FailReason: "3 lingering objects in the configuration partition", WarnReason: "strict replication consistency is off", Weight: 2.0), new(Category: "DNS", Name: "DNS forwarders consistency", FailReason: "forwarders differ from the domain baseline", WarnReason: "one forwarder does not answer", Weight: 1.0), new(Category: "DNS", Name: "DNS scavenging", FailReason: "scavenging disabled on all zones", WarnReason: "no-refresh interval longer than 7 days", Weight: 1.0), new(Category: "DNS", Name: "Root hints reachable", FailReason: "4 of 13 root servers unreachable", WarnReason: "root hints file older than 2 years", Weight: 1.6), new(Category: "DNS", Name: "DC locator SRV records", FailReason: "_ldap._tcp.dc._msdcs record missing", WarnReason: "stale SRV record for a retired DC", Weight: 1.0), new(Category: "Time", Name: "Time source hierarchy", FailReason: "PDC emulator syncs from the local CMOS clock", WarnReason: "NTP server configured by IP address", Weight: 1.0), new(Category: "Time", Name: "Clock skew", FailReason: "clock skew of 7 minutes against the PDC emulator", WarnReason: "clock skew of 48 seconds", Weight: 1.0), new(Category: "Kerberos", Name: "Kerberos AES-only encryption", FailReason: "RC4 still allowed for Kerberos", WarnReason: "3 accounts without AES keys", Weight: 1.0), new(Category: "Kerberos", Name: "krbtgt password age", FailReason: "krbtgt password not changed for 812 days", WarnReason: "krbtgt password is 190 days old", Weight: 1.0), new(Category: "Kerberos", Name: "Unconstrained delegation", FailReason: "2 servers trusted for unconstrained delegation", WarnReason: "1 service account trusted for delegation", Weight: 1.0), new(Category: "Security", Name: "Audit policy baseline", FailReason: "advanced audit policy not applied", WarnReason: "logon auditing set to success only", Weight: 1.3), new(Category: "Security", Name: "Event log sizes", FailReason: "Security log is 20 MB and overwrites in 2 hours", WarnReason: "Directory Service log under 64 MB", Weight: 1.0), new(Category: "Security", Name: "Local administrators", FailReason: "4 unexpected members in BUILTIN\\Administrators", WarnReason: "nested group in BUILTIN\\Administrators", Weight: 1.0), new(Category: "Security", Name: "Firewall profile", FailReason: "domain firewall profile is disabled", WarnReason: "inbound rule allows any remote address", Weight: 1.0), new(Category: "Security", Name: "Remote Desktop exposure", FailReason: "RDP allowed without network level authentication", WarnReason: "RDP open to all internal subnets", Weight: 1.0), new(Category: "Security", Name: "PowerShell logging", FailReason: "script block logging is disabled", WarnReason: "transcription not configured", Weight: 1.0), ];
SyntheticRule[] DomainRules = [new(Category: "Group Policy", Name: "GPO permissions baseline", FailReason: "Authenticated Users can edit 2 GPOs", WarnReason: "a GPO without Authenticated Users read", Weight: 2.4), new(Category: "Group Policy", Name: "Orphaned GPO folders", FailReason: "5 SYSVOL folders without a GPO object", WarnReason: "1 GPO object without a SYSVOL folder", Weight: 1.0), new(Category: "Group Policy", Name: "Empty GPOs", FailReason: "9 GPOs with no settings are linked", WarnReason: "3 unlinked empty GPOs", Weight: 1.0), new(Category: "Group Policy", Name: "LAPS policy", FailReason: "no LAPS policy applies to member servers", WarnReason: "legacy LAPS still deployed alongside Windows LAPS", Weight: 1.0), new(Category: "Accounts", Name: "Stale computer accounts", FailReason: "214 computers inactive for over 180 days", WarnReason: "41 computers inactive for over 90 days", Weight: 1.8), new(Category: "Accounts", Name: "Password never expires", FailReason: "18 user accounts never expire their password", WarnReason: "2 service accounts never expire their password", Weight: 1.0), new(Category: "Accounts", Name: "Privileged group membership", FailReason: "Domain Admins has 14 members", WarnReason: "a disabled account is still in Enterprise Admins", Weight: 1.5), new(Category: "Accounts", Name: "AdminSDHolder protected objects", FailReason: "6 orphaned adminCount=1 accounts", WarnReason: "AdminSDHolder ACL differs from the default", Weight: 1.0), new(Category: "Accounts", Name: "Service account SPN hygiene", FailReason: "duplicate SPN HTTP/intranet registered twice", WarnReason: "3 SPNs on user accounts with old passwords", Weight: 1.4), new(Category: "PKI", Name: "CA weak key size", FailReason: "issuing CA uses a 1024-bit RSA key", WarnReason: "issuing CA certificate expires in 9 months", Weight: 1.0), new(Category: "PKI", Name: "Certificate template exposure", FailReason: "template allows SAN from the requester (ESC1)", WarnReason: "template enrollable by Domain Users", Weight: 1.7), new(Category: "Kerberos", Name: "Constrained delegation overview", FailReason: "protocol transition allowed to DC services", WarnReason: "delegation to CIFS on 2 servers", Weight: 1.0), new(Category: "Forest", Name: "Recycle Bin enabled", FailReason: "AD Recycle Bin is not enabled", WarnReason: "deleted object lifetime is 60 days", Weight: 1.0), new(Category: "Forest", Name: "Site link costs", FailReason: "site link with cost 0 and 15-minute schedule gaps", WarnReason: "2 site links use default cost 100", Weight: 1.0), new(Category: "Forest", Name: "Subnets without sites", FailReason: "11 client subnets are not mapped to a site", WarnReason: "3 overlapping subnet definitions", Weight: 1.0), new(Category: "Forest", Name: "Functional level", FailReason: "domain functional level is Windows Server 2008 R2", WarnReason: "forest functional level is behind the domains", Weight: 1.0), ];
const int Workers = 6;
IReadOnlyList<KeyValuePair<string, string>> Targets() => [new(key: "dom-ad", value: "ad.adatum.com (domain)"), new(key: "dom-eu", value: "eu.ad.adatum.com (domain)"), new(key: "dc1", value: "DC01.ad.adatum.com"), new(key: "dc2", value: "DC02.ad.adatum.com"), new(key: "dc3", value: "DC03.ad.adatum.com"), new(key: "dc4", value: "DC04.ad.adatum.com"), new(key: "dc5", value: "DC05.ad.adatum.com"), new(key: "dc6", value: "DC06.ad.adatum.com"), new(key: "dc7", value: "DC07.eu.ad.adatum.com"), new(key: "dc8", value: "DC08.eu.ad.adatum.com"), new(key: "dc9", value: "DC09.eu.ad.adatum.com"), new(key: "dc10", value: "DC10.eu.ad.adatum.com"), ];
int TotalRules = (2 * DomainRules.Length) + (10 * DcRules.Length);
List<LiveEvent> Generate(int seed = 2026)
{
    Random random = new Random(seed);
    List<LiveEvent> events = new List<LiveEvent>();
    IReadOnlyList<KeyValuePair<string, string>> targets = Targets();
    Add(events, 0, "dashboard", "status", text: "discovering forest", state: "running");
    Add(events, 0, "phases", "state", "Discover", state: "running");
    foreach (KeyValuePair<string, string> target in targets)
    {
        int rules = target.Key.StartsWith("dom", StringComparison.Ordinal) ? DomainRules.Length : DcRules.Length;
        Add(events, 0, "lanes", "add", target.Key, target.Value, total: rules);
    }

    Add(events, 400, "log", "write", text: "Connecting to ad.adatum.com via LDAP (DC01.ad.adatum.com)", state: "info");
    Add(events, 1300, "log", "write", text: "Forest ad.adatum.com: 2 domains, 10 domain controllers, 4 sites", state: "info");
    Add(events, 2100, "log", "write", text: "DC10.eu.ad.adatum.com responded slowly (812 ms LDAP bind)", state: "warn");
    Add(events, 2800, "phases", "state", "Discover", state: "pass");
    Add(events, 2800, "phases", "state", "Collect", state: "running");
    Add(events, 2800, "dashboard", "status", text: "collecting rule inputs", state: "running");
    Add(events, 3600, "log", "write", text: "Collecting 18 data sources per domain controller", state: "info");
    Add(events, 5200, "log", "write", text: "Using cached schema snapshot from 2026-10-15", state: "verbose");
    Add(events, 6800, "phases", "state", "Collect", state: "pass");
    Add(events, 6800, "phases", "state", "Evaluate", state: "running");
    Add(events, 6800, "dashboard", "status", text: "evaluating " + TotalRules.ToString(CultureInfo.InvariantCulture) + " rules on " + Workers.ToString(CultureInfo.InvariantCulture) + " workers", state: "running");
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
        Add(events, t, "log", "write", text: target.Value + ": evaluating " + rules.Length.ToString(CultureInfo.InvariantCulture) + " rules", state: "info");
        for (int r = 0; r < rules.Length; r++)
        {
            SyntheticRule rule = rules[r];
            // Native transcendental functions can differ by one ULP across operating systems. Keep the
            // synthetic recording stable at nanosecond precision, well below its millisecond timestamps.
            double seconds = Math.Round(Math.Exp(-0.9 + (0.75 * Gaussian(random))) * rule.Weight * (domain ? 1.6 : 1.0), 9);
            t += seconds * 1000;
            string result = PickResult(random, troubled && rule.Category == "Replication");
            string? next = r + 1 < rules.Length ? rules[r + 1].Name : "finishing";
            Add(events, t, "lanes", "advance", target.Key, next, result, 1);
            Add(events, t, "durations", "add", value: seconds);
            done++;
            completions.Add(t);
            switch (result)
            {
                case "pass":
                    passed++;
                    Add(events, t, "results", "add", "Passed", value: 1);
                    break;
                case "warn":
                    warned++;
                    Add(events, t, "results", "add", "Warnings", value: 1);
                    if (Sample(random) < 0.5)
                    {
                        Add(events, t, "log", "write", text: target.Value + ": " + rule.Name + " - " + rule.WarnReason, state: "warn");
                    }

                    break;
                case "fail":
                    failed++;
                    laneFailed = true;
                    failures.Add(t);
                    Add(events, t, "results", "add", "Failed", value: 1);
                    Add(events, t, "findings", "add", rule.Category, value: 1);
                    Add(events, t, "log", "write", text: target.Value + ": " + rule.Name + " failed - " + rule.FailReason, state: "err");
                    break;
                default:
                    skipped++;
                    Add(events, t, "results", "add", "Skipped", value: 1);
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
    Add(events, evaluateEnd, "phases", "state", "Evaluate", state: failed > 0 ? "warn" : "pass");
    Add(events, evaluateEnd, "phases", "state", "Report", state: "running");
    Add(events, evaluateEnd, "dashboard", "status", text: "writing report", state: "running");
    Add(events, evaluateEnd + 900, "log", "write", text: "Report written to Reports/Assessment-ad.adatum.com.html (4.2 MB)", state: "pass");
    Add(events, evaluateEnd + 1800, "phases", "state", "Report", state: "pass");
    string final = done.ToString(CultureInfo.InvariantCulture) + " rules · " + passed.ToString(CultureInfo.InvariantCulture) + " passed · " + failed.ToString(CultureInfo.InvariantCulture) + " failed · " + warned.ToString(CultureInfo.InvariantCulture) + " warnings · " + skipped.ToString(CultureInfo.InvariantCulture) + " skipped";
    Add(events, evaluateEnd + 1800, "dashboard", "status", text: final, state: failed > 0 ? "warn" : "pass");
    Add(events, evaluateEnd + 4500, "log", "write", text: "Done.", state: "info");
    // Stable ordering: events with the same timestamp keep the order they were produced in.
    return events.OrderBy(e => e.Time).ToList();
}

void AddThroughput(List<LiveEvent> events, List<double> completions, List<double> failures, double start, double end)
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

        Add(events, second, "throughput", "add", "completed", value: count);
        Add(events, second, "throughput", "add", "failed", value: failCount);
    }
}

void AddSummaryUpdates(List<LiveEvent> events, List<double> completions, List<KeyValuePair<double, bool>> targetFinishes, double end)
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
        Add(events, second, "summary", "set", "Evaluated", done.ToString(CultureInfo.InvariantCulture) + " / " + total.ToString(CultureInfo.InvariantCulture));
        Add(events, second, "summary", "set", "Average rate", rate.ToString("0.0", CultureInfo.InvariantCulture) + " rules/s");
        double remaining = rate > 0 ? (total - done) / rate : 0;
        Add(events, second, "summary", "set", "Remaining", done >= total ? "-" : "~" + Math.Ceiling(remaining).ToString(CultureInfo.InvariantCulture) + "s");
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

        Add(events, second, "summary", "set", "Targets done", targetsDone.ToString(CultureInfo.InvariantCulture) + " / 12");
        Add(events, second, "summary", "set", "Targets failing", targetsFailed.ToString(CultureInfo.InvariantCulture));
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

void Add(List<LiveEvent> events, double time, string target, string operation, string? key = null, string? text = null, string? state = null, double value = 0, double total = 0)
{
    events.Add(new LiveEvent(target, operation, key, text, state, value, total, Math.Max(1, (long)Math.Round(time))));
}

// Layout: retain the worker lanes, result split, rate chart, duration histogram and category findings.
var events = Generate();
Phases phases = new Phases().Set("Discover").Set("Collect").Set("Evaluate").Set("Report");
StackedBar results = new StackedBar("results").Add("Passed", 0, ThemeToken.Pass).Add("Warnings", 0, ThemeToken.Warn).Add("Failed", 0, ThemeToken.Fail).Add("Skipped", 0, ThemeToken.Skip);
KeyValueList summary = new KeyValueList("summary").Add("Evaluated", "0 / " + TotalRules).Add("Average rate", "-").Add("Remaining", "-").Add("Targets done", "0 / 12").Add("Targets failing", "0");
LineChart throughput = new LineChart("throughput").Height(7);
throughput.AddSeries("completed", ThemeToken.Accent);
throughput.AddSeries("failed", ThemeToken.Fail);
Histogram durations = new Histogram(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 }, "durations").Height(6).Unit("s");
BarChart findings = new BarChart("findings").Role(ThemeToken.Fail);
foreach (string category in new[]
{
    "Domain controllers",
    "Replication",
    "DNS",
    "Kerberos",
    "Security",
    "Group Policy",
    "Accounts",
    "PKI"
}

)
    findings.Add(category, 0);
Lanes lanes = new Lanes().MaxLabelWidth(28);
LogRegion log = new LogRegion().PreferredHeight(6);
Rows body = new Rows(new Padding().Bottom(1).Right(1).Top(0).Left(1).Content(phases), new Columns().Add(new Panel().Title("Targets").Content(lanes).HeaderRight("6 workers"), LayoutSize.Fraction(2.3)).Add(new Panel().Title("Results").Content(new Rows(results, new Rule(), summary))), new Columns().Add(new Panel().Title("Throughput").Content(throughput).HeaderRight("rules/s"), LayoutSize.Fraction(1.2)).Add(new Panel().Title("Rule duration").Content(durations)).Add(new Panel().Title("Failures by category").Content(findings))).Add(new Panel().Title("Log").Content(log), LayoutSize.Fraction());
Dashboard dashboard = new Dashboard().Content(body).Title("[bold]Nightly checks[/]").Subtitle("ad.adatum.com forest assessment · 12 targets · 332 rules").ShowElapsed(false).Footer("[muted]Discover · Collect · Evaluate · Report    Ctrl+C stop[/]");
// Behaviour: post each synthetic event in a bounded live run, paced at 4x.
using LiveSession session = new LiveSession(dashboard);
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
catch (OperationCanceledException cancelled)when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}

record SyntheticRule(string Category, string Name, string FailReason, string WarnReason, double Weight = 1);
