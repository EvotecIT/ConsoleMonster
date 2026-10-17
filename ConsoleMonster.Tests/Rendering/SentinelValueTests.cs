using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// "Never" and "no limit" sentinels (DateTime.MinValue/MaxValue, TimeSpan.MinValue/MaxValue, numbers beyond the
    /// TimeSpan range) present as text instead of throwing, in any culture and time zone, and never abort a table.
    /// </summary>
    public sealed class SentinelValueTests {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        // Far east of UTC: the zone where DateTime.MinValue overflows when shifted.
        private static readonly TimeZoneInfo LineIslands = TimeZoneInfo.CreateCustomTimeZone("UTC+14", TimeSpan.FromHours(14), "UTC+14", "UTC+14");
        private static readonly TimeZoneInfo BakerIsland = TimeZoneInfo.CreateCustomTimeZone("UTC-12", TimeSpan.FromHours(-12), "UTC-12", "UTC-12");

        private static PresenterContext Context(TimeZoneInfo zone, GlyphTier tier = GlyphTier.Full) {
            TerminalProfile profile = new TerminalProfile(ColorDepth.None, tier, OutputMode.Interactive, 80, 24);
            return new PresenterContext(new RenderContext(profile, Theme.Dark), CultureInfo.GetCultureInfo("pl-PL"), Now, zone);
        }

        public static IEnumerable<object[]> SentinelDates() {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) };
            yield return new object[] { DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local) };
            yield return new object[] { DateTime.MinValue.AddHours(3) };
            yield return new object[] { DateTimeOffset.MinValue };
            yield return new object[] { DateTimeOffset.MaxValue };
        }

        [Theory]
        [MemberData(nameof(SentinelDates))]
        public void Date_Sentinel_ShowsNeverInFarEastAndFarWestZones(object value) {
            foreach (TimeZoneInfo zone in new[] { LineIslands, BakerIsland, TimeZoneInfo.Utc }) {
                foreach (DateDisplay display in new[] { DateDisplay.Absolute, DateDisplay.Relative, DateDisplay.Both }) {
                    PresentedValue presented = new DatePresenter(display).Present(value, Context(zone));
                    Assert.Equal("never", presented.Text);
                    Assert.Null(presented.Number);
                }
            }
        }

        [Fact]
        public void Date_OneDayInsideTheRange_StillFormatsInFarEastZone() {
            DateTimeOffset early = new DateTimeOffset(DateTime.MinValue.AddDays(1), TimeSpan.Zero);
            PresentedValue presented = new DatePresenter().Present(early, Context(LineIslands));
            Assert.Equal("0001-01-02 14:00", presented.Text);
        }

        [Theory]
        [InlineData("max")]
        [InlineData("min")]
        public void Duration_TimeSpanLimits_ShowADash(string which) {
            TimeSpan value = which == "max" ? TimeSpan.MaxValue : TimeSpan.MinValue;
            Assert.Equal("—", CellPresenters.Duration.Present(value, Context(LineIslands)).Text);
            Assert.Equal("-", CellPresenters.Duration.Present(value, Context(LineIslands, GlyphTier.Ascii)).Text);
        }

        [Theory]
        [InlineData(1e300)]
        [InlineData(-1e300)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NaN)]
        public void Duration_NumbersBeyondTheRange_ShowADash(double seconds) {
            Assert.Equal("—", CellPresenters.Duration.Present(seconds, Context(LineIslands)).Text);
        }

        [Fact]
        public void Duration_Format_MinValueDoesNotOverflow() {
            string text = DurationPresenter.Format(TimeSpan.MinValue, CultureInfo.GetCultureInfo("pl-PL"));
            Assert.StartsWith("-10675199 d", text);
        }

        [Fact]
        public void Table_WithSentinelsAndTotals_RendersEveryRow() {
            Table table = new Table { ShowTotals = true, RecordViewRows = 0 };
            table.Presentation.Culture = CultureInfo.GetCultureInfo("pl-PL");
            table.Presentation.Now = Now;
            table.Presentation.TimeZone = LineIslands;
            table.AddColumn(new TableColumn("Account") { IsKey = true });
            table.AddColumn(new TableColumn("Expires") { Presenter = new DatePresenter(DateDisplay.Both) });
            table.AddColumn(new TableColumn("Lockout") { Presenter = CellPresenters.Duration, Aggregate = ColumnAggregate.Sum });
            table.AddValues("krbtgt", DateTime.MinValue, TimeSpan.MaxValue);
            table.AddValues("svc-backup", DateTime.MaxValue, TimeSpan.MinValue);
            table.AddValues("przemek", new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(30));
            string text = Terminal.RenderText(table, TerminalProfile.Fixed(100, 30, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.Contains("krbtgt", text);
            Assert.Contains("svc-backup", text);
            Assert.Contains("never", text);
            Assert.Contains("2026-11-09 02:00", text);
            Assert.DoesNotContain("render error", text);
        }
    }
}
