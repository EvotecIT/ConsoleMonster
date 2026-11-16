using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Relative dates are measured from a fixed time, so this example prints the same every day.
DateTimeOffset now = new DateTimeOffset(2026, 10, 16, 14, 30, 0, TimeSpan.FromHours(2));
RecordView view = new RecordView("[bold]mail.evotec.xyz[/]")
{
    Summary = "TLS certificate · ECDSA P-256 · Let's Encrypt",
    Presentation = new PresentationOptions
    {
        Now = now,
        TimeZone = TimeZoneInfo.Utc
    }
};
view.Section("Certificate");
view.AddField("Subject", "CN=mail.evotec.xyz");
view.AddField("Issuer", "CN=R11, O=Let's Encrypt, C=US");
view.AddField("Serial", "04:A1:7F:3C:9E:22:51:0B:6D:8E:F0:13:44:2A:BC:91");
view.AddField(
    "Valid from (UTC)",
    new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero),
    new DatePresenter(DateDisplay.Both));
view.AddField(
    "Valid to (UTC)",
    new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero),
    new DatePresenter(DateDisplay.Both),
    new[] { RoleRule.Below(0, ThemeToken.Fail), RoleRule.Below(30, ThemeToken.Warn) });
view.AddField("Key", "ECDSA P-256");
view.AddField("OCSP stapling", true);
view.AddField("Revoked", false, new BooleanPresenter("yes", "no", true));
view.AddField("Notes", (object? )null);
view.Group("Subject alternative names", "3");
view.Widget(new Table() { Border = TableBorder.None, ShowHeader = false, RecordViewRows = 0 }
    .Column("Name")
    .Values("mail.evotec.xyz")
    .Values("autodiscover.evotec.xyz")
    .Values("mta-sts.evotec.xyz"));
view.Group("Chain", "3 certificates");
view.Widget(new Table()
{
    Border = TableBorder.Minimal,
    RecordViewRows = 0,
    Presentation = new PresentationOptions { Now = now, TimeZone = TimeZoneInfo.Utc }
}
    .Column("Subject", c =>
    {
        c.IsKey = true;
    })
    .Column("Expires", c =>
    {
        c.Presenter = new DatePresenter(DateDisplay.Relative);
        c.RoleRule(RoleRule.Below(30, ThemeToken.Warn));
    })
    .Column("Key", c =>
    {
        c.Priority = 1;
    })
    .Values(
        "CN=mail.evotec.xyz",
        new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero),
        "ECDSA P-256")
    .Values(
        "CN=R11, O=Let's Encrypt",
        new DateTimeOffset(2027, 3, 12, 23, 59, 59, TimeSpan.Zero),
        "RSA 2048")
    .Values(
        "CN=ISRG Root X1",
        new DateTimeOffset(2035, 6, 4, 11, 4, 38, TimeSpan.Zero),
        "RSA 4096"));
Terminal.Write(view);
