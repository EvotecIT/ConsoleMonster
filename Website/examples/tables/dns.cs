using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

const string Cache = "/var/cache/dns/evotec.xyz/";
Table table = new Table()
{
    Title = "[bold]evotec.xyz[/] [muted]· DNS records via Cloudflare DoH[/]"
}.Column("Name", c =>
{
    c.IsKey = true;
    c.Overflow = ColumnOverflow.TruncateMiddle;
    c.MinWidth = 10;
}).Column("Type", c =>
{
    c.Priority = 9;
    c.Overflow = ColumnOverflow.Never;
}).Column("TTL", c =>
{
    c.Priority = 2;
    c.Presenter = new DurationPresenter();
}).Column("Data", c =>
{
    c.Priority = 10;
    c.Overflow = ColumnOverflow.Wrap;
    c.MaxLines = 3;
    c.Flexible = true;
    c.MinWidth = 28;
}).Column("Latency", c =>
{
    c.Priority = 3;
    c.Presenter = new NumberPresenter("ms");
    c.RoleRule(RoleRule.Above(100, ThemeToken.Warn));
}).Column("Cache file", c =>
{
    c.Priority = 1;
    c.Overflow = ColumnOverflow.TruncateMiddle;
    c.MinWidth = 20;
    c.MaxWidth = 36;
});
table.Values("evotec.xyz", "A", 300, "104.21.48.12", 12, Cache + "A.json");
// <more>
table.Values("evotec.xyz", "AAAA", 300, "2606:4700:3036::6815:300c", 14, Cache + "AAAA.json");
table.Values("evotec.xyz", "MX", 3600, "10 evotec-xyz.mail.protection.outlook.com", 18,
    Cache + "MX.json");
table.Values("evotec.xyz", "TXT", 3600,
    "v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net include:mailgun.org ip4:51.38.120.9 ip4:51.38.120.10 -all",
    21, Cache + "TXT.json");
table.Values("_dmarc.evotec.xyz", "TXT", 3600,
    "v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; ruf=mailto:forensic@evotec.xyz; fo=1", 133,
    Cache + "_dmarc.TXT.json");
table.Values("selector1._domainkey.evotec.xyz", "CNAME", 3600,
    "selector1-evotec-xyz._domainkey.evotecpl.onmicrosoft.com", 16,
    Cache + "selector1._domainkey.CNAME.json");
table.Values("evotec.xyz", "CAA", 3600, "0 issue \"letsencrypt.org\"", 15, Cache + "CAA.json");
// </more>
Terminal.Write(table);
