using System.Globalization;
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Localization;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class WarningCountTests {
        [Theory]
        [InlineData(0, "0 warnings")]
        [InlineData(1, "1 warning")]
        [InlineData(2, "2 warnings")]
        public void CountLabel_Warnings_SelectsSingularOnlyForOne(int count, string expected) {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(120, 20), Theme.Dark);
            Assert.Equal(expected, CellMap.CountLabel(StatusKind.Warn, count, context));
        }

        [Theory]
        [InlineData("de", "1 Warnung")]
        [InlineData("es", "1 advertencia")]
        [InlineData("fr", "1 avertissement")]
        [InlineData("it", "1 avviso")]
        [InlineData("pl", "1 ostrzeżenie")]
        public void Warnings_One_UsesLocalizedSingular(string language, string expected) {
            WidgetLabels labels = WidgetLabels.For(CultureInfo.GetCultureInfo(language));
            Assert.Equal(expected, WidgetLabels.Format(labels.Warnings(1), "1"));
        }
    }
}
