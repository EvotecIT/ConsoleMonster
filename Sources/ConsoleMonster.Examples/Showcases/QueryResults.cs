using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Showcase 3: DnsClientX / DomainDetective style query results printed once (no live view): a summary, the
    /// records as a table, the delegation as a tree, mail-security checks as a status list and resolver latency as bars.
    /// </summary>
    public static class QueryResults {
        /// <summary>Builds the showcase as one widget.</summary>
        /// <returns>The widget tree.</returns>
        public static IWidget Build() {
            KeyValueList summary = new KeyValueList()
                .Add("Domain", "[bold]evotec.xyz[/]")
                .Add("Resolver", "Cloudflare DNS over HTTPS [muted](1.1.1.1)[/]")
                .Add("Query time", "38 ms [muted]for 7 record types[/]")
                .Add("Records", "14")
                .Add("DNSSEC", "not signed", StatusKind.Warn);

            Table records = new Table { Border = TableBorder.Rounded, Expand = true };
            records.AddColumn(new TableColumn("Name") { Wrap = false });
            records.AddColumn(new TableColumn("Type") { Wrap = false });
            records.AddColumn("TTL");
            records.AddColumn(new TableColumn("Data") { Flexible = true });
            records.AddPlainRow("evotec.xyz", "A", "300", "104.21.48.12");
            records.AddPlainRow("evotec.xyz", "A", "300", "172.67.161.94");
            records.AddPlainRow("evotec.xyz", "AAAA", "300", "2606:4700:3036::6815:300c");
            records.AddPlainRow("evotec.xyz", "MX", "3600", "10 evotec-xyz.mail.protection.outlook.com");
            records.AddPlainRow("evotec.xyz", "NS", "86400", "dana.ns.cloudflare.com");
            records.AddPlainRow("evotec.xyz", "NS", "86400", "kirk.ns.cloudflare.com");
            records.AddPlainRow("evotec.xyz", "TXT", "3600", "v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net ip4:51.38.120.9 -all");
            records.AddPlainRow("_dmarc.evotec.xyz", "TXT", "3600", "v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; ruf=mailto:forensic@evotec.xyz; fo=1");
            records.AddPlainRow("evotec.xyz", "CAA", "3600", "0 issue \"letsencrypt.org\"");
            records.AddPlainRow("evotec.xyz", "SOA", "3600", "dana.ns.cloudflare.com. dns.cloudflare.com. 2351234567 10000 2400 604800 3600");

            Tree delegation = new Tree("[bold].[/] [muted]root[/]");
            TreeNode xyz = delegation.Add("[bold]xyz.[/]", null, "6 name servers");
            xyz.Add("x.nic.xyz", null, "194.169.218.42");
            xyz.Add("y.nic.xyz", null, "185.24.64.42");
            TreeNode evotec = xyz.Add("[bold]evotec.xyz.[/]", null, "2 name servers");
            evotec.Add("dana.ns.cloudflare.com", StatusKind.Pass, "108.162.192.103 · 12 ms");
            evotec.Add("kirk.ns.cloudflare.com", StatusKind.Pass, "172.64.33.149 · 14 ms");
            TreeNode mail = evotec.Add("mail", null, "MX target");
            mail.Add("evotec-xyz.mail.protection.outlook.com", StatusKind.Pass, "52.101.68.17");

            StatusList checks = new StatusList()
                .Add("SPF record present and valid", StatusKind.Pass, "9 lookups")
                .Add("DMARC policy enforced", StatusKind.Pass, "p=reject")
                .Add("DKIM selector1 / selector2", StatusKind.Pass, "2048-bit")
                .Add("MTA-STS policy", StatusKind.Warn, "not published")
                .Add("TLS-RPT reporting", StatusKind.Warn, "not published")
                .Add("DNSSEC chain of trust", StatusKind.Fail, "zone not signed")
                .Add("BIMI record", StatusKind.Skip, "optional");

            BarChart latency = new BarChart("latency") { Unit = " ms" }
                .Add("Cloudflare", 12)
                .Add("Google", 18)
                .Add("Quad9", 24)
                .Add("OpenDNS", 31)
                .Add("System resolver", 47);

            Columns lower = new Columns(
                new Panel(delegation, "Delegation"),
                new Panel(new Rows(checks, new Rule(), latency) { Gap = 0 }, "Mail security") { Caption = "DomainDetective" });

            return new Rows(
                new Rule("[bold]DNS query results[/] [muted]· evotec.xyz[/]"),
                new Padding(summary, 1, 0, 1, 0),
                records,
                lower) { Gap = 1 };
        }
    }
}
