using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Table table = new Table()
    .Title("[bold]evotec.xyz[/] [muted]· mail security checks[/]")
    .AddColumn("Check", c => c.IsKey().Overflow(ColumnOverflow.Never))
    .AddColumn("Status", c => c.Priority(10).Presenter(CellPresenters.Status))
    .AddColumn("Detail", c => c
        .Priority(5)
        .Overflow(ColumnOverflow.Wrap)
        .MaxLines(2)
        .Flexible()
        .MinWidth(18))
    .AddColumn("Took", c => c.Priority(1).Presenter(CellPresenters.Duration))
    .AddColumn("RFC", c => c.Priority(0).Overflow(ColumnOverflow.Never));
table.AddValues("SPF", StatusKind.Pass, "9 DNS lookups (limit 10); ends with -all", TimeSpan.FromMilliseconds(84), "7208");
// <more>
table.AddValues("DMARC", StatusKind.Pass, "p=reject, aggregate and forensic reports configured", TimeSpan.FromMilliseconds(41), "7489");
table.AddValues("DKIM", StatusKind.Warn, "selector2 key is 1024-bit; selector1 is 2048-bit. Rotate selector2 to a 2048-bit key", TimeSpan.FromMilliseconds(212), "6376");
table.AddValues("MTA-STS", StatusKind.Fail, "policy file returns 404 at https://mta-sts.evotec.xyz/.well-known/mta-sts.txt", TimeSpan.FromSeconds(1.4), "8461");
table.AddValues("TLS-RPT", StatusKind.Skip, "not published", TimeSpan.FromMilliseconds(22), "8460");
table.AddValues("DNSSEC", StatusKind.Warn, "zone is not signed", TimeSpan.FromMilliseconds(95), "4033");
table.AddValues("BIMI", StatusKind.Info, "no record; optional", TimeSpan.FromMilliseconds(19), null);
// </more>
Terminal.Write(table);