using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using ChartForgeX;
using ChartForgeX.Raster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Graphics {
    /// <summary>
    /// An image drawn inline at a size in cells, keeping its aspect ratio. <see cref="Write"/> prints it with the best protocol
    /// the profile allows (<see cref="TerminalProfile.ImageProtocol"/>): Kitty, iTerm2 or Sixel pixels in terminals that
    /// support them, half blocks on any other 256-colour terminal, and a text placeholder in redirected, CI or no-colour output.
    /// </summary>
    /// <remarks>
    /// As a widget inside layouts, dashboards and live regions it always draws half blocks (or the placeholder): those
    /// outputs diff and redraw cells, so an image stays exact on refresh and an unchanged image writes nothing. Terminal
    /// pixels are print-once only.
    /// </remarks>
    public sealed class ConsoleImage : IWidget {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private readonly RgbaImage _image;
        private readonly byte[]? _png;
        private StrongBox<RgbaImage>? _cells;

        /// <summary>Creates an image from encoded PNG, JPEG, GIF, BMP, TIFF or PPM bytes, for example a ChartForgeX <c>ToPng()</c> render.</summary>
        /// <param name="encoded">The encoded image.</param>
        /// <param name="name">The name shown by the text placeholder.</param>
        /// <exception cref="NotSupportedException">The bytes are not an image format ChartForgeX decodes.</exception>
        /// <exception cref="InvalidDataException">The image is damaged or truncated.</exception>
        public ConsoleImage(byte[] encoded, string? name = null) {
            if (encoded == null) { throw new ArgumentNullException(nameof(encoded)); }
            _image = RasterImageDecoder.Decode(encoded);
            _png = IsPng(encoded) ? encoded : null;
            Name = string.IsNullOrWhiteSpace(name) ? "image" : name!;
        }

        /// <summary>Creates an image from decoded pixels, for example a ChartForgeX raster render. The pixels are copied.</summary>
        /// <param name="image">The RGBA pixels.</param>
        /// <param name="name">The name shown by the text placeholder.</param>
        public ConsoleImage(RgbaImage image, string? name = null) {
            if (image.Pixels == null || image.Width < 1 || image.Height < 1 || image.Pixels.Length < image.Width * image.Height * 4) {
                throw new ArgumentException("The image has no pixels.", nameof(image));
            }
            _image = new RgbaImage(image.Width, image.Height, (byte[])image.Pixels.Clone());
            Name = string.IsNullOrWhiteSpace(name) ? "image" : name!;
        }

        /// <summary>Reads an image file; the file name becomes <see cref="Name"/>.</summary>
        /// <param name="path">A PNG, JPEG, GIF, BMP, TIFF or PPM file.</param>
        /// <returns>The image.</returns>
        public static ConsoleImage FromFile(string path) {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentException("Give the path of an image file.", nameof(path)); }
            return new ConsoleImage(File.ReadAllBytes(path), Path.GetFileName(path));
        }

        /// <summary>The name shown by the text placeholder, <c>[image: name WxH]</c>.</summary>
        public string Name { get; }

        /// <summary>The source width in pixels.</summary>
        public int PixelWidth => _image.Width;

        /// <summary>The source height in pixels.</summary>
        public int PixelHeight => _image.Height;

        /// <summary>The width in cells, or 0 to follow <see cref="Height"/> or one cell per 10 pixels. Always within the space offered.</summary>
        public int Width { get; set; }

        /// <summary>The height in cells, or 0 to follow the width. With both set, the image fits inside that box.</summary>
        public int Height { get; set; }

        /// <summary>The placeholder written where pixels cannot be drawn.</summary>
        public string Placeholder => "[image: " + Name + " " + PixelWidth.ToString(CultureInfo.InvariantCulture) + "x" + PixelHeight.ToString(CultureInfo.InvariantCulture) + "]";

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            if (context.Profile.ImageProtocol == ImageProtocol.None) {
                return new Size(Math.Min(constraints.MaxWidth, DisplayWidth.Measure(Placeholder)), Math.Min(1, constraints.MaxHeight));
            }
            return Cells(constraints.MaxWidth, constraints.MaxHeight);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (context.Profile.ImageProtocol == ImageProtocol.None) {
                region.Write(0, 0, Placeholder, context.Style(ThemeToken.Muted));
                return;
            }
            Size size = Cells(region.Width, region.Height);
            if (size.Width == 0 || size.Height == 0) { return; }
            // Live regions render every frame: resample only when the cell size changes.
            StrongBox<RgbaImage>? box = _cells;
            if (box == null || box.Value.Width != size.Width || box.Value.Height != size.Height * 2) {
                _cells = box = new StrongBox<RgbaImage>(ImageSampler.Resize(_image, size.Width, size.Height * 2, keepDetail: true));
            }
            RgbaImage cached = box.Value;
            byte[] px = cached.Pixels;
            if (context.Profile.Glyphs == GlyphTier.Ascii) {
                // No half-block glyph: one background-coloured space per cell, averaging its two pixels by opacity.
                for (int y = 0; y < size.Height; y++) {
                    for (int x = 0; x < size.Width; x++) {
                        int top = (((y * 2) * size.Width) + x) * 4, bottom = top + (size.Width * 4);
                        int alpha = px[top + 3] + px[bottom + 3];
                        if (alpha < 256) { continue; }
                        Color color = Color.FromRgb(Mix(px, top, bottom, 0, alpha), Mix(px, top, bottom, 1, alpha), Mix(px, top, bottom, 2, alpha));
                        region.Put(x, y, " ", Style.Plain.WithBackgroundColor(color).Downsample(context.Depth));
                    }
                }
                return;
            }
            HalfBlockCanvas canvas = new HalfBlockCanvas(size.Width, size.Height);
            for (int y = 0; y < cached.Height; y++) {
                for (int x = 0; x < cached.Width; x++) {
                    int i = ((y * cached.Width) + x) * 4;
                    if (px[i + 3] >= 128) { canvas.Set(x, y, Color.FromRgb(px[i], px[i + 1], px[i + 2])); }
                }
            }
            canvas.Render(region, 0, 0, context);
        }

        /// <summary>
        /// Prints the image once below the current output: Kitty, iTerm2 or Sixel pixels when the profile allows them, else
        /// the half blocks or placeholder every other output gets. While a live session owns the console the image goes above
        /// it as half blocks. To choose a protocol, pass <c>Terminal.GetCurrentProfile().WithImageProtocol(...)</c>.
        /// </summary>
        /// <param name="profile">The profile, or null for <see cref="Terminal.GetCurrentProfile"/>.</param>
        /// <param name="writer">The sink, or null for the console. The caller keeps ownership of a given writer.</param>
        public void Write(TerminalProfile? profile = null, ITerminalWriter? writer = null) {
            TerminalProfile p = profile ?? Terminal.GetCurrentProfile();
            ImageProtocol protocol = p.ImageProtocol;
            if (protocol == ImageProtocol.None || protocol == ImageProtocol.HalfBlock || (writer == null && Terminal.ConsoleOwner != null)) {
                Terminal.Write(this, p, writer);
                return;
            }
            // Keep the whole image on screen: the rows are reserved first and the cursor cannot move above the top.
            Size size = Cells(p.Width, Math.Max(1, p.Height - 1), fit: true);
            Terminal.WriteEncoded(ImageSequences.Place(Encode(protocol, size), size.Height), p, writer);
        }

        // The image never runs past the reserved cells: Sixel is drawn at its pixel size, so it is rendered at 10 pixels per
        // column with its own height; Kitty stretches to the cells it is given, so its PNG is padded with transparency to the
        // cells' shape; iTerm2 preserves the ratio inside its box. Kitty and iTerm2 PNGs keep up to twice the cell resolution
        // (sharp on high-DPI screens); only larger images shrink before they are sent.
        internal string Encode(ImageProtocol protocol, Size size) {
            if (protocol == ImageProtocol.Sixel) {
                int width = size.Width * SixelEncoder.CellWidth;
                int height = (int)Math.Round((double)_image.Height * width / _image.Width);
                if (height > size.Height * SixelEncoder.CellHeight) {
                    height = size.Height * SixelEncoder.CellHeight;
                    width = (int)Math.Round((double)_image.Width * height / _image.Height);
                }
                return SixelEncoder.Encode(ImageSampler.Resize(_image, Math.Max(1, width), Math.Max(1, height), keepDetail: true));
            }
            double scale = Math.Min(1, Math.Min(size.Width * SixelEncoder.CellWidth * 2.0 / _image.Width, size.Height * SixelEncoder.CellHeight * 2.0 / _image.Height));
            RgbaImage image = scale < 1
                ? ImageSampler.Resize(_image, Math.Max(1, (int)Math.Round(_image.Width * scale)), Math.Max(1, (int)Math.Round(_image.Height * scale)))
                : _image;
            if (protocol == ImageProtocol.Kitty) {
                RgbaImage boxed = Pad(image, size);
                byte[] kitty = scale >= 1 && boxed.Width == _image.Width && boxed.Height == _image.Height ? _png ?? boxed.ToPng() : boxed.ToPng();
                return ImageSequences.Kitty(kitty, size.Width, size.Height);
            }
            return ImageSequences.ITerm2(scale < 1 ? image.ToPng() : _png ?? _image.ToPng(), Name, size.Width, size.Height);
        }

        // Places the image at the top left of a transparent canvas shaped like the cells (one column to two pixel rows).
        private static RgbaImage Pad(RgbaImage image, Size size) {
            int width = Math.Max(image.Width, (int)Math.Ceiling((double)image.Height * size.Width / (size.Height * 2) - 1e-9));
            int height = Math.Max(image.Height, (int)Math.Ceiling((double)image.Width * size.Height * 2 / size.Width - 1e-9));
            if (width == image.Width && height == image.Height) { return image; }
            byte[] canvas = new byte[width * height * 4];
            for (int y = 0; y < image.Height; y++) {
                Array.Copy(image.Pixels, y * image.Width * 4, canvas, y * width * 4, image.Width * 4);
            }
            return new RgbaImage(width, height, canvas);
        }

        // A cell is about twice as tall as it is wide, so one cell row covers two pixel rows for every pixel column. Half
        // blocks round to the nearest cell; terminal pixels (fit) reserve enough rows for the whole image.
        internal Size Cells(int maxWidth, int maxHeight, bool fit = false) {
            double rowsPerColumn = _image.Height / (double)_image.Width / 2;
            int columns, rows;
            if (Width > 0 && Height > 0) {
                columns = Math.Min(Width, Columns(Height, rowsPerColumn, fit));
                rows = Math.Min(Height, Rows(columns, rowsPerColumn, fit));
            } else if (Height > 0) {
                rows = Height;
                columns = Columns(rows, rowsPerColumn, fit);
            } else {
                columns = Width > 0 ? Width : (_image.Width + SixelEncoder.CellWidth - 1) / SixelEncoder.CellWidth;
                rows = Rows(columns, rowsPerColumn, fit);
            }
            if (columns > maxWidth) {
                columns = maxWidth;
                rows = Rows(columns, rowsPerColumn, fit);
            }
            if (rows > maxHeight) {
                rows = maxHeight;
                columns = Math.Min(columns, Columns(rows, rowsPerColumn, fit));
            }
            return columns <= 0 || rows <= 0 ? Size.Empty : new Size(columns, rows);
        }

        private static int Rows(int columns, double rowsPerColumn, bool fit) =>
            columns <= 0 ? 0 : Math.Max(1, (int)(fit ? Math.Ceiling((columns * rowsPerColumn) - 1e-9) : Math.Round(columns * rowsPerColumn)));
        private static int Columns(int rows, double rowsPerColumn, bool fit) =>
            rows <= 0 ? 0 : Math.Max(1, (int)(fit ? Math.Floor((rows / rowsPerColumn) + 1e-9) : Math.Round(rows / rowsPerColumn)));
        private static byte Mix(byte[] px, int top, int bottom, int channel, int alpha) =>
            (byte)(((px[top + channel] * px[top + 3]) + (px[bottom + channel] * px[bottom + 3]) + (alpha / 2)) / alpha);

        private static bool IsPng(byte[] data) {
            if (data.Length < PngSignature.Length) { return false; }
            for (int i = 0; i < PngSignature.Length; i++) { if (data[i] != PngSignature[i]) { return false; } }
            return true;
        }
    }
}
