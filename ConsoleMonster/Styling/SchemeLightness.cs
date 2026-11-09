using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Styling {
    // OKLCH lightness changes keep the Oklab a:b direction (hue); reduce chroma only at the sRGB boundary.
    // Ottosson's public-domain conversion matrices: https://bottosson.github.io/posts/oklab/
    internal readonly struct SchemeLightness {
        internal SchemeLightness(Color color) {
            double r = Linear(color.R), g = Linear(color.G), b = Linear(color.B);
            double l = Math.Pow(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b, 1.0 / 3);
            double m = Math.Pow(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b, 1.0 / 3);
            double s = Math.Pow(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b, 1.0 / 3);
            Lightness = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
            A = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
            B = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        }
        internal double Lightness { get; }
        internal double A { get; }
        internal double B { get; }

        internal Color At(double lightness) {
            double scale = 1;
            if (!InGamut(Channels(lightness, scale))) {
                double low = 0, high = 1;
                for (int i = 0; i < 24; i++) {
                    double middle = (low + high) / 2;
                    if (InGamut(Channels(lightness, middle))) { low = middle; } else { high = middle; }
                }
                scale = low;
            }
            double[] rgb = Channels(lightness, scale);
            return Color.FromRgb(Encode(rgb[0]), Encode(rgb[1]), Encode(rgb[2]));
        }

        internal static List<Color> Candidates(Color original, Color background, double contrast) {
            SchemeLightness lch = new SchemeLightness(original);
            List<Color> colors = new List<Color> { original };
            for (int i = 0; i <= 200; i++) { colors.Add(lch.At(i / 200.0)); }
            return colors.Distinct().Where(color => ThemeCheck.Contrast(color, background) >= contrast)
                .OrderBy(color => Math.Abs(new SchemeLightness(color).Lightness - lch.Lightness)).ToList();
        }

        private double[] Channels(double lightness, double scale) {
            double a = A * scale, b = B * scale;
            double l = Cube(lightness + 0.3963377774 * a + 0.2158037573 * b);
            double m = Cube(lightness - 0.1055613458 * a - 0.0638541728 * b);
            double s = Cube(lightness - 0.0894841775 * a - 1.2914855480 * b);
            return new[] { 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
                -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
                -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s };
        }
        private static bool InGamut(double[] rgb) => rgb.All(value => value >= -0.0000001 && value <= 1.0000001);
        private static double Cube(double value) => value * value * value;
        private static double Linear(byte value) { double c = value / 255.0; return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        private static byte Encode(double value) {
            value = Math.Min(1, Math.Max(0, value));
            return (byte)Math.Round(255 * (value <= 0.0031308 ? value * 12.92 : 1.055 * Math.Pow(value, 1 / 2.4) - 0.055));
        }
    }
}
