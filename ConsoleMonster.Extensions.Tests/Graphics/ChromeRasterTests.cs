using System;
using ChartForgeX.Raster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class ChromeRasterTests {
        [Theory]
        [InlineData(WindowChromeStyle.MacOS, 28)]
        [InlineData(WindowChromeStyle.Windows, 32)]
        [InlineData(WindowChromeStyle.Linux, 36)]
        public void PngPreservesPageInsetRoundedBodyAndPaintedShadows(WindowChromeStyle style, int bar) {
            var frame = new CellBuffer(75, 8);
            var paint = new Style(default, Color.FromRgb(240, 20, 30));
            frame.Clear(paint);
            frame.Set(0, 7, "x", paint); // An occupied footer must not extend its paint into the page.
            var options = new GraphicsExportOptions { Frame = new ExportOptions { WindowChrome = true, ChromeStyle = style, Theme = ExportTheme.Dark } };
            RgbaImage image = frame.ToRaster(options);
            var measured = ConsoleExporter.MeasureFrame(75, 8, options.Frame);
            Assert.Equal(measured.Width, image.Width); Assert.Equal(measured.Height, image.Height);
            Assert.Equal((58, 58, 63), Pixel(image, 0, 0));
            Assert.Equal((12, 12, 12), Pixel(image, 20, bar + 12));
            Assert.Equal((240, 20, 30), Pixel(image, 30, bar + 25));
            Assert.NotEqual((240, 20, 30), Pixel(image, image.Width - 10, image.Height - 10));
            // The far painted shadow reaches below the window at the centre of the canvas.
            Assert.True(Pixel(image, image.Width / 2, image.Height - 7).Item1 < 58);
        }

        [Fact]
        public void LightWindowsChromeUsesWhiteShellInkAndDarkWindowControls() {
            var image = new CellBuffer(75, 8).ToRaster(new GraphicsExportOptions {
                Frame = new ExportOptions { WindowChrome = true, ChromeStyle = WindowChromeStyle.Windows, Theme = ExportTheme.Light }
            });
            bool whiteShellInk = false;
            for (int y = 21; y <= 30; y++) for (int x = 31; x <= 42; x++) {
                var pixel = Pixel(image, x, y);
                whiteShellInk |= pixel.Item1 > 220 && pixel.Item2 > 220 && pixel.Item3 > 220;
            }
            Assert.True(whiteShellInk);
            Assert.True(Pixel(image, image.Width - 100, 26).Item1 < 180);
            Assert.Equal((255, 255, 255), Pixel(image, 20, 44));
        }
        private static (int, int, int) Pixel(RgbaImage image, int x, int y) {
            int p = 4 * (y * image.Width + x);
            Assert.Equal(255, image.Pixels[p + 3]);
            return (image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2]);
        }
    }
}
