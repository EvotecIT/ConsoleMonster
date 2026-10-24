// Forest assessment: twelve targets, six workers, 332 rules and a complete findings dashboard.
using System.Globalization;
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional targets and rules, generated entirely in memory. No network checks or files are written.
SyntheticRule[] DcRules = {
    new SyntheticRule("Domain controllers", "LDAP signing required", "LDAPServerIntegrity is 1 (signing not required)", "12 unsigned binds in the last 24h"),
    new SyntheticRule("Domain controllers", "LDAP channel binding", "LdapEnforceChannelBinding is 0", "channel binding set to 'when supported'"),
    new SyntheticRule("Domain controllers", "SMB signing enforced", "RequireSecuritySignature is disabled", "SMB1 client still installed"),
    new SyntheticRule("Domain controllers", "Print Spooler disabled", "Spooler service is running", "Spooler set to manual start"),
    new SyntheticRule("Domain controllers", "LSA protection enabled", "RunAsPPL is not set", "RunAsPPL set without UEFI lock"),
    new SyntheticRule("Domain controllers", "Credential Guard posture", "virtualization-based security is off", "Credential Guard configured but not running", 1.4),
    new SyntheticRule("Domain controllers", "Secure Boot certificate rollout", "2023 CA certificate not applied", "update staged, reboot pending"),
    new SyntheticRule("Domain controllers", "Windows Update compliance", "last cumulative update 94 days ago", "update installed, restart pending", 2.2),
    new SyntheticRule("Domain controllers", "Operating system support", "Windows Server 2012 R2 is out of support", "support ends in 11 months"),
    new SyntheticRule("Domain controllers", "NTLM audit configuration", "NTLM auditing is disabled", "audit enabled for incoming traffic only"),
    new SyntheticRule("Replication", "Replication latency per partner", "no successful inbound replication from DC02 for 4h", "latency to DC05 is 42 minutes", 2.6),
    new SyntheticRule("Replication", "USN rollback detection", "event 2095 logged (USN rollback suspected)", "invocation ID changed after restore", 1.8),
    new SyntheticRule("Replication", "SYSVOL DFSR state", "DFSR state is 'In Error' (5)", "DFSR backlog of 37 files", 1.5),
    new SyntheticRule("Replication", "Tombstone lifetime", "tombstone lifetime is 60 days", "tombstone lifetime not set explicitly"),
    new SyntheticRule("Replication", "Lingering objects", "3 lingering objects in the configuration partition", "strict replication consistency is off", 2.0),
    new SyntheticRule("DNS", "DNS forwarders consistency", "forwarders differ from the domain baseline", "one forwarder does not answer"),
    new SyntheticRule("DNS", "DNS scavenging", "scavenging disabled on all zones", "no-refresh interval longer than 7 days"),
    new SyntheticRule("DNS", "Root hints reachable", "4 of 13 root servers unreachable", "root hints file older than 2 years", 1.6),
    new SyntheticRule("DNS", "DC locator SRV records", "_ldap._tcp.dc._msdcs record missing", "stale SRV record for a retired DC"),
    new SyntheticRule("Time", "Time source hierarchy", "PDC emulator syncs from the local CMOS clock", "NTP server configured by IP address"),
    new SyntheticRule("Time", "Clock skew", "clock skew of 7 minutes against the PDC emulator", "clock skew of 48 seconds"),
    new SyntheticRule("Kerberos", "Kerberos AES-only encryption", "RC4 still allowed for Kerberos", "3 accounts without AES keys"),
    new SyntheticRule("Kerberos", "krbtgt password age", "krbtgt password not changed for 812 days", "krbtgt password is 190 days old"),
    new SyntheticRule("Kerberos", "Unconstrained delegation", "2 servers trusted for unconstrained delegation", "1 service account trusted for delegation"),
    new SyntheticRule("Security", "Audit policy baseline", "advanced audit policy not applied", "logon auditing set to success only", 1.3),
    new SyntheticRule("Security", "Event log sizes", "Security log is 20 MB and overwrites in 2 hours", "Directory Service log under 64 MB"),
    new SyntheticRule("Security", "Local administrators", "4 unexpected members in BUILTIN\\Administrators", "nested group in BUILTIN\\Administrators"),
    new SyntheticRule("Security", "Firewall profile", "domain firewall profile is disabled", "inbound rule allows any remote address"),
    new SyntheticRule("Security", "Remote Desktop exposure", "RDP allowed without network level authentication", "RDP open to all internal subnets"),
    new SyntheticRule("Security", "PowerShell logging", "script block logging is disabled", "transcription not configured")
};

SyntheticRule[] DomainRules = {
    new SyntheticRule("Group Policy", "GPO permissions baseline", "Authenticated Users can edit 2 GPOs", "a GPO without Authenticated Users read", 2.4),
    new SyntheticRule("Group Policy", "Orphaned GPO folders", "5 SYSVOL folders without a GPO object", "1 GPO object without a SYSVOL folder"),
    new SyntheticRule("Group Policy", "Empty GPOs", "9 GPOs with no settings are linked", "3 unlinked empty GPOs"),
    new SyntheticRule("Group Policy", "LAPS policy", "no LAPS policy applies to member servers", "legacy LAPS still deployed alongside Windows LAPS"),
    new SyntheticRule("Accounts", "Stale computer accounts", "214 computers inactive for over 180 days", "41 computers inactive for over 90 days", 1.8),
    new SyntheticRule("Accounts", "Password never expires", "18 user accounts never expire their password", "2 service accounts never expire their password"),
    new SyntheticRule("Accounts", "Privileged group membership", "Domain Admins has 14 members", "a disabled account is still in Enterprise Admins", 1.5),
    new SyntheticRule("Accounts", "AdminSDHolder protected objects", "6 orphaned adminCount=1 accounts", "AdminSDHolder ACL differs from the default"),
    new SyntheticRule("Accounts", "Service account SPN hygiene", "duplicate SPN HTTP/intranet registered twice", "3 SPNs on user accounts with old passwords", 1.4),
    new SyntheticRule("PKI", "CA weak key size", "issuing CA uses a 1024-bit RSA key", "issuing CA certificate expires in 9 months"),
    new SyntheticRule("PKI", "Certificate template exposure", "template allows SAN from the requester (ESC1)", "template enrollable by Domain Users", 1.7),
    new SyntheticRule("Kerberos", "Constrained delegation overview", "protocol transition allowed to DC services", "delegation to CIFS on 2 servers"),
    new SyntheticRule("Forest", "Recycle Bin enabled", "AD Recycle Bin is not enabled", "deleted object lifetime is 60 days"),
    new SyntheticRule("Forest", "Site link costs", "site link with cost 0 and 15-minute schedule gaps", "2 site links use default cost 100"),
    new SyntheticRule("Forest", "Subnets without sites", "11 client subnets are not mapped to a site", "3 overlapping subnet definitions"),
    new SyntheticRule("Forest", "Functional level", "domain functional level is Windows Server 2008 R2", "forest functional level is behind the domains")
};
const int Workers = 6;
IReadOnlyList<KeyValuePair<string, string>> Targets() {
    List<KeyValuePair<string, string>> targets = new List<KeyValuePair<string, string>> {
        new KeyValuePair<string, string>("dom-ad", "ad.adatum.com (domain)"),
        new KeyValuePair<string, string>("dom-eu", "eu.ad.adatum.com (domain)")
    };
    for (int i = 1; i <= 10; i++) {
        string dc = "DC" + i.ToString("00", CultureInfo.InvariantCulture) + (i <= 6 ? ".ad.adatum.com" : ".eu.ad.adatum.com");
        targets.Add(new KeyValuePair<string, string>("dc" + i.ToString(CultureInfo.InvariantCulture), dc));
    }
    return targets;
}
int TotalRules = (2 * DomainRules.Length) + (10 * DcRules.Length);
List<LiveEvent> Generate(int seed = 2026) {
    Random random = new Random(seed);
    List<LiveEvent> events = new List<LiveEvent>();
    IReadOnlyList<KeyValuePair<string, string>> targets = Targets();

    Add(events, 0, "dashboard", "status", text: "discovering forest", state: "running");
    Add(events, 0, "phases", "state", "Discover", state: "running");
    foreach (KeyValuePair<string, string> target in targets) {
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
    for (int w = 0; w < Workers; w++) {
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
    foreach (KeyValuePair<string, string> target in targets) {
        int worker = 0;
        for (int w = 1; w < Workers; w++) {
            if (workerClock[w] < workerClock[worker]) {
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
        for (int r = 0; r < rules.Length; r++) {
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
            switch (result) {
                case "pass":
                    passed++;
                    Add(events, t, "results", "add", "Passed", value: 1);
                    break;
                case "warn":
                    warned++;
                    Add(events, t, "results", "add", "Warnings", value: 1);
                    if (Sample(random) < 0.5) {
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
    foreach (double clock in workerClock) {
        evaluateEnd = Math.Max(evaluateEnd, clock);
    }
    AddThroughput(events, completions, failures, 7000, evaluateEnd);
    AddSummaryUpdates(events, completions, targetFinishes, evaluateEnd);

    Add(events, evaluateEnd, "phases", "state", "Evaluate", state: failed > 0 ? "warn" : "pass");
    Add(events, evaluateEnd, "phases", "state", "Report", state: "running");
    Add(events, evaluateEnd, "dashboard", "status", text: "writing report", state: "running");
    Add(events, evaluateEnd + 900, "log", "write", text: "Report written to Reports/Assessment-ad.adatum.com.html (4.2 MB)", state: "pass");
    Add(events, evaluateEnd + 1800, "phases", "state", "Report", state: "pass");
    string final = done.ToString(CultureInfo.InvariantCulture) + " rules · " + passed.ToString(CultureInfo.InvariantCulture) + " passed · "
        + failed.ToString(CultureInfo.InvariantCulture) + " failed · " + warned.ToString(CultureInfo.InvariantCulture) + " warnings · " + skipped.ToString(CultureInfo.InvariantCulture) + " skipped";
    Add(events, evaluateEnd + 1800, "dashboard", "status", text: final, state: failed > 0 ? "warn" : "pass");
    Add(events, evaluateEnd + 4500, "log", "write", text: "Done.", state: "info");

    // Stable ordering: events with the same timestamp keep the order they were produced in.
    return events.OrderBy(e => e.Time).ToList();
}

void AddThroughput(List<LiveEvent> events, List<double> completions, List<double> failures, double start, double end) {
    completions.Sort();
    failures.Sort();
    int ci = 0;
    int fi = 0;
    for (double second = start + 1000; second <= end + 1000; second += 1000) {
        int count = 0;
        int failCount = 0;
        while (ci < completions.Count && completions[ci] < second) {
            count++;
            ci++;
        }
        while (fi < failures.Count && failures[fi] < second) {
            failCount++;
            fi++;
        }
        Add(events, second, "throughput", "add", "completed", value: count);
        Add(events, second, "throughput", "add", "failed", value: failCount);
    }
}

void AddSummaryUpdates(List<LiveEvent> events, List<double> completions, List<KeyValuePair<double, bool>> targetFinishes, double end) {
    int total = TotalRules;
    for (double second = 7000; second <= end + 1000; second += 1000) {
        int done = 0;
        foreach (double c in completions) {
            if (c <= second) {
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
        foreach (KeyValuePair<double, bool> finish in targetFinishes) {
            if (finish.Key <= second) {
                targetsDone++;
                if (finish.Value) {
                    targetsFailed++;
                }
            }
        }
        Add(events, second, "summary", "set", "Targets done", targetsDone.ToString(CultureInfo.InvariantCulture) + " / 12");
        Add(events, second, "summary", "set", "Targets failing", targetsFailed.ToString(CultureInfo.InvariantCulture));
    }
}

string PickResult(Random random, bool troubled) {
    double roll = Sample(random);
    if (troubled) {
        return roll < 0.45 ? "fail" : roll < 0.7 ? "warn" : "pass";
    }
    if (roll < 0.045) {
        return "fail";
    }
    if (roll < 0.15) {
        return "warn";
    }
    if (roll < 0.19) {
        return "skip";
    }
    return "pass";
}

// Match Get-Random -SetSeed's byte-based sampler so the native PowerShell and C# inputs agree.
double Sample(Random random) {
    byte[] bytes = new byte[4];
    int value;
    do {
        random.NextBytes(bytes);
        value = BitConverter.ToInt32(bytes, 0);
    } while (value == int.MaxValue);
    if (value < 0) value += int.MaxValue;
    return value * (1.0 / int.MaxValue);
}

double Gaussian(Random random) {
    double u1 = 1.0 - Sample(random);
    double u2 = Sample(random);
    return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
}

void Add(List<LiveEvent> events, double time, string target, string operation, string? key = null, string? text = null, string? state = null, double value = 0, double total = 0) {
    events.Add(new LiveEvent(target, operation, key, text, state, value, total, Math.Max(1, (long)Math.Round(time))));
}

// Layout: retain the worker lanes, result split, rate chart, duration histogram and category findings.
var events = Generate();
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], CultureInfo.InvariantCulture);
if (step < 0 || step > 11) throw new ArgumentOutOfRangeException(nameof(step));
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
long end = events[^1].Time;
long cutoff = step switch { 0 => 1, 1 => 4000, 11 => end, _ => 6800 + (end - 11300) * (step - 2) / 8 };
Phases phases = new Phases().Set("Discover").Set("Collect").Set("Evaluate").Set("Report");
StackedBar results = new StackedBar("results").Add("Passed", 0, ThemeToken.Pass)
    .Add("Warnings", 0, ThemeToken.Warn).Add("Failed", 0, ThemeToken.Fail).Add("Skipped", 0, ThemeToken.Skip);
KeyValueList summary = new KeyValueList("summary").Add("Evaluated", "0 / " + TotalRules)
    .Add("Average rate", "-").Add("Remaining", "-").Add("Targets done", "0 / 12").Add("Targets failing", "0");
LineChart throughput = new LineChart("throughput") { Height = 7 };
throughput.AddSeries("completed", ThemeToken.Accent);
throughput.AddSeries("failed", ThemeToken.Fail);
Histogram durations = new Histogram(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 }, "durations") { Height = 6, Unit = "s" };
BarChart findings = new BarChart("findings") { BarToken = ThemeToken.Fail };
foreach (string category in new[] { "Domain controllers", "Replication", "DNS", "Kerberos", "Security", "Group Policy", "Accounts", "PKI" }) findings.Add(category, 0);
Lanes lanes = new Lanes { MaxLabelWidth = 28 };
LogRegion log = new LogRegion { PreferredHeight = 6 };
Rows body = new Rows(new Padding(phases, 1, 0, 1, 1),
    new Columns().Add(new Panel(lanes, "Targets") { Caption = "6 workers" }, LayoutSize.Fraction(2.3))
        .Add(new Panel(new Rows(results, new Rule(), summary), "Results")),
    new Columns().Add(new Panel(throughput, "Throughput") { Caption = "rules/s" }, LayoutSize.Fraction(1.2))
        .Add(new Panel(durations, "Rule duration")).Add(new Panel(findings, "Failures by category")))
    .Add(new Panel(log, "Log"), LayoutSize.Fraction());
Dashboard dashboard = new Dashboard("[bold]Nightly checks[/]", body) {
    Subtitle = "ad.adatum.com forest assessment · 12 targets · 332 rules", ShowClock = false,
    Footer = "[muted]Discover · Collect · Evaluate · Report    Ctrl+C stop[/]"
};

// Behaviour: a snapshot applies the same timestamped updates as the bounded native live run.
if (snapshot) {
    var targets = new ILiveTarget[] { dashboard, phases, results, summary, throughput, durations, findings, lanes, log }
        .ToDictionary(target => target.Name);
    foreach (LiveEvent update in events.Where(update => update.Time <= cutoff)) targets[update.Target].Apply(update);
    Terminal.Write(dashboard);
    return;
}
using LiveSession session = new LiveSession(dashboard);
session.Run(live => {
    long previous = 0;
    foreach (LiveEvent update in events) {
        int delay = (int)((update.Time - previous) / 4);
        if (live.Cancellation.WaitHandle.WaitOne(delay)) break;
        live.Post(update);
        previous = update.Time;
    }
});

record SyntheticRule(string Category, string Name, string FailReason, string WarnReason, double Weight = 1);
