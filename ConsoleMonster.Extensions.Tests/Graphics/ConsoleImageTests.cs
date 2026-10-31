using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX;
using ChartForgeX.Raster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Graphics;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class ConsoleImageTests {
        // 2x2 PNG: red, green / blue, transparent white. Small enough to pass through to Kitty and iTerm2 unchanged.
        private const string DotPng = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAGElEQVR4nGP4z8Dwn6GB4T8DA8N/EGAAAELVCHmF7Ec3AAAAAElFTkSuQmCC";
        private const string Esc = "\u001b";

        [Fact]
        public void Write_Kitty_SendsPngInOneFramedChunkAndContinuesBelowTheImage() {
            string output = Write(new ConsoleImage(Convert.FromBase64String(DotPng), "dot.png") { Width = 2 }, ImageProtocol.Kitty);
            Assert.Equal("\r\n" + Esc + "[1A" + Esc + "7" + Esc + "_Ga=T,f=100,q=2,C=1,c=2,r=1,m=0;" + DotPng + Esc + "\\" + Esc + "8" + Esc + "[1B", output);
        }

        [Fact]
        public void Write_KittyLargePng_SplitsIntoContinuationChunksOfAtMost4096() {
            byte[] png = Noise(64, 64).ToPng();
            string output = Write(new ConsoleImage(png, "noise.png") { Width = 8 }, ImageProtocol.Kitty);
            string data = Convert.ToBase64String(png);
            Assert.True(data.Length > 3 * 4096, "The fixture must need several chunks.");
            List<string> chunks = new List<string>();
            int at = output.IndexOf(Esc + "_G", StringComparison.Ordinal);
            while (at >= 0) {
                int end = output.IndexOf(Esc + "\\", at, StringComparison.Ordinal);
                chunks.Add(output.Substring(at + 3, end - at - 3));
                at = output.IndexOf(Esc + "_G", end, StringComparison.Ordinal);
            }
            Assert.StartsWith("a=T,f=100,q=2,C=1,c=8,r=4,m=1;", chunks[0]);
            StringBuilder joined = new StringBuilder(chunks[0].Substring(chunks[0].IndexOf(';') + 1));
            for (int i = 1; i < chunks.Count; i++) {
                string keys = i == chunks.Count - 1 ? "q=2,m=0;" : "q=2,m=1;";
                Assert.StartsWith(keys, chunks[i]);
                joined.Append(chunks[i].Substring(keys.Length));
            }
            for (int i = 0; i < chunks.Count - 1; i++) {
                Assert.Equal(4096, chunks[i].Length - chunks[i].IndexOf(';') - 1);
            }
            Assert.Equal(data, joined.ToString());
        }

        [Fact]
        public void Write_ITerm2_SendsOneInlineFileSizedInCells() {
            string output = Write(new ConsoleImage(Convert.FromBase64String(DotPng), "dot.png") { Width = 2 }, ImageProtocol.ITerm2);
            string name = Convert.ToBase64String(Encoding.UTF8.GetBytes("dot.png"));
            Assert.Equal("\r\n" + Esc + "[1A" + Esc + "7" + Esc + "]1337;File=name=" + name + ";size=81;width=2;height=1;inline=1:" + DotPng + "\u0007"
                + Esc + "8" + Esc + "[1B", output);
        }

        [Fact]
        public void Write_Sixel_KeepsTheAspectRatioAtTenPixelsPerColumnInSixRowBands() {
            // 2:1 at one column: 10x5 pixels, one band of five rows, inside one reserved row.
            RgbaImage redBlue = new RgbaImage(2, 1, new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 });
            string output = Write(new ConsoleImage(redBlue, "pair") { Width = 1 }, ImageProtocol.Sixel);
            string sixel = Esc + "P0;1;0q\"1;1;10;5#0;2;100;0;0#1;2;0;0;100#0!5^$#1!5?!5^" + Esc + "\\";
            Assert.Equal("\r\n" + Esc + "[1A" + Esc + "7" + sixel + Esc + "8" + Esc + "[1B", output);

            // 1:2 at one column: 10x20 pixels over four bands, the last two rows tall.
            RgbaImage tall = new RgbaImage(1, 2, new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 });
            string bands = Write(new ConsoleImage(tall, "tall") { Width = 1 }, ImageProtocol.Sixel);
            Assert.Contains("\"1;1;10;20#0;2;100;0;0#1;2;0;0;100#0!10~-#0!10N$#1!10o-#1!10~-#1!10B" + Esc + "\\", bands);
        }

        [Fact]
        public void Write_SixelWithMoreThan256Colours_DefinesAtMost256Registers() {
            string output = Write(new ConsoleImage(Noise(40, 40), "noise") { Width = 4 }, ImageProtocol.Sixel);
            Assert.InRange(Regex.Matches(output, "#[0-9]+;2;").Count, 2, 256);
            Assert.Contains("\"1;1;40;40", output);
            Assert.EndsWith(Esc + "\\" + Esc + "8" + Esc + "[2B", output);
        }

        [Fact]
        public void Render_HalfBlocks_MapTopPixelToForegroundAndBottomToBackground() {
            ConsoleImage image = new ConsoleImage(new RgbaImage(2, 4, new byte[] {
                255, 0, 0, 255, 0, 255, 0, 255,
                255, 0, 0, 255, 0, 0, 255, 255,
                255, 255, 255, 255, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            })) { Width = 2 };
            Color red = Color.FromRgb(255, 0, 0), green = Color.FromRgb(0, 255, 0), blue = Color.FromRgb(0, 0, 255), white = Color.FromRgb(255, 255, 255);

            CellBuffer full = Render(image, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(new Cell("█", new Style(red)), full[0, 0]);
            Assert.Equal(new Cell("▀", new Style(green, blue)), full[1, 0]);
            Assert.Equal(new Cell("▀", new Style(white)), full[0, 1]);
            Assert.Equal(Cell.Blank, full[1, 1]);

            CellBuffer palette = Render(image, ColorDepth.Ansi256, GlyphTier.Full);
            Assert.Equal(new Style(green, blue).Downsample(ColorDepth.Ansi256), palette[1, 0].Style);

            // ASCII fonts get a background-coloured space per cell, averaging its two pixels.
            CellBuffer ascii = Render(image, ColorDepth.TrueColor, GlyphTier.Ascii);
            Assert.Equal(new Cell(" ", Style.Plain.WithBackgroundColor(Color.FromRgb(0, 128, 128))), ascii[1, 0]);
        }

        [Fact]
        public void LiveFrames_KittyProfile_DrawCellsOnceAndNothingOnAnUnchangedRefresh() {
            TerminalProfile profile = TerminalProfile.Fixed(20, 6, ColorDepth.TrueColor).WithImageProtocol(ImageProtocol.Kitty);
            ConsoleImage image = new ConsoleImage(Convert.FromBase64String(DotPng)) { Width = 4 };
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ITerminalOutput output = Terminal.CreateOutput(profile, writer)) {
                output.BeginLive();
                output.RenderFrame(Terminal.Render(image, profile, Theme.Dark));
                string first = writer.ToString();
                output.RenderFrame(Terminal.Render(image, profile, Theme.Dark));
                string refresh = writer.ToString().Substring(first.Length);
                Assert.Contains("█", first);
                Assert.DoesNotContain(Esc + "_G", first);
                Assert.Equal(Esc + "[?2026h" + Esc + "[0m" + Esc + "[?2026l", refresh);
            }
        }

        [Fact]
        public void Write_RedirectedOutput_PrintsOnlyThePlaceholderEvenWithAForcedProtocol() {
            // Colour and VT are kept so that only the redirection guard stands between the image and the file.
            TerminalProfile redirected = new TerminalProfile(ColorDepth.TrueColor, GlyphTier.Full, OutputMode.Interactive, 80, 24,
                supportsVirtualTerminal: true, isOutputRedirected: true).WithImageProtocol(ImageProtocol.Kitty);
            StringTerminalWriter writer = new StringTerminalWriter();
            new ConsoleImage(Convert.FromBase64String(DotPng), "dot.png") { Width = 2 }.Write(redirected, writer);
            string output = writer.ToString();
            Assert.Contains("[image: dot.png 2x2]", output);
            Assert.DoesNotContain(Esc + "_G", output);
            Assert.DoesNotContain("▀", output);
        }

        [Fact]
        public void Write_LargeImage_StaysOnScreenAndShrinksBeforeSending() {
            // 400x800 on a six-row screen: five rows, five columns, and a PNG at twice that cell resolution.
            ConsoleImage image = new ConsoleImage(new RgbaImage(400, 800, Solid(400, 800)));
            StringTerminalWriter writer = new StringTerminalWriter();
            image.Write(TerminalProfile.Fixed(80, 6, ColorDepth.TrueColor).WithImageProtocol(ImageProtocol.Kitty), writer);
            string output = writer.ToString();
            Assert.StartsWith("\r\n\r\n\r\n\r\n\r\n" + Esc + "[5A", output);
            Assert.Contains("c=5,r=5,", output);
            string payload = output.Substring(output.IndexOf(";", output.IndexOf(Esc + "_G", StringComparison.Ordinal), StringComparison.Ordinal) + 1);
            RgbaImage sent = RasterImageDecoder.Decode(Convert.FromBase64String(payload.Substring(0, payload.IndexOf(Esc, StringComparison.Ordinal))));
            Assert.Equal(100, sent.Width);
            Assert.Equal(200, sent.Height);
        }

        [Fact]
        public void Write_Kitty_PadsThePngToTheReservedCellsSoItNeverRunsPastThem() {
            // 560x272 at ten columns reserves three rows; the PNG is shrunk to 200x97 and padded to the cells' 10:6 shape.
            ConsoleImage image = new ConsoleImage(new RgbaImage(560, 272, Solid(560, 272))) { Width = 10 };
            string output = Write(image, ImageProtocol.Kitty);
            Assert.Contains("c=10,r=3,", output);
            string payload = output.Substring(output.IndexOf(";", output.IndexOf(Esc + "_G", StringComparison.Ordinal), StringComparison.Ordinal) + 1);
            RgbaImage sent = RasterImageDecoder.Decode(Convert.FromBase64String(payload.Substring(0, payload.IndexOf(Esc, StringComparison.Ordinal))));
            Assert.Equal(200, sent.Width);
            Assert.Equal(120, sent.Height);
            Assert.Equal(255, sent.Pixels[(96 * 200 * 4) + 3]);
            Assert.Equal(0, sent.Pixels[(97 * 200 * 4) + 3]);
        }

        [Fact]
        public void Render_HalfBlocks_KeepAOnePixelLineOnATransparentBackground() {
            // 8x8, transparent except row 3: each cell pixel averages a 4x4 area where the line covers a quarter.
            byte[] pixels = new byte[8 * 8 * 4];
            for (int x = 0; x < 8; x++) { int i = ((3 * 8) + x) * 4; pixels[i] = 255; pixels[i + 3] = 255; }
            CellBuffer cells = Render(new ConsoleImage(new RgbaImage(8, 8, pixels)) { Width = 2 }, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(new Cell("▀", new Style(Color.FromRgb(255, 0, 0))), cells[0, 0]);
            Assert.Equal(new Cell("▀", new Style(Color.FromRgb(255, 0, 0))), cells[1, 0]);
        }

        [Theory]
        [InlineData(0, 0, 80, 4, 1)]   // one cell per 10 pixels: 40x20 px
        [InlineData(20, 0, 80, 20, 5)]
        [InlineData(0, 5, 80, 20, 5)]
        [InlineData(20, 2, 80, 8, 2)]  // fits inside the box
        [InlineData(20, 0, 12, 12, 3)] // narrower space keeps the aspect
        public void Measure_CellSize_KeepsTheAspectRatio(int width, int height, int space, int columns, int rows) {
            ConsoleImage image = new ConsoleImage(new RgbaImage(40, 20, new byte[40 * 20 * 4])) { Width = width, Height = height };
            Size size = image.Measure(new Constraints(space), Terminal.CreateContext(TerminalProfile.Fixed(space, 24, ColorDepth.TrueColor), Theme.Dark));
            Assert.Equal(new Size(columns, rows), size);
        }

        private static string Write(ConsoleImage image, ImageProtocol protocol) {
            StringTerminalWriter writer = new StringTerminalWriter();
            image.Write(TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor).WithImageProtocol(protocol), writer);
            return writer.ToString();
        }

        private static CellBuffer Render(ConsoleImage image, ColorDepth depth, GlyphTier glyphs) =>
            Terminal.Render(image, TerminalProfile.Fixed(10, 4, depth, glyphs), Theme.Dark);

        private static byte[] Solid(int width, int height) {
            byte[] pixels = new byte[width * height * 4];
            for (int i = 0; i < pixels.Length; i += 4) { pixels[i + 2] = 200; pixels[i + 3] = 255; }
            return pixels;
        }

        // Deterministic noise with thousands of distinct colours, so PNG stays large and Sixel must reduce its palette.
        private static RgbaImage Noise(int width, int height) {
            byte[] pixels = new byte[width * height * 4];
            uint state = 2463534242;
            for (int i = 0; i < pixels.Length; i += 4) {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                pixels[i] = (byte)state; pixels[i + 1] = (byte)(state >> 8); pixels[i + 2] = (byte)(state >> 16); pixels[i + 3] = 255;
            }
            return new RgbaImage(width, height, pixels);
        }
    }
}
