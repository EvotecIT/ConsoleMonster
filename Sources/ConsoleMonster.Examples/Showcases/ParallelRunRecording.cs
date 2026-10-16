using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Live;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Generates a deterministic, synthetic event stream for a TestimoX-style parallel assessment: 10 domain controllers
    /// and 2 domains processed by 6 workers, about 330 rules with realistic durations and pass/warn/fail/skip results.
    /// The stream is what a live session records; the showcase replays it.
    /// </summary>
    public static class ParallelRunRecording {
        private static readonly SyntheticRule[] DcRules = {
            new SyntheticRule("Domain controllers", "LDAP signing required"),
            new SyntheticRule("Domain controllers", "LDAP channel binding"),
            new SyntheticRule("Domain controllers", "SMB signing enforced"),
            new SyntheticRule("Domain controllers", "Print Spooler disabled"),
            new SyntheticRule("Domain controllers", "LSA protection enabled"),
            new SyntheticRule("Domain controllers", "Credential Guard posture", 1.4),
            new SyntheticRule("Domain controllers", "Secure Boot certificate rollout"),
            new SyntheticRule("Domain controllers", "Windows Update compliance", 2.2),
            new SyntheticRule("Domain controllers", "Operating system support"),
            new SyntheticRule("Domain controllers", "NTLM audit configuration"),
            new SyntheticRule("Replication", "Replication latency per partner", 2.6),
            new SyntheticRule("Replication", "USN rollback detection", 1.8),
            new SyntheticRule("Replication", "SYSVOL DFSR state", 1.5),
            new SyntheticRule("Replication", "Tombstone lifetime"),
            new SyntheticRule("Replication", "Lingering objects", 2.0),
            new SyntheticRule("DNS", "DNS forwarders consistency"),
            new SyntheticRule("DNS", "DNS scavenging"),
            new SyntheticRule("DNS", "Root hints reachable", 1.6),
            new SyntheticRule("DNS", "DC locator SRV records"),
            new SyntheticRule("Time", "Time source hierarchy"),
            new SyntheticRule("Time", "Clock skew"),
            new SyntheticRule("Kerberos", "Kerberos AES-only encryption"),
            new SyntheticRule("Kerberos", "krbtgt password age"),
            new SyntheticRule("Kerberos", "Unconstrained delegation"),
            new SyntheticRule("Security", "Audit policy baseline", 1.3),
            new SyntheticRule("Security", "Event log sizes"),
            new SyntheticRule("Security", "Local administrators"),
            new SyntheticRule("Security", "Firewall profile"),
            new SyntheticRule("Security", "Remote Desktop exposure"),
            new SyntheticRule("Security", "PowerShell logging")
        };

        private static readonly SyntheticRule[] DomainRules = {
            new SyntheticRule("Group Policy", "GPO permissions baseline", 2.4),
            new SyntheticRule("Group Policy", "Orphaned GPO folders"),
            new SyntheticRule("Group Policy", "Empty GPOs"),
            new SyntheticRule("Group Policy", "LAPS policy"),
            new SyntheticRule("Accounts", "Stale computer accounts", 1.8),
            new SyntheticRule("Accounts", "Password never expires"),
            new SyntheticRule("Accounts", "Privileged group membership", 1.5),
            new SyntheticRule("Accounts", "AdminSDHolder protected objects"),
            new SyntheticRule("Accounts", "Service account SPN hygiene", 1.4),
            new SyntheticRule("PKI", "CA weak key size"),
            new SyntheticRule("PKI", "Certificate template exposure", 1.7),
            new SyntheticRule("Kerberos", "Constrained delegation overview"),
            new SyntheticRule("Forest", "Recycle Bin enabled"),
            new SyntheticRule("Forest", "Site link costs"),
            new SyntheticRule("Forest", "Subnets without sites"),
            new SyntheticRule("Forest", "Functional level")
        };

        private static readonly string[] FailReasons = {
            "partner DC02 last success 4h ago", "event 2095 present", "value 0 expected 1", "3 accounts affected",
            "baseline drift on 2 settings", "certificate expires in 9 days", "not configured", "12 objects found"
        };

        /// <summary>The number of parallel workers simulated.</summary>
        public const int Workers = 6;

        /// <summary>The targets (lane key, label, rule count).</summary>
        /// <returns>Targets in processing order.</returns>
        public static IReadOnlyList<KeyValuePair<string, string>> Targets() {
            List<KeyValuePair<string, string>> targets = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("dom-ad", "ad.evotec.xyz (domain)"),
                new KeyValuePair<string, string>("dom-eu", "eu.ad.evotec.xyz (domain)")
            };
            for (int i = 1; i <= 10; i++) {
                string dc = "DC" + i.ToString("00", CultureInfo.InvariantCulture) + (i <= 6 ? ".ad.evotec.xyz" : ".eu.ad.evotec.xyz");
                targets.Add(new KeyValuePair<string, string>("dc" + i.ToString(CultureInfo.InvariantCulture), dc));
            }
            return targets;
        }

        /// <summary>Total number of rules in the run.</summary>
        public static int TotalRules => (2 * DomainRules.Length) + (10 * DcRules.Length);

        /// <summary>Generates the event stream ordered by time.</summary>
        /// <param name="seed">Random seed (the same seed gives the same run).</param>
        /// <returns>Events with times in milliseconds.</returns>
        public static List<LiveEvent> Generate(int seed = 2026) {
            Random random = new Random(seed);
            List<LiveEvent> events = new List<LiveEvent>();
            IReadOnlyList<KeyValuePair<string, string>> targets = Targets();

            Add(events, 0, "dashboard", "status", text: "discovering forest", state: "running");
            Add(events, 0, "phases", "state", "Discover", state: "running");
            foreach (KeyValuePair<string, string> target in targets) {
                int rules = target.Key.StartsWith("dom", StringComparison.Ordinal) ? DomainRules.Length : DcRules.Length;
                Add(events, 0, "lanes", "add", target.Key, target.Value, total: rules);
            }
            Add(events, 400, "log", "write", text: "Connecting to ad.evotec.xyz via LDAP (DC01.ad.evotec.xyz)", state: "info");
            Add(events, 1300, "log", "write", text: "Forest ad.evotec.xyz: 2 domains, 10 domain controllers, 4 sites", state: "info");
            Add(events, 2100, "log", "write", text: "DC10.eu.ad.evotec.xyz responded slowly (812 ms LDAP bind)", state: "warn");
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
                    double seconds = Math.Exp(-0.9 + (0.75 * Gaussian(random))) * rule.Weight * (domain ? 1.6 : 1.0);
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
                            if (random.NextDouble() < 0.5) {
                                Add(events, t, "log", "write", text: target.Value + ": " + rule.Name + " - " + FailReasons[random.Next(FailReasons.Length)], state: "warn");
                            }
                            break;
                        case "fail":
                            failed++;
                            laneFailed = true;
                            failures.Add(t);
                            Add(events, t, "results", "add", "Failed", value: 1);
                            Add(events, t, "findings", "add", rule.Category, value: 1);
                            Add(events, t, "log", "write", text: target.Value + ": " + rule.Name + " failed - " + FailReasons[random.Next(FailReasons.Length)], state: "err");
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
            Add(events, evaluateEnd + 900, "log", "write", text: "Report written to Reports\\Assessment-ad.evotec.xyz.html (4.2 MB)", state: "pass");
            Add(events, evaluateEnd + 1800, "phases", "state", "Report", state: "pass");
            string final = done.ToString(CultureInfo.InvariantCulture) + " rules · " + passed.ToString(CultureInfo.InvariantCulture) + " passed · "
                + failed.ToString(CultureInfo.InvariantCulture) + " failed · " + warned.ToString(CultureInfo.InvariantCulture) + " warnings · " + skipped.ToString(CultureInfo.InvariantCulture) + " skipped";
            Add(events, evaluateEnd + 1800, "dashboard", "status", text: final, state: failed > 0 ? "warn" : "pass");
            Add(events, evaluateEnd + 4500, "log", "write", text: "Done.", state: "info");

            // Stable ordering: events with the same timestamp keep the order they were produced in.
            return events.OrderBy(e => e.Time).ToList();
        }

        private static void AddThroughput(List<LiveEvent> events, List<double> completions, List<double> failures, double start, double end) {
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

        private static void AddSummaryUpdates(List<LiveEvent> events, List<double> completions, List<KeyValuePair<double, bool>> targetFinishes, double end) {
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

        private static string PickResult(Random random, bool troubled) {
            double roll = random.NextDouble();
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

        private static double Gaussian(Random random) {
            double u1 = 1.0 - random.NextDouble();
            double u2 = random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static void Add(List<LiveEvent> events, double time, string target, string operation, string? key = null, string? text = null, string? state = null, double value = 0, double total = 0) {
            events.Add(new LiveEvent(target, operation, key, text, state, value, total, Math.Max(1, (long)Math.Round(time))));
        }
    }
}
