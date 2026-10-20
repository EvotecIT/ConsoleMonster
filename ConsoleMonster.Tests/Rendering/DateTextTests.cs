using System;
using System.Globalization;
using ConsoleMonster.Changes;
using ConsoleMonster.Presenters;
using ConsoleMonster.Values;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Dates read the same in tables, records, titles, trees and the change view.</summary>
    public sealed class DateTextTests {
        private static readonly DateTime Local = new DateTime(2026, 10, 19, 14, 5, 33, DateTimeKind.Local);

        [Fact]
        public void Text_IsIsoStyleInLocalTimeWhateverTheCulture() {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
                Assert.Equal("2026-10-19 14:05", DatePresenter.Text(Local));
                Assert.Equal("2026-10-19 14:05", DatePresenter.Text(new DateTimeOffset(Local)));
                Assert.Equal("2026-10-19 14:05", DatePresenter.Text(Local.ToUniversalTime()));
            } finally {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Text_SentinelsReadNever_AndOtherValuesAreNotDates() {
            Assert.Equal("never", DatePresenter.Text(DateTime.MinValue));
            Assert.Equal("never", DatePresenter.Text(DateTime.MaxValue));
            Assert.Null(DatePresenter.Text("2026-10-19"));
        }

        [Fact]
        public void ScalarTextAndChangeText_MatchTheTableText() {
            string table = DatePresenter.Text(new DateTimeOffset(Local))!;
            Assert.Equal(table, ValueShaper.ScalarText(new DateTimeOffset(Local)));
            Assert.Equal(table, ValueShaper.ScalarText(Local));
            Assert.Equal(table, ObjectDiff.Text(Local));
        }

        [Fact]
        public void PairText_DatesThatDifferBelowAMinute_ShowSeconds() {
            DateTime later = Local.AddSeconds(10);
            Assert.Equal("2026-10-19 14:05:33", ObjectDiff.PairText(Local, later));
            Assert.Equal("2026-10-19 14:05:43", ObjectDiff.PairText(later, Local));
            Assert.Equal("2026-10-19 14:05", ObjectDiff.PairText(Local, Local.AddHours(1)));
            PropertyChange change = new PropertyChange("When", Local, Local.AddMilliseconds(250));
            Assert.NotEqual(change.BeforeText, change.AfterText);
        }
    }
}
