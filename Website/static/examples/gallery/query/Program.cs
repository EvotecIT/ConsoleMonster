// DNS and mail security: summary, wrapped records, delegation and resolver latency.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: reserved synthetic names and documentation addresses. No DNS requests run.
string domain = "example.test";
var records = new[] {
    new { Name = domain, Type = "A", TTL = 300, Data = "192.0.2.12" },
    new { Name = domain, Type = "A", TTL = 300, Data = "198.51.100.94" },
    new { Name = domain, Type = "AAAA", TTL = 300, Data = "2001:db8::12" },
    new { Name = domain, Type = "MX", TTL = 3600, Data = "10 mail.example.test" },
    new { Name = domain, Type = "NS", TTL = 86400, Data = "ns1.example.test" },
    new { Name = domain, Type = "NS", TTL = 86400, Data = "ns2.example.test" },
    new { Name = domain, Type = "TXT", TTL = 3600, Data = "v=spf1 include:spf.example.test include:relay.example.test ip4:203.0.113.9 -all" },
    new { Name = "_dmarc." + domain, Type = "TXT", TTL = 3600, Data = "v=DMARC1; p=reject; rua=mailto:dmarc@example.test; ruf=mailto:forensic@example.test; fo=1" },
    new { Name = domain, Type = "CAA", TTL = 3600, Data = "0 issue \"ca.example.test\"" },
    new { Name = domain, Type = "SOA", TTL = 3600, Data = "ns1.example.test. hostmaster.example.test. 2026102401 10000 2400 604800 3600" }
};
var checks = new[] {
    ("SPF record present and valid", StatusKind.Pass, "9 lookups"),
    ("DMARC policy enforced", StatusKind.Pass, "p=reject"),
    ("DKIM selector1 / selector2", StatusKind.Pass, "2048-bit"),
    ("MTA-STS policy", StatusKind.Warn, "not published"),
    ("TLS-RPT reporting", StatusKind.Warn, "not published"),
    ("DNSSEC chain of trust", StatusKind.Fail, "zone not signed"),
    ("BIMI record", StatusKind.Skip, "optional")
};
var resolvers = new[] { ("Edge west", 12), ("Edge east", 18), ("Cache north", 24), ("Cache south", 31), ("Local resolver", 47) };

// Layout: keep the dense report readable through wrapping and two balanced panels.
KeyValueList summary = new KeyValueList()
    .Add("Domain", "[bold]example.test[/]")
    .Add("Resolver", "Edge west DNS over HTTPS [muted](192.0.2.53)[/]")
    .Add("Query time", "38 ms [muted]for 7 record types[/]")
    .Add("Records", "10")
    .Add("DNSSEC", "not signed", StatusKind.Warn);
Table table = Table.For(records).WithExpand().WithHeaderSeparator(false)
    .Column(r => r.Name).Column(r => r.Type)
    .Column(r => r.TTL, c => c.WithHeader("TTL").WithAlignment(HorizontalAlignment.Right))
    .Column(r => r.Data, c => c.WithFlexible());
Tree delegation = new Tree("[bold].[/] [muted]root[/]");
TreeNode test = delegation.Add("[bold]test.[/]", null, "reserved namespace");
test.Add("ns.test", null, "192.0.2.42");
TreeNode zone = test.Add("[bold]example.test.[/]", null, "2 name servers");
zone.Add("ns1.example.test", StatusKind.Pass, "198.51.100.103 · 12 ms");
zone.Add("ns2.example.test", StatusKind.Pass, "203.0.113.149 · 14 ms");
zone.Add("mail", null, "MX target").Add("mail.example.test", StatusKind.Pass, "192.0.2.17");
StatusList mail = new StatusList();
foreach (var (label, status, detail) in checks) mail.Add(label, status, detail);
BarChart latency = new BarChart("latency") { Unit = " ms" };
foreach (var (label, value) in resolvers) latency.Add(label, value);
IWidget report = new Rows(
    new Rule("[bold]DNS query results[/] [muted]· example.test[/]"),
    new Padding(summary, 1, 0, 1, 0), table,
    new Columns(new Panel(delegation, "Delegation"),
        new Panel(new Rows(mail, new Rule(), latency) { Gap = 0 }, "Mail security") { HeaderRight = domain })) { Gap = 1 };

// Behaviour: print the report once to the terminal or redirected output.
Terminal.Write(report);
