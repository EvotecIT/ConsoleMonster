using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class TerminalBackgroundTests {
        [Theory]
        [InlineData(0, 0, 0, false)]
        [InlineData(255, 255, 255, true)]
        [InlineData(12, 12, 12, false)]
        [InlineData(250, 250, 250, true)]
        [InlineData(253, 246, 227, true)]
        [InlineData(0, 43, 54, false)]
        public void CalculateLuminance_StandardBackgrounds_ClassifiesLightness(byte red, byte green, byte blue, bool isLight) {
            double luminance = TerminalBackground.CalculateLuminance(red, green, blue);
            Assert.Equal(isLight, luminance > 0.5);
        }

        [Theory]
        [InlineData(0x0000, 0)]
        [InlineData(0x00F0, 15)]
        [InlineData(0x00A0, 10)]
        public void GetAttributeIndex_BackgroundNibble_ReturnsPaletteIndex(ushort attributes, int expected) {
            Assert.Equal(expected, TerminalBackground.GetAttributeIndex(attributes));
        }

        [Theory]
        [InlineData("7;0", 0, true)]
        [InlineData("7;7", 7, true)]
        [InlineData("15;15", 15, true)]
        [InlineData("0;8", 8, true)]
        [InlineData("0;14", 14, true)]
        [InlineData("0;16", 0, false)]
        [InlineData("bad", 0, false)]
        public void TryParseColorFgbg_BackgroundIndex_ParsesSupportedValues(string value, int expectedIndex, bool expectedValid) {
            bool valid = TerminalBackground.TryParseColorFgbg(value, out bool isLight);
            Assert.Equal(expectedValid, valid);
            if (valid) {
                Assert.Equal(expectedIndex == 7 || expectedIndex == 15, isLight);
            }
        }
    }
}
