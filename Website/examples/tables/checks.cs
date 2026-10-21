using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Table table = new Table()
    .WithTitle("[bold]evotec.xyz[/] [muted]· mail security checks[/]")
    .AddColumn("Check", c => c.WithIsKey().WithOverflow(ColumnOverflow.Never))
    .AddColumn("Status", c => c.WithPriority(10).WithPresenter(CellPresenters.Status))
    .AddColumn("Detail", c => c
        .WithPriority(5)
        .WithOverflow(ColumnOverflow.Wrap)
        .WithMaxLines(2)
        .WithFlexible()
        .WithMinWidth(18))
    .AddColumn("Took", c => c.WithPriority(1).WithPresenter(CellPresenters.Duration))
    .AddColumn("RFC", c => c.WithPriority(0).WithOverflow(ColumnOverflow.Never));
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