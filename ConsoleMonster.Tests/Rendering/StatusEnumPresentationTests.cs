using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed class StatusEnumPresentationTests {
        [Theory]
        [InlineData("Running", StatusKind.Running, ThemeToken.Running)]
        [InlineData("Pending", StatusKind.Pending, ThemeToken.Pending)]
        [InlineData("Stopped", StatusKind.Skip, ThemeToken.Skip)]
        [InlineData("Established", StatusKind.Pass, ThemeToken.Pass)]
        [InlineData("Listen", StatusKind.Info, ThemeToken.Info)]
        [InlineData("TimeWait", StatusKind.Skip, ThemeToken.Skip)]
        [InlineData("CloseWait", StatusKind.Skip, ThemeToken.Skip)]
        public void LifecycleWordsUseTheirSemanticMarkerAndTone(
            string word, StatusKind status, ThemeToken role) {
            var render = Terminal.CreateContext(TestTerminal.Profile(60));
            PresentedValue value = CellPresenters.StatusWord.Present(word, new PresenterContext(render));
            Assert.Equal(role, value.Role);
            Assert.Equal(render.Glyphs.StatusMarker(status) + " " + word, value.Text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, StatusMarkerStyle.Words)]
        [InlineData(GlyphTier.Basic, StatusMarkerStyle.Words)]
        [InlineData(GlyphTier.Ascii, StatusMarkerStyle.Symbols)]
        public void WordMarkersReplaceRecognizedSynonymsAndPreserveDistinctText(
            GlyphTier tier, StatusMarkerStyle markers) {
            var profile = TestTerminal.Profile(60, glyphs: tier).WithStatusMarkers(markers);
            var context = new PresenterContext(Terminal.CreateContext(profile));
            Assert.Equal("[OK]", CellPresenters.StatusWord.Present("Healthy", context).Text);
            Assert.Equal("[FAIL]", CellPresenters.StatusWord.Present("Failed", context).Text);
            Assert.Equal("[BUSY]", CellPresenters.StatusWord.Present("Running", context).Text);
            Assert.Equal("[WAIT]", CellPresenters.StatusWord.Present("Pending", context).Text);
            Assert.Equal("[OK]", CellPresenters.StatusWord.Present("Established", context).Text);
            Assert.Equal("[INFO]", CellPresenters.StatusWord.Present("Listen", context).Text);
            Assert.Equal("[SKIP]", CellPresenters.StatusWord.Present("TimeWait", context).Text);
            Assert.Equal("[SKIP]", CellPresenters.StatusWord.Present("CloseWait", context).Text);
            Assert.Equal("verification failed",
                CellPresenters.StatusWord.Present("verification failed", context).Text);
        }
    }
}
