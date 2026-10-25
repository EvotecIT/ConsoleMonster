using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

const string Cache = "/var/cache/dns/evotec.xyz/";
Table table = new Table()
    .Title("[bold]evotec.xyz[/] [muted]· DNS records via Cloudflare DoH[/]")
    .AddColumn("Name", c => c
        .IsKey()
        .Overflow(ColumnOverflow.TruncateMiddle)
        .MinWidth(10))
    .AddColumn("Type", c => c.Priority(9).Overflow(ColumnOverflow.Never))
    .AddColumn("TTL", c => c.Priority(2).Presenter(new DurationPresenter()))
    .AddColumn("Data", c => c
        .Priority(10)
        .Overflow(ColumnOverflow.Wrap)
        .MaxLines(3)
        .Flexible()
        .MinWidth(28))
    .AddColumn("Latency", c => c
        .Priority(3)
        .Presenter(new NumberPresenter("ms"))
        .AddRoleRules(RoleRule.Above(100, ThemeToken.Warn)))
    .AddColumn("Cache file", c => c
        .Priority(1)
        .Overflow(ColumnOverflow.TruncateMiddle)
        .MinWidth(20)
        .MaxWidth(36));
table.AddValues("evotec.xyz", "A", 300, "104.21.48.12", 12, Cache + "A.json");
// <more>
table.AddValues("evotec.xyz", "AAAA", 300, "2606:4700:3036::6815:300c", 14, Cache + "AAAA.json");
table.AddValues("evotec.xyz", "MX", 3600, "10 evotec-xyz.mail.protection.outlook.com", 18, Cache + "MX.json");
table.AddValues("evotec.xyz", "TXT", 3600, "v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net include:mailgun.org ip4:51.38.120.9 ip4:51.38.120.10 -all", 21, Cache + "TXT.json");
table.AddValues("_dmarc.evotec.xyz", "TXT", 3600, "v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; ruf=mailto:forensic@evotec.xyz; fo=1", 133, Cache + "_dmarc.TXT.json");
table.AddValues("selector1._domainkey.evotec.xyz", "CNAME", 3600, "selector1-evotec-xyz._domainkey.evotecpl.onmicrosoft.com", 16, Cache + "selector1._domainkey.CNAME.json");
table.AddValues("evotec.xyz", "CAA", 3600, "0 issue \"letsencrypt.org\"", 15, Cache + "CAA.json");
// </more>
Terminal.Write(table);