using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Raster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class GraphicsTests {
        [Fact]
        public void TextKeepsItsCellPositionsAndSpacesWhenRasterised() {
            var frame = new CellBuffer(7, 1);
            frame.Write(0, 0, "M  M  M", ConsoleMonster.Styling.Style.Plain);
            var image = frame.ToRaster(new GraphicsExportOptions { Frame = new ExportOptions {
                FontSize = 20, LineHeight = 1, Padding = 0, Theme = ExportTheme.Dark
            } });
            foreach (int column in new[] { 0, 3, 6 }) {
                int ink = 0;
                for (int y = 0; y < image.Height; y++) {
                    for (int x = column * 12; x < (column + 1) * 12; x++) { if (Ink(image, x, y)) { ink++; } }
                }
                Assert.True(ink > 10, "Missing text at cell " + column);
            }
            foreach (int column in new[] { 1, 2, 4, 5 }) {
                for (int y = 0; y < image.Height; y++) { Assert.False(Ink(image, column * 12 + 6, y)); }
            }
        }
        [Fact]
        public void BoxAndBlockCellsMeetAcrossEveryPixelAtTheirEdges() {
            var frame = new CellBuffer(4, 3);
            frame.Set(0, 0, "┌", ConsoleMonster.Styling.Style.Plain); frame.Set(1, 0, "─", ConsoleMonster.Styling.Style.Plain); frame.Set(2, 0, "─", ConsoleMonster.Styling.Style.Plain); frame.Set(3, 0, "┐", ConsoleMonster.Styling.Style.Plain);
            frame.Set(0, 1, "│", ConsoleMonster.Styling.Style.Plain); frame.Set(3, 1, "│", ConsoleMonster.Styling.Style.Plain); frame.Set(0, 2, "└", ConsoleMonster.Styling.Style.Plain); frame.Set(1, 2, "─", ConsoleMonster.Styling.Style.Plain); frame.Set(2, 2, "─", ConsoleMonster.Styling.Style.Plain); frame.Set(3, 2, "┘", ConsoleMonster.Styling.Style.Plain);
            var settings = new GraphicsExportOptions { Frame = new ExportOptions { FontSize = 20, LineHeight = 1, Padding = 0, Theme = ExportTheme.Dark } };
            var image = frame.ToRaster(settings);
            Assert.Equal(48, image.Width); Assert.Equal(60, image.Height);
            for (int x = 6; x < 42; x++) { Assert.True(Ink(image, x, 10), "Top border gap at " + x); }
            for (int y = 10; y < 50; y++) { Assert.True(Ink(image, 6, y), "Side border gap at " + y); }
            var blocks = new CellBuffer(2, 2);
            for (int y = 0; y < 2; y++) { for (int x = 0; x < 2; x++) { blocks.Set(x, y, "█", ConsoleMonster.Styling.Style.Plain); } }
            var solid = blocks.ToRaster(settings);
            for (int i = 0; i < solid.Pixels.Length; i += 4) { Assert.True(solid.Pixels[i] > 180); Assert.Equal(255, solid.Pixels[i + 3]); }
        }
        [Theory]
        [InlineData(GraphicsPreset.Slide16x9, 1920, 1080)]
        [InlineData(GraphicsPreset.Slide4x3, 1440, 1080)]
        [InlineData(GraphicsPreset.Social, 1200, 630)]
        public void PresetsProduceSlideDimensionsAndStableEmbeddedText(GraphicsPreset preset, int width, int height) {
            var frame = new CellBuffer(20, 2); frame.Write(0, 0, "A demo: 42 ✓", ConsoleMonster.Styling.Style.Plain);
            var options = new GraphicsExportOptions { Preset = preset };
            byte[] first = frame.ToPng(options), second = frame.ToPng(options);
            Assert.Equal(first, second);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, first.Take(8).ToArray());
            Assert.Equal(width, BigEndian(first, 16)); Assert.Equal(height, BigEndian(first, 20));
        }
        [Fact]
        public void GifAndApngContainTimedAnimationAndChapterPngUsesCompletedView() {
            var recording = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":20,\"height\":3}\n[0,\"m\",\"First\"]\n[0.1,\"o\",\"one\"]\n[1,\"o\",\"\\r\\nfinished\"]\n[5,\"m\",\"Second\"]\n[5.1,\"o\",\"\\r\\ntwo\"]\n"));
            string folder = Path.Combine(Path.GetTempPath(), "cm-graphics-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            try {
                var settings = new GraphicsExportOptions { LoopCount = 2, IdleLimit = TimeSpan.FromSeconds(0.5) };
                string gif = Path.Combine(folder, "demo.gif"), apng = Path.Combine(folder, "demo.apng");
                recording.SaveGraphics(gif, GraphicsFormat.Gif, settings); recording.SaveGraphics(apng, GraphicsFormat.Apng, settings);
                byte[] bytes = File.ReadAllBytes(gif);
                Assert.Equal("GIF89a", Encoding.ASCII.GetString(bytes, 0, 6)); Assert.Equal(0x3b, bytes[bytes.Length - 1]);
                var delays = GifDelays(bytes, out int repetitions);
                Assert.Equal(1, repetitions); // two total plays in the public API
                Assert.InRange(delays.Count, 3, 4);
                Assert.All(delays.Take(delays.Count - 1), delay => Assert.InRange(delay, 1, 50));
                Assert.True(delays[delays.Count - 1] >= 200);
                byte[] png = File.ReadAllBytes(apng);
                Assert.Contains("acTL", Encoding.ASCII.GetString(png)); Assert.Contains("fcTL", Encoding.ASCII.GetString(png)); Assert.Contains("fdAT", Encoding.ASCII.GetString(png));
                Assert.Contains("finished", ConsoleExporter.ToText(recording.ChapterFrame(0)));
                var slides = recording.SaveChapterPngs(folder, settings); Assert.Equal(2, slides.Count); Assert.All(slides, path => Assert.True(new FileInfo(path).Length > 100));
            } finally { Directory.Delete(folder, true); }
        }
        [Fact]
        public void ResizedTerminalExportsUseOneCanvasWithoutCropping() {
            var recording = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":10,\"height\":3}\n[0.1,\"o\",\"small\"]\n[0.5,\"r\",\"20x5\"]\n[0.6,\"o\",\"\\r\\nlarge\\u001b[5;20HZ\"]\n"));
            string folder = Path.Combine(Path.GetTempPath(), "cm-resize-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                string gif = Path.Combine(folder, "resize.gif"), apng = Path.Combine(folder, "resize.apng");
                recording.SaveGraphics(gif, GraphicsFormat.Gif);
                recording.SaveGraphics(apng, GraphicsFormat.Apng);
                byte[] bytes = File.ReadAllBytes(gif), png = File.ReadAllBytes(apng);
                int width = bytes[6] | (bytes[7] << 8), height = bytes[8] | (bytes[9] << 8);
                Assert.Equal(BigEndian(png, 16), width); Assert.Equal(BigEndian(png, 20), height);
                Assert.True(width >= 20 * 8); Assert.True(height >= 5 * 16);
                Assert.True(GifDelays(bytes, out _).Count >= 2);
            } finally { Directory.Delete(folder, true); }
        }
        private static List<int> GifDelays(byte[] bytes, out int repetitions) {
            int position = 13;
            if ((bytes[10] & 0x80) != 0) { position += 3 * (1 << ((bytes[10] & 7) + 1)); }
            List<int> delays = new List<int>(); int delay = 0; repetitions = -1;
            while (bytes[position] != 0x3b) {
                byte marker = bytes[position++];
                if (marker == 0x21) {
                    byte label = bytes[position++];
                    if (label == 0xf9) {
                        Assert.Equal(4, bytes[position++]); position++;
                        delay = bytes[position] | (bytes[position + 1] << 8); position += 2;
                        position++; Assert.Equal(0, bytes[position++]);
                    } else {
                        int length;
                        bool app = label == 0xff;
                        while ((length = bytes[position++]) != 0) {
                            if (app && length == 3 && bytes[position] == 1) { repetitions = bytes[position + 1] | (bytes[position + 2] << 8); }
                            position += length;
                        }
                    }
                } else {
                    Assert.Equal(0x2c, marker);
                    byte packed = bytes[position + 8]; position += 9;
                    if ((packed & 0x80) != 0) { position += 3 * (1 << ((packed & 7) + 1)); }
                    position++; // LZW code size
                    int length; while ((length = bytes[position++]) != 0) { position += length; }
                    delays.Add(delay);
                }
            }
            Assert.Equal(bytes.Length - 1, position);
            return delays;
        }
        private static bool Ink(RgbaImage image, int x, int y) => image.Pixels[(y * image.Width + x) * 4] > 100;
        private static int BigEndian(byte[] bytes, int offset) => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
