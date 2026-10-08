using System;
using System.IO;
using OfficeIMO.Drawing;
using Spectre.Console;

namespace ConsoleMonster;

/// <summary>Loads raster images through the shared managed image decoder for terminal display.</summary>
public static class SpectreImageRenderer {
    /// <summary>Loads a file into a Spectre canvas, preserving transparent pixels and fitting the terminal width.</summary>
    /// <param name="filePath">The raster image to decode.</param>
    /// <returns>A canvas whose width may be limited by the caller.</returns>
    public static Canvas Load(string filePath) => Load(filePath, null);

    /// <summary>Loads an image with bicubic sampling when it must be resized for terminal display.</summary>
    /// <param name="filePath">The raster image to decode.</param>
    /// <param name="maxWidth">Optional maximum image width in pixels.</param>
    /// <returns>A canvas resized to fit the terminal and the requested maximum width.</returns>
    public static Canvas Load(string filePath, int? maxWidth) {
        if (filePath == null) {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (maxWidth.HasValue && maxWidth.Value < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        }
        using var stream = File.OpenRead(filePath);
        if (!OfficeRasterImageDecoder.TryDecode(stream, out var image) || image == null) {
            throw new InvalidDataException("The image format or encoding is not supported by the managed raster decoder.");
        }
        var terminalWidth = Math.Max(1, AnsiConsole.Profile.Width / (AnsiConsole.Profile.Capabilities.Unicode ? 1 : 2));
        var width = Math.Min(image.Width, Math.Min(maxWidth ?? image.Width, terminalWidth));
        if (width != image.Width) {
            var height = Math.Max(1, (int)((long)image.Height * width / image.Width));
            image = OfficeRasterResampler.Resize(image, width, height, OfficeRasterResamplingMode.Bicubic);
        }
        var canvas = new Canvas(image.Width, image.Height);
        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                var pixel = image.GetPixel(x, y);
                if (pixel.A != 0) {
                    canvas.SetPixel(x, y, new Color(pixel.R, pixel.G, pixel.B));
                }
            }
        }
        return canvas;
    }
}
