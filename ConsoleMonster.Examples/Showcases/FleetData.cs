using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>A server in the fleet health check example (invented data).</summary>
    public sealed class FleetServer {
        /// <summary>Creates a server.</summary>
        public FleetServer(string name, string domain, string site, string role, string address, string? offline = null) {
            Name = name;
            Domain = domain;
            Site = site;
            Role = role;
            Address = address;
            Offline = offline;
        }

        /// <summary>The host name.</summary>
        public string Name { get; }

        /// <summary>The domain the server belongs to.</summary>
        public string Domain { get; }

        /// <summary>The site.</summary>
        public string Site { get; }

        /// <summary>The role (Domain controller, File, Web, Database).</summary>
        public string Role { get; }

        /// <summary>The IP address.</summary>
        public string Address { get; }

        /// <summary>Why the server cannot be checked, or null.</summary>
        public string? Offline { get; }

        /// <summary>How much slower than average its checks run (database servers and the remote site are slower).</summary>
        public double Slowness => (Role == "Database" ? 1.5 : 1.0) * (Site == "Berlin" ? 1.3 : 1.0);

        /// <inheritdoc />
        public override string ToString() => Name;
    }

    /// <summary>A health check in the fleet health check example.</summary>
    public sealed class FleetCheck {
        /// <summary>Creates a check.</summary>
        public FleetCheck(string id, string name, string category, string description, string expected, double seconds) {
            Id = id;
            Name = name;
            Category = category;
            Description = description;
            Expected = expected;
            Seconds = seconds;
        }

        /// <summary>The parameter value.</summary>
        public string Id { get; }

        /// <summary>The display name.</summary>
        public string Name { get; }

        /// <summary>The category (Identity, DNS, Certificates, Updates, Storage, Services).</summary>
        public string Category { get; }

        /// <summary>What the check looks at.</summary>
        public string Description { get; }

        /// <summary>The value a healthy server has.</summary>
        public string Expected { get; }

        /// <summary>Typical seconds per server.</summary>
        public double Seconds { get; }

        /// <inheritdoc />
        public override string ToString() => Id;
    }

    /// <summary>A saved combination of servers, checks and options in the fleet health check example.</summary>
    public sealed class FleetPreset {
        /// <summary>Creates a preset.</summary>
        public FleetPreset(string name, string schedule, string scope, Func<FleetServer, bool> servers, IReadOnlyList<string> checks, int parallel, string format, bool notify) {
            Name = name;
            Schedule = schedule;
            Scope = scope;
            Servers = servers;
            Checks = checks;
            Parallel = parallel;
            Format = format;
            Notify = notify;
        }

        /// <summary>The display name.</summary>
        public string Name { get; }

        /// <summary>When it runs on its own.</summary>
        public string Schedule { get; }

        /// <summary>The servers it covers, in words.</summary>
        public string Scope { get; }

        /// <summary>Which servers of the chosen domain it selects.</summary>
        public Func<FleetServer, bool> Servers { get; }

        /// <summary>The check ids it runs.</summary>
        public IReadOnlyList<string> Checks { get; }

        /// <summary>Servers checked at the same time.</summary>
        public int Parallel { get; }

        /// <summary>The report format.</summary>
        public string Format { get; }

        /// <summary>Whether the report is emailed.</summary>
        public bool Notify { get; }

        /// <summary>
        /// The New check form's values the preset sets: checks and options, and every online server it covers in any
        /// domain (the form chooses the ones of the chosen domain once its servers load).
        /// </summary>
        /// <returns>Values by field name.</returns>
        public IDictionary<string, object?> Values() {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                ["Servers"] = FleetData.Servers.Where(s => s.Offline == null && Servers(s)).Select(s => (object?)s.Name).ToList(),
                ["Checks"] = Checks.Cast<object?>().ToList(),
                ["Parallel"] = (decimal)Parallel,
                ["Format"] = Format,
                ["Notify"] = Notify
            };
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }

    /// <summary>The result one check gives on one server, when it is not a plain pass.</summary>
    public sealed class FleetOutcome {
        /// <summary>Creates an outcome.</summary>
        public FleetOutcome(StatusKind status, string actual) {
            Status = status;
            Actual = actual;
        }

        /// <summary>Warn or Fail.</summary>
        public StatusKind Status { get; }

        /// <summary>What the server reported.</summary>
        public string Actual { get; }
    }

    /// <summary>The invented domains, servers, checks, presets and outcomes of the fleet health check example.</summary>
    public static class FleetData {
        /// <summary>The fixed wall clock of the example, so every recording shows the same date and time.</summary>
        public static DateTime Now { get; } = new DateTime(2026, 10, 20, 9, 30, 0);

        /// <summary>The fixed clock as the header shows it.</summary>
        public static string NowText => Now.ToString("ddd d MMM yyyy · HH:mm", CultureInfo.InvariantCulture);

        /// <summary>The domains, with the sites they span.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Domains { get; } = new[] {
            new KeyValuePair<string, string>("corp.contoso.com", "Warsaw, Dublin, Berlin"),
            new KeyValuePair<string, string>("fabrikam.net", "Oslo, Madrid"),
            new KeyValuePair<string, string>("lab.adatum.com", "Lab")
        };

        /// <summary>Every server.</summary>
        public static IReadOnlyList<FleetServer> Servers { get; } = new[] {
            new FleetServer("DC01", "corp.contoso.com", "Warsaw", "Domain controller", "10.20.1.11"),
            new FleetServer("DC02", "corp.contoso.com", "Warsaw", "Domain controller", "10.20.1.12"),
            new FleetServer("FS01", "corp.contoso.com", "Warsaw", "File", "10.20.1.21"),
            new FleetServer("SQL01", "corp.contoso.com", "Warsaw", "Database", "10.20.1.31"),
            new FleetServer("WEB01", "corp.contoso.com", "Warsaw", "Web", "10.20.1.41"),
            new FleetServer("DC11", "corp.contoso.com", "Dublin", "Domain controller", "10.30.1.11"),
            new FleetServer("FS11", "corp.contoso.com", "Dublin", "File", "10.30.1.21"),
            new FleetServer("SQL11", "corp.contoso.com", "Dublin", "Database", "10.30.1.31"),
            new FleetServer("WEB11", "corp.contoso.com", "Dublin", "Web", "10.30.1.41"),
            new FleetServer("DC21", "corp.contoso.com", "Berlin", "Domain controller", "10.40.1.11"),
            new FleetServer("WEB21", "corp.contoso.com", "Berlin", "Web", "10.40.1.41"),
            new FleetServer("FS22", "corp.contoso.com", "Berlin", "File", "10.40.1.22", "offline since 07:12"),
            new FleetServer("OSL-DC1", "fabrikam.net", "Oslo", "Domain controller", "172.16.1.10"),
            new FleetServer("OSL-FS1", "fabrikam.net", "Oslo", "File", "172.16.1.20"),
            new FleetServer("OSL-WEB1", "fabrikam.net", "Oslo", "Web", "172.16.1.40"),
            new FleetServer("MAD-DC1", "fabrikam.net", "Madrid", "Domain controller", "172.17.1.10"),
            new FleetServer("MAD-APP1", "fabrikam.net", "Madrid", "Web", "172.17.1.40"),
            new FleetServer("MAD-SQL1", "fabrikam.net", "Madrid", "Database", "172.17.1.30"),
            new FleetServer("LAB-DC1", "lab.adatum.com", "Lab", "Domain controller", "192.168.50.10"),
            new FleetServer("LAB-WEB1", "lab.adatum.com", "Lab", "Web", "192.168.50.40"),
            new FleetServer("LAB-SQL1", "lab.adatum.com", "Lab", "Database", "192.168.50.30"),
            new FleetServer("LAB-TEST1", "lab.adatum.com", "Lab", "File", "192.168.50.90", "in maintenance until Friday")
        };

        /// <summary>Every check, in category order.</summary>
        public static IReadOnlyList<FleetCheck> Checks { get; } = new[] {
            new FleetCheck("SecureChannel", "Secure channel", "Identity", "Tests the computer account's trust with the domain.", "trusted", 1.2),
            new FleetCheck("TimeOffset", "Time offset", "Identity", "Compares the clock with the domain's time source.", "under 1 s", 0.8),
            new FleetCheck("LocalAdmins", "Local admins", "Identity", "Compares local Administrators with the approved list.", "approved list", 2.5),
            new FleetCheck("ForwardRecord", "Forward record", "DNS", "Resolves the host name and compares the A record with the server's address.", "matches IP", 0.6),
            new FleetCheck("ReverseRecord", "Reverse record", "DNS", "Looks up the PTR record of the server's address.", "PTR present", 0.7),
            new FleetCheck("Resolvers", "DNS resolvers", "DNS", "Asks each configured resolver for the domain's SOA record.", "2 of 2", 1.5),
            new FleetCheck("CertExpiry", "Cert expiry", "Certificates", "Finds machine certificates that expire within 30 days.", "30+ days", 1.8),
            new FleetCheck("CertChain", "Cert chain", "Certificates", "Builds each certificate's chain to a trusted root.", "trusted root", 3.5),
            new FleetCheck("MissingUpdates", "Missing updates", "Updates", "Lists security updates released more than 14 days ago and not installed.", "0", 6.5),
            new FleetCheck("PendingRestart", "Pending restart", "Updates", "Reports a restart waiting after updates or role changes.", "no", 1.0),
            new FleetCheck("FreeSpace", "Free space", "Storage", "Measures free space on every fixed volume.", "15% free", 1.3),
            new FleetCheck("VolumeHealth", "Volume health", "Storage", "Reads the health and operational status of every volume.", "healthy", 4.0),
            new FleetCheck("AutoServices", "Auto services", "Services", "Checks that automatic services are running.", "all running", 2.2),
            new FleetCheck("EventErrors", "Event errors", "Services", "Counts System and Application errors in the last 24 hours.", "under 20", 5.0)
        };

        /// <summary>The categories, in display order.</summary>
        public static IReadOnlyList<string> Categories { get; } = new[] { "Identity", "DNS", "Certificates", "Updates", "Storage", "Services" };

        private static readonly string[] NightlyChecks = { "SecureChannel", "TimeOffset", "ForwardRecord", "Resolvers", "CertExpiry", "MissingUpdates", "PendingRestart", "FreeSpace", "AutoServices", "EventErrors" };

        /// <summary>The presets.</summary>
        public static IReadOnlyList<FleetPreset> Presets { get; } = new[] {
            new FleetPreset("Morning · identity & DNS", "weekdays 07:00", "domain controllers", s => s.Role == "Domain controller",
                new[] { "SecureChannel", "TimeOffset", "LocalAdmins", "ForwardRecord", "ReverseRecord", "Resolvers" }, 4, "Html", false),
            new FleetPreset("Nightly · all sites", "every day 02:00", "every online server", s => true, NightlyChecks, 8, "Html", true),
            new FleetPreset("Patch Tuesday · updates", "second Tuesday 22:00", "every online server", s => true,
                new[] { "MissingUpdates", "PendingRestart", "AutoServices", "EventErrors" }, 12, "Csv", true),
            new FleetPreset("Certificates · web tier", "Mondays 06:00", "web servers", s => s.Role == "Web",
                new[] { "CertExpiry", "CertChain", "ForwardRecord" }, 4, "Html", false)
        };

        // Results that are not a plain pass; everything else passes. Curated, so every run tells the same story.
        private static readonly Dictionary<string, FleetOutcome> Outcomes = new Dictionary<string, FleetOutcome>(StringComparer.OrdinalIgnoreCase) {
            ["DC02/TimeOffset"] = new FleetOutcome(StatusKind.Fail, "4.7 s"),
            ["FS11/MissingUpdates"] = new FleetOutcome(StatusKind.Fail, "3 updates"),
            ["SQL11/FreeSpace"] = new FleetOutcome(StatusKind.Fail, "6% on D:"),
            ["WEB21/CertExpiry"] = new FleetOutcome(StatusKind.Fail, "9 days"),
            ["DC11/TimeOffset"] = new FleetOutcome(StatusKind.Warn, "0.9 s"),
            ["DC21/Resolvers"] = new FleetOutcome(StatusKind.Warn, "1 of 2"),
            ["WEB01/CertChain"] = new FleetOutcome(StatusKind.Warn, "intermediate ends in 40 days"),
            ["SQL01/PendingRestart"] = new FleetOutcome(StatusKind.Warn, "since Sunday"),
            ["FS01/EventErrors"] = new FleetOutcome(StatusKind.Warn, "27"),
            ["SQL11/EventErrors"] = new FleetOutcome(StatusKind.Warn, "34"),
            ["WEB11/AutoServices"] = new FleetOutcome(StatusKind.Warn, "1 restarted")
        };

        /// <summary>Finds a check by id.</summary>
        public static FleetCheck Check(string id) => Checks.First(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>Finds a server by name.</summary>
        public static FleetServer Server(string name) => Servers.First(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>What a check reports on a server: null for a pass, otherwise the warning or failure.</summary>
        public static FleetOutcome? Outcome(string server, string check) => Outcomes.TryGetValue(server + "/" + check, out FleetOutcome? outcome) ? outcome : null;

        /// <summary>The domain choices, with the sites and server counts.</summary>
        public static List<Choice> DomainChoices() {
            List<Choice> choices = new List<Choice>();
            foreach (KeyValuePair<string, string> domain in Domains) {
                int count = Servers.Count(s => s.Domain == domain.Key);
                choices.Add(new Choice(domain.Key, domain.Key) {
                    Description = count.ToString(CultureInfo.InvariantCulture) + " servers · " + domain.Value,
                    Badge = RichText.Plain(count.ToString(CultureInfo.InvariantCulture) + " servers", new RichStyle(ThemeToken.Muted))
                });
            }
            return choices;
        }

        /// <summary>
        /// Loads a domain's servers as a real tool would query an inventory: after a pause, grouped by site, offline
        /// servers disabled, and every online server selected (a preset's servers take their place when one is applied).
        /// </summary>
        /// <param name="domain">The domain.</param>
        /// <param name="delay">How long the inventory takes to answer.</param>
        /// <param name="token">Cancels the load.</param>
        /// <returns>The server choices.</returns>
        public static Task<IEnumerable<Choice>> LoadServersAsync(string? domain, TimeSpan delay, CancellationToken token) {
            List<Choice> choices = new List<Choice>();
            foreach (FleetServer server in Servers) {
                if (server.Domain != domain) {
                    continue;
                }
                choices.Add(new Choice(server.Name, server.Name) {
                    Group = server.Site,
                    Badge = RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted)),
                    Description = server.Role + " · " + server.Address,
                    DisabledReason = server.Offline,
                    Selected = server.Offline == null
                });
            }
            if (delay <= TimeSpan.Zero) {
                return Task.FromResult<IEnumerable<Choice>>(choices);
            }
            return Task.Delay(delay, token).ContinueWith(t => (IEnumerable<Choice>)choices, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        /// <summary>The check choices with groups, typical times and descriptions.</summary>
        public static List<Choice> CheckChoices() {
            List<Choice> choices = new List<Choice>();
            foreach (FleetCheck check in Checks) {
                choices.Add(new Choice(check.Id, check.Name) {
                    Group = check.Category,
                    Badge = RichText.Plain("~" + check.Seconds.ToString("0.#", CultureInfo.InvariantCulture) + " s", new RichStyle(ThemeToken.Muted)),
                    Description = check.Description
                });
            }
            return choices;
        }

        /// <summary>The preview of a check: what it looks at and what a healthy server reports.</summary>
        public static IWidget CheckPreview(FleetCheck check) {
            KeyValueList facts = new KeyValueList()
                .Add("Category", Markup.Escape(check.Category))
                .Add("Healthy", "[pass]" + Markup.Escape(check.Expected) + "[/]")
                .Add("Typical", "about " + check.Seconds.ToString("0.#", CultureInfo.InvariantCulture) + " s per server")
                .Add("Parameter", "[accent]-Check " + Markup.Escape(check.Id) + "[/]");
            return new Rows(new TextBlock(Markup.Escape(check.Description)), facts) { Gap = 1 };
        }

        /// <summary>The preview of a preset: its scope, checks and options.</summary>
        public static IWidget PresetPreview(FleetPreset preset) {
            List<string> categories = preset.Checks.Select(id => Check(id).Category).Distinct().ToList();
            KeyValueList facts = new KeyValueList()
                .Add("Servers", Markup.Escape(preset.Scope))
                .Add("Checks", preset.Checks.Count.ToString(CultureInfo.InvariantCulture) + " [muted]· " + Markup.Escape(string.Join(", ", categories)) + "[/]")
                .Add("Workers", preset.Parallel.ToString(CultureInfo.InvariantCulture) + " servers at a time")
                .Add("Report", preset.Format.ToUpperInvariant() + (preset.Notify ? " [muted]· emailed[/]" : string.Empty))
                .Add("Schedule", Markup.Escape(preset.Schedule));
            TextBlock checks = new TextBlock("[muted]" + Markup.Escape(string.Join(" · ", preset.Checks.Select(id => Check(id).Name))) + "[/]");
            return new Rows(facts, checks) { Gap = 1 };
        }
    }
}
