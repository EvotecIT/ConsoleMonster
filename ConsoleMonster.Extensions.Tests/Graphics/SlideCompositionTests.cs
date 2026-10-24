using System;
using System.IO;
using ChartForgeX.Raster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class SlideCompositionTests {
        [Theory]
        [InlineData(GraphicsPreset.Slide16x9, 1920, 1080)]
        [InlineData(GraphicsPreset.Slide4x3, 1440, 1080)]
        [InlineData(GraphicsPreset.Social, 1200, 630)]
        public void SlideWindowIsCentredContainedAndFitsContent(GraphicsPreset preset, int width, int height) {
            CellBuffer frame = new CellBuffer(100, 40);
            frame.Write(0, 0, "A talk", Style.Plain); frame.Write(0, 5, new string('X', 45), Style.Plain);
            GraphicsExportOptions settings = Settings(preset);
            RgbaImage content = frame.ToRaster(settings);
            Assert.Equal(width, content.Width); Assert.Equal(height, content.Height);
            var bounds = WindowBounds(content);
            int inset = (int)Math.Ceiling(Math.Min(width, height) * 0.06);
            int right = width - bounds.Right - 1, bottom = height - bounds.Bottom - 1;
            Assert.InRange(bounds.Left, inset - 2, width / 2);
            Assert.InRange(bounds.Top, inset - 2, height / 2);
            Assert.InRange(Math.Abs(bounds.Left - right), 0, 2);
            Assert.InRange(Math.Abs(bounds.Top - bottom), 0, 2);
            Assert.InRange(Math.Min(bounds.Left, bounds.Top), inset - 2, inset + 2);
            // Rounded corners and the complete bottom edge are visible against the surrounding canvas.
            Assert.False(IsWindow(content, bounds.Left, bounds.Top));
            Assert.True(IsWindow(content, width / 2, bounds.Bottom));
            settings.Fit = GraphicsFit.Terminal;
            var terminal = WindowBounds(frame.ToRaster(settings));
            Assert.True(terminal.Bottom - terminal.Top > bounds.Bottom - bounds.Top);
            Assert.True(terminal.Right - terminal.Left < bounds.Right - bounds.Left);
        }

        [Fact]
        public void GifKeepsOneCompositionAcrossTheClipAndMatchesTheSamePngWindow() {
            SessionRecording recording = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":100,\"height\":40}\n[0.1,\"o\",\"one\"]\n[0.5,\"o\",\"\\u001b[6;41Hlast row\"]\n[1,\"o\",\"\\u001b[2J\\u001b[Hdone\"]\n"));
            Assert.Equal(40, recording.CaptureFrames(trimEmptyRows: false)[0].Frame.Height);
            string directory = Path.Combine(Path.GetTempPath(), "cm-slide-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try {
                GraphicsExportOptions settings = Settings(GraphicsPreset.Social);
                string gif = Path.Combine(directory, "talk.gif");
                recording.SaveGraphics(gif, GraphicsFormat.Gif, settings);
                Assert.True(RasterImageDecoder.TryRead(gif, out RgbaImage animation));
                // The first GIF screen is empty, but already reserves the clip's widest content and six used rows.
                CellBuffer still = recording.FrameAt(TimeSpan.FromSeconds(0.5));
                var png = WindowBounds(still.ToRaster(settings));
                var first = WindowBounds(animation);
                Assert.InRange(Math.Abs(first.Left - png.Left), 0, 1);
                Assert.InRange(Math.Abs(first.Top - png.Top), 0, 1);
                Assert.InRange(Math.Abs(first.Right - png.Right), 0, 1);
                Assert.InRange(Math.Abs(first.Bottom - png.Bottom), 0, 1);
            } finally { Directory.Delete(directory, true); }
        }

        [Fact]
        public void SoftGradientAndSolidBackgroundsKeepTheirChosenColours() {
            CellBuffer frame = new CellBuffer(40, 4); frame.Write(0, 0, "Demo", Style.Plain);
            GraphicsExportOptions settings = Settings(GraphicsPreset.Social);
            RgbaImage solid = frame.ToRaster(settings);
            Assert.Equal(new byte[] { 51, 102, 153 }, Pixel(solid, 0, 0));
            settings.Background = GraphicsBackground.Gradient; settings.BackgroundColor = "#112233"; settings.BackgroundEndColor = "#6688aa";
            RgbaImage gradient = frame.ToRaster(settings);
            Assert.True(Pixel(gradient, 0, 0)[0] < Pixel(gradient, 1199, 629)[0]);
            settings.BackgroundColor = "url(external)";
            Assert.Throws<ArgumentException>(() => frame.ToRaster(settings));
        }

        [Theory]
        [InlineData(WindowChromeStyle.MacOS)]
        [InlineData(WindowChromeStyle.Windows)]
        [InlineData(WindowChromeStyle.Linux)]
        public void AChromeTitleDoesNotChangeAnyBodyGlyphs(WindowChromeStyle chrome) {
            CellBuffer frame = new CellBuffer(100, 8);
            frame.Write(0, 0, "PS demo> Get-Item ./missing.json 2>&1 | Write-ConsoleText -Color Red", Style.Plain);
            frame.Write(0, 7, "PS demo> ", Style.Plain);
            GraphicsExportOptions settings = Settings(GraphicsPreset.Slide16x9);
            settings.Frame.WindowChrome = true; settings.Frame.ChromeStyle = chrome; settings.Frame.Title = "demo";
            RgbaImage titled = frame.ToRaster(settings);
            settings.Frame.Title = null;
            RgbaImage untitled = frame.ToRaster(settings);
            // The title is above the middle of this short window; all body pixels below must stay identical.
            for (int i = (titled.Height / 2) * titled.Width * 4; i < titled.Pixels.Length; i++) {
                Assert.True(titled.Pixels[i] == untitled.Pixels[i], "Chrome title changed a body pixel at " + ((i / 4) % titled.Width) + "," + (i / (4 * titled.Width)));
            }
        }

        [Fact]
        public void ComposingAWindowPreservesTheLastRowsGlyphs() {
            CellBuffer frame = new CellBuffer(100, 23);
            frame.Write(0, 0, "A recorded command", Style.Plain);
            frame.Write(0, 22, "PS demo>", Style.Plain.WithForeground(Color.FromRgb(255, 0, 255)));
            GraphicsExportOptions slide = Settings(GraphicsPreset.Slide16x9);
            slide.Frame.WindowChrome = true; slide.Frame.Title = "demo"; slide.Fit = GraphicsFit.Terminal;
            GraphicsExportOptions natural = new GraphicsExportOptions {
                Frame = new ExportOptions { Theme = ExportTheme.Dark, FontSize = 29, Padding = 24, WindowChrome = true, Title = "demo" }
            };
            int expected = MagentaInk(frame.ToRaster(natural)), actual = MagentaInk(frame.ToRaster(slide));
            // Slide fitting selects approximately the same font; moving it into a canvas must not lose characters.
            Assert.True(expected > 100);
            Assert.InRange(actual, (int)(expected * 0.9), (int)(expected * 1.1));
        }
        [Fact]
        public void ContentColumnsIncludeWideGlyphsAndPaintedCells() {
            CellBuffer frame = new CellBuffer(100, 8);
            frame.Write(40, 1, "界", Style.Plain);
            Assert.Equal(42, ConsoleExporter.MeasureContentColumns(frame));
            frame.Write(60, 5, " ", Style.Plain.WithBackground(Color.FromRgb(255, 0, 255)));
            Assert.Equal(61, ConsoleExporter.MeasureContentColumns(frame));
            Assert.Equal(1, ConsoleExporter.MeasureContentColumns(new CellBuffer(100, 8)));
        }

        [Fact]
        public void NarrowContentUsesLargerTextWhileTerminalAndDocsRetainTheirGrid() {
            CellBuffer frame = new CellBuffer(100, 8);
            frame.Write(0, 0, new string('M', 45), Style.Plain.WithForeground(Color.FromRgb(255, 0, 255)));
            GraphicsExportOptions settings = Settings(GraphicsPreset.Slide16x9);
            RgbaImage content = frame.ToRaster(settings);
            settings.Fit = GraphicsFit.Terminal;
            RgbaImage terminal = frame.ToRaster(settings);
            // 45 used columns plus padding can nearly double the font of a 100-column terminal.
            Assert.True(MagentaInk(content) > MagentaInk(terminal) * 3);
            settings.Preset = GraphicsPreset.Docs;
            int terminalWidth = frame.ToRaster(settings).Width;
            settings.Fit = GraphicsFit.Content;
            Assert.Equal(terminalWidth, frame.ToRaster(settings).Width);
        }

        [Fact]
        public void TinyChaptersKeepTheFontCapInsteadOfFillingTheWholeSlide() {
            CellBuffer frame = new CellBuffer(100, 40);
            frame.Write(0, 0, "Hi", Style.Plain.WithForeground(Color.FromRgb(255, 0, 255)));
            GraphicsExportOptions settings = Settings(GraphicsPreset.Slide16x9);
            RgbaImage slide = frame.ToRaster(settings);
            var bounds = WindowBounds(slide);
            Assert.True(bounds.Right - bounds.Left < 300);
            Assert.True(bounds.Bottom - bounds.Top < 200);
            RgbaImage natural = frame.ToRaster(new GraphicsExportOptions {
                Frame = new ExportOptions { Theme = ExportTheme.Dark, FontSize = 72 }
            });
            Assert.InRange(MagentaInk(slide), (int)(MagentaInk(natural) * 0.9), (int)(MagentaInk(natural) * 1.1));
        }

        private static int MagentaInk(RgbaImage image) {
            int count = 0;
            for (int i = 0; i < image.Pixels.Length; i += 4) {
                if (image.Pixels[i] > 180 && image.Pixels[i + 1] < 90 && image.Pixels[i + 2] > 180) { count++; }
            }
            return count;
        }

        private static GraphicsExportOptions Settings(GraphicsPreset preset) => new GraphicsExportOptions {
            Preset = preset, Background = GraphicsBackground.Solid, BackgroundColor = "#336699",
            Frame = new ExportOptions { Theme = ExportTheme.Dark }
        };
        private static byte[] Pixel(RgbaImage image, int x, int y) {
            int offset = (y * image.Width + x) * 4;
            return new[] { image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2] };
        }
        private static bool IsWindow(RgbaImage image, int x, int y) {
            int offset = (y * image.Width + x) * 4;
            return Math.Abs(image.Pixels[offset] - 12) <= 1 && Math.Abs(image.Pixels[offset + 1] - 12) <= 1 && Math.Abs(image.Pixels[offset + 2] - 12) <= 1;
        }
        private static (int Left, int Top, int Right, int Bottom) WindowBounds(RgbaImage image) {
            int left = image.Width, top = image.Height, right = -1, bottom = -1;
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    if (!IsWindow(image, x, y)) { continue; }
                    left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                }
            }
            Assert.True(right > left && bottom > top, "The terminal window must be visible.");
            return (left, top, right, bottom);
        }
    }
}
