using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Relative dates are measured from a fixed time, so this example prints the same every day.
DateTimeOffset now = new DateTimeOffset(2026, 10, 16, 14, 30, 0, TimeSpan.FromHours(2));
RecordView view = new RecordView("[bold]mail.evotec.xyz[/]")
    .WithSummary("TLS certificate · ECDSA P-256 · Let's Encrypt")
    .WithPresentation(p => { p.Now = now; p.TimeZone = TimeZoneInfo.Utc; });
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
view.AddWidget(new Table()
    .WithBorder(TableBorder.None)
    .WithShowHeader(false)
    .WithRecordViewRows(0)
    .AddColumn("Name")
    .AddValues("mail.evotec.xyz")
    .AddValues("autodiscover.evotec.xyz")
    .AddValues("mta-sts.evotec.xyz"));
view.AddGroup("Chain", "3 certificates");
view.AddWidget(new Table()
    .WithBorder(TableBorder.Minimal)
    .WithRecordViewRows(0)
    .WithPresentation(p => { p.Now = now; p.TimeZone = TimeZoneInfo.Utc; })
    .AddColumn("Subject", c => c.WithIsKey())
    .AddColumn("Expires", c => c
        .WithPresenter(new DatePresenter(DateDisplay.Relative))
        .AddToneRules(ToneRule.Below(30, ThemeToken.Warn)))
    .AddColumn("Key", c => c.WithPriority(1))
    .AddValues("CN=mail.evotec.xyz", new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero), "ECDSA P-256")
    .AddValues("CN=R11, O=Let's Encrypt", new DateTimeOffset(2027, 3, 12, 23, 59, 59, TimeSpan.Zero), "RSA 2048")
    .AddValues("CN=ISRG Root X1", new DateTimeOffset(2035, 6, 4, 11, 4, 38, TimeSpan.Zero), "RSA 4096"));
Terminal.Write(view);