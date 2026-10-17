using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class TerminalSizeTests {
        [Fact]
        public void TryComputeWindowSize_ScrollableBuffer_UsesVisibleWindowRectangle() {
            bool valid = TerminalSize.TryComputeWindowSize(20, 5, 119, 34, out int width, out int height);
            Assert.True(valid);
            Assert.Equal(100, width);
            Assert.Equal(30, height);
        }

        [Fact]
        public void TryComputeWindowSize_InvalidRectangle_ReturnsFalse() {
            Assert.False(TerminalSize.TryComputeWindowSize(10, 10, 9, 9, out _, out _));
        }
    }
}
