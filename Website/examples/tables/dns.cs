using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

const string Cache = "/var/cache/dns/evotec.xyz/";
Table table = new Table()
    .WithTitle("[bold]evotec.xyz[/] [muted]· DNS records via Cloudflare DoH[/]")
    .AddColumn("Name", c => c
        .WithIsKey()
        .WithOverflow(ColumnOverflow.TruncateMiddle)
        .WithMinWidth(10))
    .AddColumn("Type", c => c.WithPriority(9).WithOverflow(ColumnOverflow.Never))
    .AddColumn("TTL", c => c.WithPriority(2).WithPresenter(new DurationPresenter()))
    .AddColumn("Data", c => c
        .WithPriority(10)
        .WithOverflow(ColumnOverflow.Wrap)
        .WithMaxLines(3)
        .WithFlexible()
        .WithMinWidth(28))
    .AddColumn("Latency", c => c
        .WithPriority(3)
        .WithPresenter(new NumberPresenter("ms"))
        .AddToneRules(ToneRule.Above(100, ThemeToken.Warn)))
    .AddColumn("Cache file", c => c
        .WithPriority(1)
        .WithOverflow(ColumnOverflow.TruncateMiddle)
        .WithMinWidth(20)
        .WithMaxWidth(36));
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