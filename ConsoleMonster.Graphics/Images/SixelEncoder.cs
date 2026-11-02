using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;

namespace ConsoleMonster.Graphics {
    // DEC Sixel (VT330/VT340 graphics). Sixel draws at its raw pixel size, so callers render at exactly cells x 10x20 pixels:
    // Windows Terminal lays sixels out on that VT340 cell and scales them to the real font.
    internal static class SixelEncoder {
        internal const int CellWidth = 10;
        internal const int CellHeight = 20;
        private const int MaxColors = 256;

        // P2=1 leaves unpainted (transparent) pixels alone; raster attributes "1;1;W;H set square pixels and the size.
        internal static string Encode(RgbaImage image) {
            int width = image.Width, height = image.Height;
            int[] indices = new int[width * height];
            List<int> palette = Quantize(image, indices);
            StringBuilder sb = new StringBuilder(width * height / 2 + 1024);
            sb.Append("\u001bP0;1;0q\"1;1;").Append(N(width)).Append(';').Append(N(height));
            for (int i = 0; i < palette.Count; i++) {
                int rgb = palette[i];
                sb.Append('#').Append(N(i)).Append(";2;").Append(Percent(rgb >> 16)).Append(';').Append(Percent((rgb >> 8) & 0xFF)).Append(';').Append(Percent(rgb & 0xFF));
            }
            char[] row = new char[width];
            bool[] used = new bool[palette.Count];
            for (int top = 0; top < height; top += 6) {
                if (top > 0) { sb.Append('-'); }
                Array.Clear(used, 0, used.Length);
                int bottom = Math.Min(height, top + 6);
                for (int y = top; y < bottom; y++) {
                    for (int x = 0; x < width; x++) {
                        int index = indices[(y * width) + x];
                        if (index >= 0) { used[index] = true; }
                    }
                }
                bool first = true;
                for (int color = 0; color < palette.Count; color++) {
                    if (!used[color]) { continue; }
                    for (int x = 0; x < width; x++) {
                        int bits = 0;
                        for (int y = top; y < bottom; y++) {
                            if (indices[(y * width) + x] == color) { bits |= 1 << (y - top); }
                        }
                        row[x] = (char)('?' + bits);
                    }
                    int end = width;
                    while (end > 0 && row[end - 1] == '?') { end--; }
                    // '$' returns to the band's start for the next colour; '-' at the next band also returns.
                    if (!first) { sb.Append('$'); }
                    first = false;
                    sb.Append('#').Append(N(color));
                    AppendRuns(sb, row, end);
                }
            }
            return sb.Append("\u001b\\").ToString();
        }

        // Runs of four or more repeat with !count, which is never longer than the literal run.
        private static void AppendRuns(StringBuilder sb, char[] row, int end) {
            for (int x = 0; x < end;) {
                int run = 1;
                while (x + run < end && row[x + run] == row[x]) { run++; }
                if (run >= 4) { sb.Append('!').Append(N(run)).Append(row[x]); }
                else { sb.Append(row[x], run); }
                x += run;
            }
        }

        // Exact colours when the image has at most 256 (charts and diagrams); otherwise median cut over 15-bit colour bins.
        // Pixels under half opacity stay transparent (-1).
        private static List<int> Quantize(RgbaImage image, int[] indices) {
            byte[] px = image.Pixels;
            Dictionary<int, int> exact = new Dictionary<int, int>();
            List<int> palette = new List<int>();
            bool overflow = false;
            for (int p = 0; p < indices.Length; p++) {
                int i = p * 4;
                if (px[i + 3] < 128) { indices[p] = -1; continue; }
                int rgb = (px[i] << 16) | (px[i + 1] << 8) | px[i + 2];
                if (!exact.TryGetValue(rgb, out int index)) {
                    if (palette.Count == MaxColors) { overflow = true; break; }
                    index = palette.Count;
                    exact[rgb] = index;
                    palette.Add(rgb);
                }
                indices[p] = index;
            }
            return overflow ? MedianCut(px, indices) : palette;
        }

        private static List<int> MedianCut(byte[] px, int[] indices) {
            int[] counts = new int[32768];
            long[] sums = new long[32768 * 3];
            for (int p = 0; p < indices.Length; p++) {
                int i = p * 4;
                if (px[i + 3] < 128) { indices[p] = -1; continue; }
                int bin = Bin(px[i], px[i + 1], px[i + 2]);
                counts[bin]++;
                sums[bin * 3] += px[i]; sums[(bin * 3) + 1] += px[i + 1]; sums[(bin * 3) + 2] += px[i + 2];
            }
            List<int> bins = new List<int>();
            for (int bin = 0; bin < counts.Length; bin++) { if (counts[bin] > 0) { bins.Add(bin); } }
            int[] order = bins.ToArray();
            List<int[]> boxes = new List<int[]> { new[] { 0, order.Length } };
            while (boxes.Count < MaxColors) {
                int best = -1, channel = 0;
                long bestScore = 0;
                for (int b = 0; b < boxes.Count; b++) {
                    int[] box = boxes[b];
                    if (box[1] - box[0] < 2) { continue; }
                    long population = 0;
                    int[] min = { 31, 31, 31 }, max = { 0, 0, 0 };
                    for (int k = box[0]; k < box[1]; k++) {
                        population += counts[order[k]];
                        for (int c = 0; c < 3; c++) { int v = Channel(order[k], c); min[c] = Math.Min(min[c], v); max[c] = Math.Max(max[c], v); }
                    }
                    for (int c = 0; c < 3; c++) {
                        long score = population * (max[c] - min[c]);
                        if (score > bestScore) { bestScore = score; best = b; channel = c; }
                    }
                }
                if (best < 0) { break; }
                int[] split = boxes[best];
                int length = split[1] - split[0];
                int[] keys = new int[length], segment = new int[length];
                Array.Copy(order, split[0], segment, 0, length);
                for (int k = 0; k < length; k++) { keys[k] = (Channel(segment[k], channel) << 15) | segment[k]; }
                Array.Sort(keys, segment);
                Array.Copy(segment, 0, order, split[0], length);
                long total = 0, half = 0;
                for (int k = split[0]; k < split[1]; k++) { total += counts[order[k]]; }
                int cut = split[0] + 1;
                for (int k = split[0]; k < split[1] - 1; k++) {
                    half += counts[order[k]];
                    cut = k + 1;
                    if (half * 2 >= total) { break; }
                }
                boxes[best] = new[] { split[0], cut };
                boxes.Add(new[] { cut, split[1] });
            }
            List<int> palette = new List<int>(boxes.Count);
            int[] binIndex = new int[32768];
            for (int b = 0; b < boxes.Count; b++) {
                long n = 0, r = 0, g = 0, bl = 0;
                for (int k = boxes[b][0]; k < boxes[b][1]; k++) {
                    int bin = order[k];
                    n += counts[bin]; r += sums[bin * 3]; g += sums[(bin * 3) + 1]; bl += sums[(bin * 3) + 2];
                    binIndex[bin] = b;
                }
                palette.Add((int)(((r / n) << 16) | ((g / n) << 8) | (bl / n)));
            }
            for (int p = 0; p < indices.Length; p++) {
                int i = p * 4;
                indices[p] = px[i + 3] < 128 ? -1 : binIndex[Bin(px[i], px[i + 1], px[i + 2])];
            }
            return palette;
        }

        private static int Bin(byte r, byte g, byte b) => ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
        private static int Channel(int bin, int channel) => (bin >> (10 - (channel * 5))) & 31;
        private static string Percent(int value) => ((value * 100 + 127) / 255).ToString(CultureInfo.InvariantCulture);
        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
