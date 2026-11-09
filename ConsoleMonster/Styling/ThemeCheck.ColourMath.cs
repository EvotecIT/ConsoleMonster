using System;

namespace ConsoleMonster.Styling {
    public static partial class ThemeCheck {
        // Machado, Oliveira and Fernandes (2009), severity 1.0, applied to linear sRGB.
        // https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html
        private static readonly double[][,] Simulations = {
            new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } },
            new double[,] { { 0.152286, 1.052583, -0.204868 }, { 0.114503, 0.786281, 0.099216 }, { -0.003882, -0.048116, 1.051998 } },
            new double[,] { { 0.367322, 0.860646, -0.227968 }, { 0.280085, 0.672501, 0.047413 }, { -0.011820, 0.042940, 0.968881 } },
            new double[,] { { 1.255528, -0.076749, -0.178779 }, { -0.078411, 0.930809, 0.147602 }, { 0.004733, 0.691367, 0.303900 } }
        };
        private static readonly string[] VisionNames = { "normal vision", "protanopia", "deuteranopia", "tritanopia" };

        private static double Linear(byte value) {
            double v = value / 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        private static double Luminance(Color c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

        internal static double Contrast(Color a, Color b) {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        internal static double MinimumStatusDistance(Color a, Color b) {
            double minimum = double.MaxValue;
            foreach (double[,] simulation in Simulations) { minimum = Math.Min(minimum, Distance(Lab(a, simulation), Lab(b, simulation))); }
            return minimum;
        }

        private static double[] Lab(Color c, double[,] matrix) {
            double r = Linear(c.R), g = Linear(c.G), b = Linear(c.B);
            double red = Clamp(matrix[0, 0] * r + matrix[0, 1] * g + matrix[0, 2] * b);
            double green = Clamp(matrix[1, 0] * r + matrix[1, 1] * g + matrix[1, 2] * b);
            double blue = Clamp(matrix[2, 0] * r + matrix[2, 1] * g + matrix[2, 2] * b);
            double x = (0.4124 * red + 0.3576 * green + 0.1805 * blue) / 0.95047;
            double y = 0.2126 * red + 0.7152 * green + 0.0722 * blue;
            double z = (0.0193 * red + 0.1192 * green + 0.9505 * blue) / 1.08883;
            return new[] { 116 * LabCurve(y) - 16, 500 * (LabCurve(x) - LabCurve(y)), 200 * (LabCurve(y) - LabCurve(z)) };
        }

        private static double Clamp(double value) => Math.Min(1, Math.Max(0, value));
        private static double LabCurve(double value) => value > 0.008856 ? Math.Pow(value, 1.0 / 3) : 7.787 * value + 16.0 / 116;
        private static double Distance(double[] a, double[] b) => Math.Sqrt(Square(a[0] - b[0]) + Square(a[1] - b[1]) + Square(a[2] - b[2]));
        private static double Square(double value) => value * value;
    }
}
