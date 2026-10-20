using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Tables and complex data from four kinds of tool output: DNS records (wrap and middle truncation), mail-security checks
    /// and a certificate record view, an assessment run summary grouped by target type with subtotals and totals, and
    /// build artifacts with bytes and durations. Each negotiates its width: columns hide by priority with a
    /// footer, and narrow widths switch to record cards. Data is synthetic.
    /// </summary>
    public static class TableShowcases {
        /// <summary>The fixed reference time, so relative dates read the same in every capture.</summary>
        public static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 16, 14, 30, 0, TimeSpan.FromHours(2));

        /// <summary>DNS records for a domain, with a long SPF value that wraps and a cache path truncated in the middle.</summary>
        /// <returns>The table.</returns>
        public static Table DnsRecords() {
            Table table = new Table { Title = "[bold]evotec.xyz[/] [muted]· DNS records via Cloudflare DoH[/]" };
            table.Presentation.Now = Now;
            table.AddColumn(new TableColumn("Name") { IsKey = true, Overflow = ColumnOverflow.TruncateMiddle, MinWidth = 10 });
            table.AddColumn(new TableColumn("Type") { Priority = 9, Overflow = ColumnOverflow.Never });
            table.AddColumn(new TableColumn("TTL") { Priority = 2, Presenter = new DurationPresenter() });
            table.AddColumn(new TableColumn("Data") { Priority = 10, Overflow = ColumnOverflow.Wrap, MaxLines = 3, Flexible = true, MinWidth = 28 });
            table.AddColumn(new TableColumn("Latency") { Priority = 3, Presenter = new NumberPresenter("ms"), ToneRules = { ToneRule.Above(100, ThemeToken.Warn) } });
            table.AddColumn(new TableColumn("Cache file") { Priority = 1, Overflow = ColumnOverflow.TruncateMiddle, MinWidth = 20, MaxWidth = 36 });
            const string Cache = @"C:\ProgramData\DnsCache\evotec.xyz\";
            table.AddValues("evotec.xyz", "A", 300, "104.21.48.12", 12, Cache + "A.json");
            table.AddValues("evotec.xyz", "AAAA", 300, "2606:4700:3036::6815:300c", 14, Cache + "AAAA.json");
            table.AddValues("evotec.xyz", "MX", 3600, "10 evotec-xyz.mail.protection.outlook.com", 18, Cache + "MX.json");
            table.AddValues("evotec.xyz", "TXT", 3600, "v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net include:mailgun.org ip4:51.38.120.9 ip4:51.38.120.10 -all", 21, Cache + "TXT.json");
            table.AddValues("_dmarc.evotec.xyz", "TXT", 3600, "v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; ruf=mailto:forensic@evotec.xyz; fo=1", 133, Cache + "_dmarc.TXT.json");
            table.AddValues("selector1._domainkey.evotec.xyz", "CNAME", 3600, "selector1-evotec-xyz._domainkey.evotecpl.onmicrosoft.com", 16, Cache + "selector1._domainkey.CNAME.json");
            table.AddValues("evotec.xyz", "CAA", 3600, "0 issue \"letsencrypt.org\"", 15, Cache + "CAA.json");
            return table;
        }

        /// <summary>Mail-security checks with status presenters, a detail that wraps to two lines and durations.</summary>
        /// <returns>The table.</returns>
        public static Table DomainChecks() {
            Table table = new Table { Title = "[bold]evotec.xyz[/] [muted]· mail security checks[/]" };
            table.AddColumn(new TableColumn("Check") { IsKey = true, Overflow = ColumnOverflow.Never });
            table.AddColumn(new TableColumn("Status") { Priority = 10, Presenter = CellPresenters.Status });
            table.AddColumn(new TableColumn("Detail") { Priority = 5, Overflow = ColumnOverflow.Wrap, MaxLines = 2, Flexible = true, MinWidth = 18 });
            table.AddColumn(new TableColumn("Took") { Priority = 1, Presenter = CellPresenters.Duration });
            table.AddColumn(new TableColumn("RFC") { Priority = 0, Overflow = ColumnOverflow.Never });
            table.AddValues("SPF", StatusKind.Pass, "9 DNS lookups (limit 10); ends with -all", TimeSpan.FromMilliseconds(84), "7208");
            table.AddValues("DMARC", StatusKind.Pass, "p=reject, aggregate and forensic reports configured", TimeSpan.FromMilliseconds(41), "7489");
            table.AddValues("DKIM", StatusKind.Warn, "selector2 key is 1024-bit; selector1 is 2048-bit. Rotate selector2 to a 2048-bit key", TimeSpan.FromMilliseconds(212), "6376");
            table.AddValues("MTA-STS", StatusKind.Fail, "policy file returns 404 at https://mta-sts.evotec.xyz/.well-known/mta-sts.txt", TimeSpan.FromSeconds(1.4), "8461");
            table.AddValues("TLS-RPT", StatusKind.Skip, "not published", TimeSpan.FromMilliseconds(22), "8460");
            table.AddValues("DNSSEC", StatusKind.Warn, "zone is not signed", TimeSpan.FromMilliseconds(95), "4033");
            table.AddValues("BIMI", StatusKind.Info, "no record; optional", TimeSpan.FromMilliseconds(19), null);
            return table;
        }

        /// <summary>The certificate of a host as a record view with sections and nested tables.</summary>
        /// <returns>The record view.</returns>
        public static RecordView Certificate() {
            RecordView view = new RecordView("[bold]mail.evotec.xyz[/]") { Summary = "TLS certificate · ECDSA P-256 · Let's Encrypt" };
            view.Presentation.Now = Now;
            view.Presentation.TimeZone = TimeZoneInfo.Utc;
            view.AddSection("Certificate");
            view.AddField("Subject", "CN=mail.evotec.xyz");
            view.AddField("Issuer", "CN=R11, O=Let's Encrypt, C=US");
            view.AddField("Serial", "04:A1:7F:3C:9E:22:51:0B:6D:8E:F0:13:44:2A:BC:91");
            view.AddField("Valid from (UTC)", new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero), new DatePresenter(DateDisplay.Both));
            view.AddField("Valid to (UTC)", new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero), new DatePresenter(DateDisplay.Both), new[] { ToneRule.Below(0, ThemeToken.Fail), ToneRule.Below(30, ThemeToken.Warn) });
            view.AddField("Key", "ECDSA P-256");
            view.AddField("OCSP stapling", true);
            view.AddField("Revoked", false, new BooleanPresenter(trueIsProblem: true));
            view.AddField("Notes", (object?)null);
            view.AddGroup("Subject alternative names", "3");
            Table sans = new Table { Border = TableBorder.None, ShowHeader = false, RecordViewRows = 0 };
            sans.AddColumn("Name");
            sans.AddValues("mail.evotec.xyz");
            sans.AddValues("autodiscover.evotec.xyz");
            sans.AddValues("mta-sts.evotec.xyz");
            view.AddWidget(sans);
            view.AddGroup("Chain", "3 certificates");
            Table chain = new Table { Border = TableBorder.Minimal, RecordViewRows = 0 };
            chain.Presentation.Now = Now;
            chain.Presentation.TimeZone = TimeZoneInfo.Utc;
            chain.AddColumn(new TableColumn("Subject") { IsKey = true });
            chain.AddColumn(new TableColumn("Expires") { Presenter = new DatePresenter(DateDisplay.Relative), ToneRules = { ToneRule.Below(30, ThemeToken.Warn) } });
            chain.AddColumn(new TableColumn("Key") { Priority = 1 });
            chain.AddValues("CN=mail.evotec.xyz", new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero), "ECDSA P-256");
            chain.AddValues("CN=R11, O=Let's Encrypt", new DateTimeOffset(2027, 3, 12, 23, 59, 59, TimeSpan.Zero), "RSA 2048");
            chain.AddValues("CN=ISRG Root X1", new DateTimeOffset(2035, 6, 4, 11, 4, 38, TimeSpan.Zero), "RSA 4096");
            view.AddWidget(chain);
            return view;
        }

        /// <summary>An assessment run summary grouped by target type, with subtotals, a totals row and a score bar.</summary>
        /// <returns>The table.</returns>
        public static Table AssessmentSummary() {
            Table table = new Table { Title = "[bold]Nightly checks[/] [muted]· run summary[/]", ShowSubtotals = true, ShowTotals = true };
            table.AddColumn(new TableColumn("Target") { IsKey = true, Overflow = ColumnOverflow.TruncateMiddle, MinWidth = 12 });
            table.AddColumn(new TableColumn("Result") { Priority = 10, Presenter = CellPresenters.Status });
            table.AddColumn(new TableColumn("Passed") { Priority = 8, Presenter = CellPresenters.Number, Aggregate = ColumnAggregate.Sum });
            table.AddColumn(new TableColumn("Failed") { Priority = 9, Presenter = CellPresenters.Number, Aggregate = ColumnAggregate.Sum, ToneRules = { ToneRule.Above(0, ThemeToken.Fail) } });
            table.AddColumn(new TableColumn("Warnings") { Priority = 6, Presenter = CellPresenters.Number, Aggregate = ColumnAggregate.Sum, ToneRules = { ToneRule.Above(0, ThemeToken.Warn) } });
            table.AddColumn(new TableColumn("Duration") { Priority = 4, Presenter = CellPresenters.Duration, Aggregate = ColumnAggregate.Sum });
            table.AddColumn(new TableColumn("Score") { Priority = 2, Presenter = new DataBarPresenter(100, new NumberPresenter("%")), Aggregate = ColumnAggregate.Average });
            table.AddGroup("Domains");
            table.AddValues("ad.evotec.xyz", StatusKind.Fail, 9, 2, 5, TimeSpan.FromSeconds(24), 56);
            table.AddValues("eu.ad.evotec.xyz", StatusKind.Fail, 13, 1, 1, TimeSpan.FromSeconds(16), 87);
            table.AddGroup("Domain controllers");
            table.AddValues("DC01.ad.evotec.xyz", StatusKind.Warn, 26, 0, 3, TimeSpan.FromSeconds(17), 90);
            table.AddValues("DC02.ad.evotec.xyz", StatusKind.Warn, 23, 0, 6, TimeSpan.FromSeconds(17), 79);
            table.AddValues("DC03.ad.evotec.xyz", StatusKind.Fail, 23, 3, 4, TimeSpan.FromSeconds(19), 77);
            table.AddValues("DC07.eu.ad.evotec.xyz", StatusKind.Fail, 21, 4, 4, TimeSpan.FromSeconds(16), 72);
            table.AddValues("DC09.eu.ad.evotec.xyz", StatusKind.Pass, 30, 0, 0, TimeSpan.FromSeconds(15), 100);
            return table;
        }

        /// <summary>Build artifacts with sizes, build times, relative dates, paths and hashes.</summary>
        /// <returns>The table.</returns>
        public static Table PublishArtifacts() {
            Table table = new Table { Title = "[bold]Build artifacts[/] [muted]· ConsoleMonster 2.0.0[/]", ShowTotals = true, MaxRows = 5, TotalRowCount = 1234 };
            table.Presentation.Now = Now;
            table.AddColumn(new TableColumn("Artifact") { IsKey = true, Overflow = ColumnOverflow.Never });
            table.AddColumn(new TableColumn("Kind") { Priority = 3 });
            table.AddColumn(new TableColumn("Size") { Priority = 9, Presenter = CellPresenters.Bytes, Aggregate = ColumnAggregate.Sum });
            table.AddColumn(new TableColumn("Built in") { Priority = 6, Presenter = CellPresenters.Duration, Aggregate = ColumnAggregate.Sum });
            table.AddColumn(new TableColumn("Created") { Priority = 4, Presenter = new DatePresenter(DateDisplay.Relative) });
            table.AddColumn(new TableColumn("Path") { Priority = 1, Overflow = ColumnOverflow.TruncateMiddle, MinWidth = 20, MaxWidth = 40 });
            table.AddColumn(new TableColumn("SHA256") { Priority = 0, Overflow = ColumnOverflow.Truncate, MaxWidth = 14 });
            const string Root = @"C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\";
            table.AddValues("ConsoleMonster.2.0.0.nupkg", "NuGet", 1_482_113L, TimeSpan.FromSeconds(48.2), Now.AddMinutes(-12), Root + "ConsoleMonster.2.0.0.nupkg", "9f2c4e81a7d03b6e");
            table.AddValues("ConsoleMonster.2.0.0.snupkg", "Symbols", 402_331L, TimeSpan.FromSeconds(3.1), Now.AddMinutes(-12), Root + "ConsoleMonster.2.0.0.snupkg", "41ab09cd77e2f310");
            table.AddValues("ConsoleMonster-2.0.0.zip", "Module", 2_904_576L, TimeSpan.FromSeconds(11.7), Now.AddMinutes(-11), Root + "ConsoleMonster-2.0.0.zip", "c0ffee4251de9a07");
            table.AddValues("ConsoleMonster.PowerShell.dll", "Binary", 188_416L, TimeSpan.FromSeconds(22.9), Now.AddHours(-3), @"C:\Support\GitHub\ConsoleMonster\Sources\ConsoleMonster.PowerShell\bin\Release\net8.0\ConsoleMonster.PowerShell.dll", "77aa13f9b2c46e5d");
            table.AddValues("en-US\\ConsoleMonster-help.xml", "Help", 96_210L, TimeSpan.FromSeconds(6.4), Now.AddDays(-2), Root + @"en-US\ConsoleMonster-help.xml", "0be4d29c6a1f8873");
            table.AddValues("Docs.zip", "Docs", 51_302L, TimeSpan.FromSeconds(1.2), Now.AddDays(-9), Root + "Docs.zip", "5d6e7f8091a2b3c4");
            return table;
        }

        /// <summary>Selected showcases stacked: a comma-separated list of dns, checks, certificate, summary and artifacts.</summary>
        /// <param name="names">The section names.</param>
        /// <returns>The widget tree.</returns>
        public static IWidget Sections(string names) {
            System.Collections.Generic.List<IWidget> widgets = new System.Collections.Generic.List<IWidget>();
            foreach (string name in (names ?? string.Empty).Split(',')) {
                switch (name.Trim().ToLowerInvariant()) {
                    case "dns":
                        widgets.Add(DnsRecords());
                        break;
                    case "checks":
                        widgets.Add(DomainChecks());
                        break;
                    case "certificate":
                        widgets.Add(Certificate());
                        break;
                    case "summary":
                        widgets.Add(AssessmentSummary());
                        break;
                    case "artifacts":
                        widgets.Add(PublishArtifacts());
                        break;
                }
            }
            return new Rows(widgets.ToArray()) { Gap = 1 };
        }

        /// <summary>All showcases stacked, for one width.</summary>
        /// <returns>The widget tree.</returns>
        public static IWidget All() {
            return new Rows(DnsRecords(), DomainChecks(), Certificate(), AssessmentSummary(), PublishArtifacts()) { Gap = 1 };
        }
    }
}
