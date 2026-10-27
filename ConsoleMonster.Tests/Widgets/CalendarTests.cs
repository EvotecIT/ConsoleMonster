using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
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
            string header = overridden.Split('\n')[2];
            Assert.Equal("Mo", header.Substring(8, 3).Trim());
            Assert.Equal("Tu", header.Substring(12, 3).Trim());
            Assert.Equal("We", header.Substring(16, 3).Trim());
        }

        // The names are pinned rather than read from the operating system, whose globalization data differs between ICU and NLS
        // ("Mo" or "M", "So." or "So"), so one calendar renders the same on Windows, macOS and Linux.
        [Theory]
        [InlineData("en-US", "Week    Su  Mo  Tu  We  Th  Fr  Sa", "October 2026", "Today 05 Oct")]
        [InlineData("en-GB", "Week    Mo  Tu  We  Th  Fr  Sa  Su", "October 2026", "Today 05 Oct")]
        [InlineData("pl-PL", "Week    Pn  Wt  Śr  Cz  Pt  So  Nd", "październik 2026", "Today 05 paź")]
        [InlineData("de-DE", "Week    Mo  Di  Mi  Do  Fr  Sa  So", "Oktober 2026", "Today 05 Okt")]
        [InlineData("fr-FR", "Week    Lu  Ma  Me  Je  Ve  Sa  Di", "octobre 2026", "Today 05 oct")]
        public void DayAndMonthNames_AreTheSameOnEveryPlatform(string name, string header, string title, string today) {
            CultureInfo culture = CultureInfo.GetCultureInfo(name);
            Calendar calendar = new Calendar(2026, 10) { Culture = culture, Today = new DateTime(2026, 10, 5) };
            string[] lines = TestTerminal.RenderText(calendar, width: 40).Split('\n');
            Assert.Equal(title, lines[0].TrimEnd());
            Assert.Equal(header, lines[2].TrimEnd());
            Assert.Contains(lines, line => line.Contains(today));
        }

        [Fact]
        public void OtherCultures_UseTwoCellLabelsWithoutPeriods() {
            CultureInfo culture = CultureInfo.GetCultureInfo("nl-NL");
            string header = TestTerminal.RenderText(new Calendar(2026, 10) { Culture = culture, ShowLegend = false }, width: 40).Split('\n')[2].PadRight(40);
            for (int col = 0; col < 7; col++) {
                string label = header.Substring(8 + col * 4, 3).TrimEnd();
                Assert.Equal(2, label.Length);
                Assert.True(char.IsUpper(label[0]), label);
                Assert.DoesNotContain(".", label);
            }
        }

        [Theory]
        [InlineData("zh-CN")]
        [InlineData("ar-SA")]
        [InlineData("ja-JP")]
        [InlineData("nl-NL")]
        [InlineData("vi-VN")]
        [InlineData("pt-BR")]
        [InlineData("pt-PT")]
        public void FallbackWeekdayHeaders_RenderSevenDistinctLocalizedLabels(string name) {
            CultureInfo culture = CultureInfo.GetCultureInfo(name);
            Calendar calendar = new Calendar(2026, 10) {
                Culture = culture,
                FirstDayOfWeek = DayOfWeek.Sunday,
                ShowLegend = false
            };
            CellBuffer frame = TestTerminal.Render(calendar, width: 40);
            HashSet<string> labels = new HashSet<string>(StringComparer.Ordinal);
            for (int col = 0; col < 7; col++) {
                StringBuilder text = new StringBuilder();
                for (int cell = 0; cell < 3; cell++) {
                    text.Append(frame[8 + col * 4 + cell, 2].Glyph);
                }
                string label = text.ToString().TrimEnd();
                Assert.NotEmpty(label);
                Assert.True(labels.Add(label), "Weekday headers must identify each day: " + label);
                Assert.InRange(ConsoleMonster.Text.DisplayWidth.Measure(label), 1, 3);
                string abbreviated = culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)col);
                string shortest = culture.DateTimeFormat.GetShortestDayName((DayOfWeek)col);
                Assert.True(culture.CompareInfo.IndexOf(abbreviated, label, CompareOptions.IgnoreCase) >= 0 ||
                    culture.CompareInfo.IndexOf(shortest, label, CompareOptions.IgnoreCase) >= 0,
                    "The header must retain the culture's weekday name: " + label);
                Assert.Equal(" ", frame[11 + col * 4, 2].Glyph);
            }
        }

        [Fact]
        public void AmbiguousCustomWeekdayNames_RetainSevenIdentifiableDays() {
            CultureInfo culture = (CultureInfo)CultureInfo.GetCultureInfo("nl-NL").Clone();
            culture.DateTimeFormat.AbbreviatedDayNames = new[] { "Day", "Day", "Day", "Day", "Day", "Day", "Day" };
            culture.DateTimeFormat.ShortestDayNames = new[] { "Day", "Day", "Day", "Day", "Day", "Day", "Day" };
            Calendar calendar = new Calendar(2026, 10) {
                Culture = culture,
                FirstDayOfWeek = DayOfWeek.Sunday,
                ShowLegend = false
            };
            string header = TestTerminal.RenderText(calendar, width: 40).Split('\n')[2];
            Assert.Equal("Week    Su  Mo  Tu  We  Th  Fr  Sa", header.TrimEnd());
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
            Assert.Contains("Today 20 " + expected.GetAbbreviatedMonthName(1).Trim().TrimEnd('.'), text);
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
