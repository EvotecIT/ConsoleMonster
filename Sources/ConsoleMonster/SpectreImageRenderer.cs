using System;
using System.IO;
using System.Threading;
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
    public static Canvas Load(string filePath, int? maxWidth) => Load(filePath, maxWidth, CancellationToken.None);

    /// <summary>Loads and fits an image while observing cancellation during decoding, resizing, and canvas population.</summary>
    /// <param name="filePath">The raster image to decode.</param>
    /// <param name="maxWidth">Optional maximum width, further limited by the terminal.</param>
    /// <param name="cancellationToken">Cancels preparation before the complete canvas is returned.</param>
    /// <returns>A complete terminal canvas.</returns>
    public static Canvas Load(string filePath, int? maxWidth, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (filePath == null) {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (maxWidth.HasValue && maxWidth.Value < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        }
        using var stream = File.OpenRead(filePath);
        var image = OfficeRasterImageDecoder.Decode(stream,
            new OfficeRasterDecodeOptions { CancellationToken = cancellationToken });
        var terminalWidth = Math.Max(1, AnsiConsole.Profile.Width / (AnsiConsole.Profile.Capabilities.Unicode ? 1 : 2));
        var width = Math.Min(image.Width, Math.Min(maxWidth ?? image.Width, terminalWidth));
        if (width != image.Width) {
            image = OfficeRasterResampler.Resize(image,
                new OfficeRasterResizeOptions { Width = width, Fit = OfficeImageFit.Contain }, cancellationToken);
        }
        var canvas = new Canvas(image.Width, image.Height);
        for (var y = 0; y < image.Height; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < image.Width; x++) {
                if ((x & 1023) == 0) {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                var pixel = image.GetPixel(x, y);
                if (pixel.A != 0) {
                    canvas.SetPixel(x, y, new Color(pixel.R, pixel.G, pixel.B));
                }
            }
        }
        return canvas;
    }
}
