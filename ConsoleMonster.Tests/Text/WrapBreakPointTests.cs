using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>
    /// Line breaks at sensible points: after spaces, then after separators (path, list and query separators before the
    /// dots, dashes and colons inside a name), so each line ends with its separator; only then by character.
    /// </summary>
    public sealed class WrapBreakPointTests {
        private const string MxHost = "evotec-xyz.mail.protection.outlook.com";
        private const string SpfIncludes = "include:spf.protection.outlook.com include:_spf.google.com";
        private const string DkimTarget = "selector1-evotec-xyz._domainkey.evotecpl.onmicrosoft.com";
        private const string PackagePath = @"C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster.2.0.0.nupkg";
        private const string PolicyUrl = "https://mta-sts.evotec.xyz/.well-known/mta-sts.txt";
        private const string UsersDn = "CN=Users,DC=ad,DC=evotec,DC=xyz";
        private const string Serial = "04:A1:7F:3C:9E:22:51:0B:6D:8E:F0:13:44:2A:BC:91";
        private const string Query = "https://example.org/report?domain=evotec.xyz&type=dmarc&days=30";

        [Theory]
        [InlineData(MxHost, 33, "evotec-xyz.mail.protection.|outlook.com")]
        [InlineData(SpfIncludes, 33, "include:spf.protection.outlook.|com include:_spf.google.com")]
        [InlineData(DkimTarget, 33, "selector1-evotec-xyz._domainkey.|evotecpl.onmicrosoft.com")]
        [InlineData(PackagePath, 36, @"C:\Support\GitHub\ConsoleMonster\|Artefacts\Packed\|ConsoleMonster.2.0.0.nupkg")]
        [InlineData(PolicyUrl, 30, "https://mta-sts.evotec.xyz/.|well-known/mta-sts.txt")]
        [InlineData(UsersDn, 20, "CN=Users,DC=ad,|DC=evotec,DC=xyz")]
        [InlineData(Serial, 30, "04:A1:7F:3C:9E:22:51:0B:6D:8E:|F0:13:44:2A:BC:91")]
        [InlineData(Query, 32, "https://example.org/report?|domain=evotec.xyz&type=dmarc&|days=30")]
        [InlineData("v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz;", 22, "v=DMARC1; p=reject;|rua=mailto:|dmarc@evotec.xyz;")]
        [InlineData("abcdefghij", 4, "abcd|efgh|ij")]
        public void Wrap_SeparatedText_BreaksAfterSeparators(string text, int width, string expected) {
            string actual = string.Join("|", TextWrapper.Wrap(StyledLine.From(text), width).Select(line => line.PlainText));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Wrap_EveryWrappingWidget_UsesTheSameBreakPoints() {
            string[] values = { MxHost, SpfIncludes, DkimTarget, PackagePath, PolicyUrl };
            TerminalProfile profile = TerminalProfile.Fixed(60, 40, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.InvariantCulture);
            StringBuilder text = new StringBuilder();

            Table table = new Table { Title = "Table cell (Data is 33 cells)", RecordViewRows = 0 };
            table.AddColumn(new TableColumn("#") { IsKey = true });
            table.AddColumn(new TableColumn("Data") { Width = 33, Overflow = ColumnOverflow.Wrap, MaxLines = 4 });
            for (int i = 0; i < values.Length; i++) {
                table.AddValues((i + 1).ToString(CultureInfo.InvariantCulture), values[i]);
            }
            text.AppendLine(Terminal.RenderText(table, profile, Theme.Dark));

            Table cards = new Table { Title = "Record cards (44 cells)", CardsBelow = 60, RecordViewRows = 0 };
            cards.AddColumn(new TableColumn("Record") { IsKey = true });
            cards.AddColumn(new TableColumn("Type"));
            cards.AddColumn(new TableColumn("Data") { MinWidth = 40, Overflow = ColumnOverflow.Wrap, MaxLines = 4 });
            for (int i = 0; i < values.Length; i++) {
                cards.AddValues("record " + (i + 1).ToString(CultureInfo.InvariantCulture), "TXT", values[i]);
            }
            text.AppendLine(Terminal.RenderText(cards, TerminalProfile.Fixed(44, 60, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.InvariantCulture), Theme.Dark));

            KeyValueList list = new KeyValueList();
            for (int i = 0; i < values.Length; i++) {
                list.Add("Value " + (i + 1).ToString(CultureInfo.InvariantCulture), Markup.Escape(values[i]));
            }
            text.AppendLine("Key/value list (44 cells)");
            text.AppendLine(Terminal.RenderText(list, TerminalProfile.Fixed(44, 40, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.InvariantCulture), Theme.Dark));

            LogRegion log = new LogRegion { Wrap = true, ShowTime = false, PreferredHeight = 20, MaxLinesPerEntry = 3 };
            foreach (string value in values) {
                log.Write("resolved " + value, "warn");
            }
            text.AppendLine("Log region with Wrap (44 cells)");
            text.AppendLine(Terminal.RenderText(log, TerminalProfile.Fixed(44, 40, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.InvariantCulture), Theme.Dark));

            string rendered = text.ToString();
            Assert.Contains("evotec-xyz.mail.protection.", rendered);
            Assert.DoesNotContain("outloo\n", rendered.Replace("\r\n", "\n").Replace(" ", string.Empty));
            Golden.Match("wrap.breakpoints.txt", rendered.Replace("\r\n", "\n").TrimEnd('\n') + "\n");
        }
    }
}
