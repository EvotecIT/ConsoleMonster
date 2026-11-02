using System;
using ChartForgeX.Raster;

namespace ConsoleMonster.Graphics {
    // Area-average resampling for half blocks, Sixel and protocol payloads. Colour is weighted by alpha so transparent
    // pixels never darken an edge; enlarging repeats the nearest source pixel.
    internal static class ImageSampler {
        // keepDetail pulls each averaged pixel halfway towards the source pixel that differs most from that average, and its
        // opacity halfway towards the most opaque one. At two pixels per cell, plain averaging fades a one-pixel chart line
        // or diagram edge to a pale smear, or drops it altogether on a transparent background.
        internal static RgbaImage Resize(RgbaImage source, int width, int height, bool keepDetail = false) {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (source.Width == width && source.Height == height) { return source; }
            byte[] input = source.Pixels, output = new byte[width * height * 4];
            for (int y = 0; y < height; y++) {
                int y0 = (int)((long)y * source.Height / height), y1 = Math.Max(y0 + 1, (int)((long)(y + 1) * source.Height / height));
                for (int x = 0; x < width; x++) {
                    int x0 = (int)((long)x * source.Width / width), x1 = Math.Max(x0 + 1, (int)((long)(x + 1) * source.Width / width));
                    long r = 0, g = 0, b = 0, a = 0, count = 0;
                    int opaque = 0;
                    for (int sy = y0; sy < y1; sy++) {
                        int row = sy * source.Width * 4;
                        for (int sx = x0; sx < x1; sx++) {
                            int i = row + (sx * 4);
                            int alpha = input[i + 3];
                            r += input[i] * alpha; g += input[i + 1] * alpha; b += input[i + 2] * alpha; a += alpha;
                            opaque = Math.Max(opaque, alpha);
                            count++;
                        }
                    }
                    int o = ((y * width) + x) * 4;
                    int average = (int)((a + (count / 2)) / count);
                    output[o + 3] = (byte)(keepDetail ? (average + opaque + 1) / 2 : average);
                    if (a == 0) { continue; }
                    int ar = (int)((r + (a / 2)) / a), ag = (int)((g + (a / 2)) / a), ab = (int)((b + (a / 2)) / a);
                    if (keepDetail && count > 1) {
                        int far = -1, distance = 0;
                        for (int sy = y0; sy < y1; sy++) {
                            int row = sy * source.Width * 4;
                            for (int sx = x0; sx < x1; sx++) {
                                int i = row + (sx * 4);
                                if (input[i + 3] < 128) { continue; }
                                int dr = input[i] - ar, dg = input[i + 1] - ag, db = input[i + 2] - ab;
                                int d = (dr * dr) + (dg * dg) + (db * db);
                                if (d > distance) { distance = d; far = i; }
                            }
                        }
                        if (far >= 0) {
                            ar = (ar + input[far] + 1) / 2; ag = (ag + input[far + 1] + 1) / 2; ab = (ab + input[far + 2] + 1) / 2;
                        }
                    }
                    output[o] = (byte)ar; output[o + 1] = (byte)ag; output[o + 2] = (byte)ab;
                }
            }
            return new RgbaImage(width, height, output);
        }
    }
}
