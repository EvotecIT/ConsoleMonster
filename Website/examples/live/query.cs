using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// DNS and mail-security query results, printed once: no live view. A summary, the records as a typed table, the
// delegation as a tree, the checks as a status list and the resolver latency as bars.
DnsRecord[] records = {
    new("evotec.xyz", "A", "300", "104.21.48.12"),
    // <more>
    new("evotec.xyz", "A", "300", "172.67.161.94"),
    new("evotec.xyz", "AAAA", "300", "2606:4700:3036::6815:300c"),
    new("evotec.xyz", "MX", "3600", "10 evotec-xyz.mail.protection.outlook.com"),
    new("evotec.xyz", "NS", "86400", "dana.ns.cloudflare.com"),
    new("evotec.xyz", "NS", "86400", "kirk.ns.cloudflare.com"),
    new("evotec.xyz", "TXT", "3600", "v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net ip4:51.38.120.9 -all"),
    new("_dmarc.evotec.xyz", "TXT", "3600", "v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; ruf=mailto:forensic@evotec.xyz; fo=1"),
    new("evotec.xyz", "CAA", "3600", "0 issue \"letsencrypt.org\""),
    new("evotec.xyz", "SOA", "3600", "dana.ns.cloudflare.com. dns.cloudflare.com. 2351234567 10000 2400 604800 3600")
    // </more>
};

KeyValueList summary = new KeyValueList()
    .Add("Domain", "[bold]evotec.xyz[/]")
    .Add("Resolver", "Cloudflare DNS over HTTPS [muted](1.1.1.1)[/]")
    .Add("Query time", "38 ms [muted]for 7 record types[/]")
    .Add("Records", "14")
    .Add("DNSSEC", "not signed", StatusKind.Warn);

Table table = Table.For(records)
    .Expand()
    .HeaderSeparator(false)
    .Column(r => r.Name, c => c.Wrap(false))
    .Column(r => r.Type, c => c.Wrap(false))
    .Column(r => r.Ttl, c => c.Header("TTL").Alignment(HorizontalAlignment.Right))
    .Column(r => r.Data, c => c.Flexible());

Tree delegation = new Tree("[bold].[/] [muted]root[/]");
TreeNode xyz = delegation.Add("[bold]xyz.[/]", detail: "6 name servers");
xyz.Add("x.nic.xyz", detail: "194.169.218.42");
xyz.Add("y.nic.xyz", detail: "185.24.64.42");
TreeNode evotec = xyz.Add("[bold]evotec.xyz.[/]", detail: "2 name servers");
evotec.Add("dana.ns.cloudflare.com", StatusKind.Pass, "108.162.192.103 · 12 ms");
evotec.Add("kirk.ns.cloudflare.com", StatusKind.Pass, "172.64.33.149 · 14 ms");
evotec.Add(
    "mail",
    detail: "MX target"
)
    .Add("evotec-xyz.mail.protection.outlook.com", StatusKind.Pass, "52.101.68.17");

StatusList checks = new StatusList()
    .Add("SPF record present and valid", StatusKind.Pass, "9 lookups")
    .Add("DMARC policy enforced", StatusKind.Pass, "p=reject")
    .Add("DKIM selector1 / selector2", StatusKind.Pass, "2048-bit")
    .Add("MTA-STS policy", StatusKind.Warn, "not published")
    .Add("TLS-RPT reporting", StatusKind.Warn, "not published")
    .Add("DNSSEC chain of trust", StatusKind.Fail, "zone not signed")
    .Add("BIMI record", StatusKind.Skip, "optional");

BarChart latency = new BarChart("latency")
    .Unit(" ms")
    .Add("Cloudflare", 12)
    .Add("Google", 18)
    .Add("Quad9", 24)
    .Add("OpenDNS", 31)
    .Add("System resolver", 47);

Terminal.Write(new Rows()
    .Gap(1)
    .Add(new Rule("[bold]DNS query results[/] [muted]· evotec.xyz[/]"))
    .Add(new Padding(summary, 1, 0, 1, 0))
    .Add(table)
    .Add(new Columns(
        new Panel(delegation, "Delegation"),
        new Panel(new Rows(checks, new Rule(), latency), "Mail security").HeaderRight("evotec.xyz"))));

record DnsRecord(string Name, string Type, string Ttl, string Data);
