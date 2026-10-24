using VtScreen = ConsoleMonster.Sessions.VtScreen;
using ConsoleMonster.Canvas;
using SiteFrames.Capture;
using SiteFrames.Vt;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class NativeStylingContracts {
        [Theory]
        [InlineData(60, "107", 1)]
        [InlineData(120, "107", 2)]
        [InlineData(60, "40", 2)]
        [InlineData(120, "40", 1)]
        public void NativeFrames_KeepContentAndExactlyOneBlankRow(int width, string background, int contentRows) {
            string vt = "\u001b[" + background + "m\u001b[2J\u001b[HGet-Pro\u001b[38;2;208;208;208mcess -Name pwsh";
            if (contentRows == 2) { vt += "\r\n\u001b[0;" + background + "mNative progress: 42%"; }
            VtScreen original = new VtScreen(width, 20);
            original.Feed(vt);
            CellBuffer before = original.Snapshot();
            CellBuffer after = NativeStylingReview.FrameFromVt(vt, width, 20);
            Assert.Equal(width, after.Width);
            Assert.Equal(contentRows + 1, after.Height);
            Assert.Equal(string.Empty, after.GetRowText(after.Height - 1).Trim());
            Assert.NotEqual(string.Empty, after.GetRowText(after.Height - 2).Trim());
            for (int y = 0; y < after.Height; y++) {
                for (int x = 0; x < after.Width; x++) { Assert.Equal(before[x, y], after[x, y]); }
            }
        }

        [Fact]
        public void NativeFrames_KeepWrappedSelectionCells() {
            string command = "PS C:\\Admin> Get-Process -Name 'pwsh' | Select-Object -First 3";
            string vt = "\u001b[107m\u001b[2J\u001b[H\u001b[38;2;0;0;0;48;2;0;255;255m" + command;
            CellBuffer after = NativeStylingReview.FrameFromVt(vt, 60, 20);
            Assert.Equal(3, after.Height);
            Assert.Contains("3", after.GetRowText(1));
            Assert.Equal(command.Replace(" ", ""), (after.GetRowText(0) + after.GetRowText(1)).Replace(" ", ""));
        }

        [Fact]
        public void NativeFrames_AddBlankRowWithoutDroppingContentAtCaptureBoundary() {
            CellBuffer frame = NativeStylingReview.FrameFromVt("\u001b[107m\u001b[2J\u001b[Hfirst\r\nlast", 12, 2);
            Assert.Equal(3, frame.Height);
            Assert.Equal("last", frame.GetRowText(1).Trim());
            Assert.Equal(string.Empty, frame.GetRowText(2).Trim());
            Assert.Equal(frame[0, 0].Style.Background, frame[0, 2].Style.Background);
        }
    }
}
