// DNS and mail security: summary, wrapped records, delegation and resolver latency.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: reserved synthetic names and documentation addresses. No DNS requests run.
string domain = "example.test";
var records = new QueryRecord[]
{
    new(domain, "A", 300, "192.0.2.12"),
    new(domain, "A", 300, "198.51.100.94"),
    new(domain, "AAAA", 300, "2001:db8::12"),
    new(domain, "MX", 3600, "10 mail.example.test"),
    new(domain, "NS", 86400, "ns1.example.test"),
    new(domain, "NS", 86400, "ns2.example.test"),
    new(domain, "TXT", 3600,
        "v=spf1 include:spf.example.test include:relay.example.test ip4:203.0.113.9 -all"),
    new("_dmarc." + domain, "TXT", 3600,
        "v=DMARC1; p=reject; rua=mailto:dmarc@example.test; ruf=mailto:forensic@example.test; fo=1"),
    new(domain, "CAA", 3600, "0 issue \"ca.example.test\""),
    new(domain, "SOA", 3600,
        "ns1.example.test. hostmaster.example.test. 2026102401 10000 2400 604800 3600")
};
var checks = new CheckRow[]
{
    new("SPF record present and valid", StatusKind.Pass, "9 lookups"),
    new("DMARC policy enforced", StatusKind.Pass, "p=reject"),
    new("DKIM selector1 / selector2", StatusKind.Pass, "2048-bit"),
    new("MTA-STS policy", StatusKind.Warn, "not published"),
    new("TLS-RPT reporting", StatusKind.Warn, "not published"),
    new("DNSSEC chain of trust", StatusKind.Fail, "zone not signed"),
    new("BIMI record", StatusKind.Skip, "optional")
};
var resolvers = new ResolverRow[]
{
    new("Edge west", 12),
    new("Edge east", 18),
    new("Cache north", 24),
    new("Cache south", 31),
    new("Local resolver", 47)
};

// Layout: keep the dense report readable through wrapping and two balanced panels.
KeyValueList summary = new KeyValueList()
    .Add("Domain", "[bold]example.test[/]")
    .Add("Resolver", "Edge west DNS over HTTPS [muted](192.0.2.53)[/]")
    .Add("Query time", "38 ms [muted]for 7 record types[/]")
    .Add("Records", "10")
    .Add("DNSSEC", "not signed", StatusKind.Warn);
var table = Table.For(records)
    .Column(row => row.Name)
    .Column(row => row.Type)
    .Column(row => row.TTL, column =>
    {
        column.Header = "TTL";
        column.Alignment = HorizontalAlignment.Right;
    })
    .Column(row => row.Data, column => column.Flexible = true);
table.Expand = true;
table.HeaderSeparator = false;
Tree delegation = new Tree("[bold].[/] [muted]root[/]");
TreeNode test = delegation.Add("[bold]test.[/]", null, "reserved namespace");
test.Add("ns.test", null, "192.0.2.42");
TreeNode zone = test.Add("[bold]example.test.[/]", null, "2 name servers");
zone.Add("ns1.example.test", StatusKind.Pass, "198.51.100.103 · 12 ms");
zone.Add("ns2.example.test", StatusKind.Pass, "203.0.113.149 · 14 ms");
zone.Add("mail", null, "MX target").Add("mail.example.test", StatusKind.Pass, "192.0.2.17");
StatusList mail = new StatusList();
foreach (var(label, status, detail)in checks)
    mail.Add(label, status, detail);
BarChart latency = new BarChart("latency")
{
    Unit = " ms"
};
foreach (var(label, value)in resolvers)
    latency.Add(label, value);
IWidget report = new Rows(
    new Rule("[bold]DNS query results[/] [muted]· example.test[/]"),
    new Padding() { Bottom = 0, Right = 1, Top = 0, Left = 1, Content = summary },
    table,
    new Columns(
        new Panel() { Title = "Delegation", Content = delegation },
        new Panel()
        {
            Title = "Mail security",
            Content = new Rows(mail, new Rule(), latency) { Gap = 0 },
            HeaderRight = domain
        }))
{
    Gap = 1
};

// Behaviour: print the report once to the terminal or redirected output.
Terminal.Write(report);

record QueryRecord(string Name, string Type, int TTL, string Data);

record CheckRow(string Label, StatusKind Status, string Detail);

record ResolverRow(string Label, int Milliseconds);
