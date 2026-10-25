using System;
using System.Globalization;
using ConsoleMonster.Dates;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using ConsoleMonster.Layout;
using Xunit;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class CalendarTests {
        [Theory]
        [InlineData(2020, 12, 31, 2020, 53)]
        [InlineData(2021, 1, 1, 2020, 53)]
        [InlineData(2021, 1, 4, 2021, 1)]
        [InlineData(2024, 12, 30, 2025, 1)]
        [InlineData(1, 1, 1, 1, 1)]
        [InlineData(9999, 12, 31, 9999, 52)]
        public void IsoWeek_UsesWeekYear(int year, int month, int day, int expectedYear, int expectedWeek) {
            Assert.Equal(expectedWeek, CalendarDate.IsoWeek(new DateTime(year, month, day), out int actualYear));
            Assert.Equal(expectedYear, actualYear);
        }

        [Fact]
        public void MarkedDate_DropsTimeAndKindWithoutConversion() {
            DateTime stamp = new DateTime(2026, 10, 25, 23, 50, 0, DateTimeKind.Utc);
            CalendarMark mark = new CalendarMark(stamp, StatusKind.Pass, "Backup");
            Assert.Equal(new DateTime(2026, 10, 25), mark.Date);
            Assert.Equal(DateTimeKind.Unspecified, mark.Date.Kind);
            CalendarData data = new CalendarData(new[] { mark });
            Assert.Equal(StatusKind.Pass, data.GetStatus(DateTime.SpecifyKind(stamp, DateTimeKind.Local)));
        }

        [Fact]
        public void DuplicateDates_RetainLabelsAndUseDocumentedPrecedence() {
            DateTime date = new DateTime(2026, 10, 25);
            object row = new object();
            CalendarData data = new CalendarData(new[] {
                new CalendarMark(date, StatusKind.Pass, "Backup", row),
                new CalendarMark(date.AddHours(10), StatusKind.Warn, "Expiry"),
                new CalendarMark(date, StatusKind.Fail, "Verification failed")
            });
            Assert.Equal(StatusKind.Fail, data.GetStatus(date));
            Assert.Equal(3, data.GetMarks(date).Count);
            Assert.Equal("Expiry", data.GetMarks(date)[1].Label);
            Assert.Same(row, data.GetMarks(date)[0].Source);
            Assert.Null(data.GetStatus(date.AddDays(1)));
        }

        [Theory]
        [InlineData(2024, "29")]
        [InlineData(2023, "28")]
        public void February_RendersOnlyValidDays(int year, string lastDay) {
            string text = TestTerminal.RenderText(new Calendar(year, 2) { ShowLegend = false }, width: 40);
            Assert.Contains(lastDay, text);
            Assert.DoesNotContain("30 ", text);
            if (year == 2023) { Assert.DoesNotContain("29 ", text); }
        }

        [Fact]
        public void CultureAndOverride_ChangeDayPositionsWithoutChangingIsoWeeks() {
            Calendar calendar = new Calendar(2026, 10) { ShowLegend = false };
            string sunday = TestTerminal.RenderText(calendar, width: 40, culture: CultureInfo.GetCultureInfo("en-US"));
            string monday = TestTerminal.RenderText(calendar, width: 40, culture: CultureInfo.GetCultureInfo("pl-PL"));
            Assert.Contains("October 2026", sunday);
            Assert.Contains("październik 2026", monday);
            Assert.Contains("W40", sunday);
            Assert.Contains("W40", monday);
            calendar.FirstDayOfWeek = DayOfWeek.Monday;
            string overridden = TestTerminal.RenderText(calendar, width: 40, culture: CultureInfo.GetCultureInfo("en-US"));
            Assert.NotEqual(sunday, overridden);
            DateTimeFormatInfo days = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
            string header = overridden.Split('\n')[2];
            Assert.Equal(days.GetShortestDayName(DayOfWeek.Monday), header.Substring(8, 3).Trim());
            Assert.Equal(days.GetShortestDayName(DayOfWeek.Tuesday), header.Substring(12, 3).Trim());
            Assert.Equal(days.GetShortestDayName(DayOfWeek.Wednesday), header.Substring(16, 3).Trim());
        }

        [Fact]
        public void Year_WrapsMonthsAndHasOneLegend() {
            Calendar calendar = new Calendar(2024, 1, new[] { new CalendarMark(new DateTime(2024, 2, 29), StatusKind.Fail, "Backup") }) { View = CalendarView.Year };
            string wide = TestTerminal.RenderText(calendar, width: 120, culture: CultureInfo.GetCultureInfo("en-US"));
            string narrow = TestTerminal.RenderText(calendar, width: 40, culture: CultureInfo.GetCultureInfo("en-US"));
            Assert.Contains("January 2024", wide);
            Assert.Contains("December 2024", narrow);
            Assert.True(narrow.Split('\n').Length > wide.Split('\n').Length);
            Assert.Equal(1, wide.Split(new[] { "✗ Fail" }, StringSplitOptions.None).Length - 1);
        }

        [Fact]
        public void StatusIsVisibleWithoutColour_AndUnmarkedDayIsNeutral() {
            Calendar calendar = new Calendar(2026, 10, new[] { new CalendarMark(new DateTime(2026, 10, 2), StatusKind.Fail, "Backup") });
            string text = TestTerminal.RenderText(calendar, width: 40);
            Assert.Contains("02✗", text);
            Assert.Contains("03", text);
            Assert.Contains("✗ Fail", text);
        }

        [Fact]
        public void UnmarkedCalendar_OmitsLegendAndFitsAlignment() {
            string text = TestTerminal.RenderText(new Align(new Calendar(2026, 10)), width: 100);
            Assert.DoesNotContain("Pass", text);
            Assert.DoesNotContain("Skip", text);
        }

        [Fact]
        public void IsoWeekGutter_OnlyShowsAWeekYearWhenItDiffers() {
            string text = TestTerminal.RenderText(new Calendar(2021, 1), width: 40);
            Assert.Contains("2020-53", text);
            Assert.Contains("W01", text);
            Assert.DoesNotContain("2021-01", text);
        }

        [Fact]
        public void AsciiMarks_ReserveTheirOwnColumnAndLegendOnlyUsesVisibleStatuses() {
            Calendar calendar = new Calendar(2026, 10, new[] { new CalendarMark(new DateTime(2026, 10, 2), StatusKind.Fail, "Backup") }) { FirstDayOfWeek = DayOfWeek.Monday };
            string text = TestTerminal.RenderText(calendar, width: 40, glyphs: ConsoleMonster.Capabilities.GlyphTier.Ascii);
            Assert.Contains("02x 03  04", text);
            Assert.Contains("x Fail", text);
            Assert.DoesNotContain("Warn", text);
        }

        [Theory]
        [InlineData(false, "20@")]
        [InlineData(true, "20!")]
        public void Today_IsNamedInPlainOutputWithoutHidingAnOutcome(bool marked, string day) {
            DateTime today = new DateTime(2026, 10, 20);
            Calendar calendar = new Calendar(2026, 10, marked ? new[] { new CalendarMark(today, StatusKind.Warn, "Expiry") } : Array.Empty<CalendarMark>()) {
                Today = today, FirstDayOfWeek = DayOfWeek.Monday
            };
            string text = TestTerminal.RenderText(calendar, width: 40, glyphs: ConsoleMonster.Capabilities.GlyphTier.Ascii, culture: CultureInfo.GetCultureInfo("en-US"));
            Assert.Contains(day, text);
            Assert.Contains("@ Today 20 Oct", text);
            calendar.Today = today.AddYears(1);
            Assert.DoesNotContain("Today", TestTerminal.RenderText(calendar, width: 40));
        }

        [Fact]
        public void NonGregorianCulture_UsesGregorianDatesWithoutChangingTheCulture() {
            CultureInfo culture = CultureInfo.GetCultureInfo("ar-SA");
            Type original = culture.DateTimeFormat.Calendar.GetType();
            DateTime today = new DateTime(2026, 1, 20);
            Calendar calendar = new Calendar(2026, 1) { Culture = culture, Today = today };
            string text = TestTerminal.RenderText(calendar, width: 40);
            DateTimeFormatInfo expected = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
            expected.Calendar = new GregorianCalendar();
            Assert.Contains(expected.GetMonthName(1) + " 2026", text);
            Assert.Contains("Today " + today.ToString("dd MMM", expected), text);
            Assert.Equal(original, culture.DateTimeFormat.Calendar.GetType());
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(9999, 12)]
        public void SupportedDateEdges_RenderWithoutOverflow(int year, int month) {
            foreach (DayOfWeek first in Enum.GetValues(typeof(DayOfWeek))) {
                string text = TestTerminal.RenderText(new Calendar(year, month) { FirstDayOfWeek = first }, width: 36);
                Assert.Contains("01", text);
            }
        }
    }
}
