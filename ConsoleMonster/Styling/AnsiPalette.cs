using System;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Reference RGB values for the 16 and 256 colour palettes, used to downsample colours to a lower tier.
    /// The 16 entries follow the Windows "Campbell" scheme used by conhost and Windows Terminal, because that is
    /// what Windows users actually see for indices 0-15.
    /// </summary>
    internal static class AnsiPalette {
        private static readonly byte[,] Standard16 = {
            { 12, 12, 12 }, { 197, 15, 31 }, { 19, 161, 14 }, { 193, 156, 0 },
            { 0, 55, 218 }, { 136, 23, 152 }, { 58, 150, 221 }, { 204, 204, 204 },
            { 118, 118, 118 }, { 231, 72, 86 }, { 22, 198, 12 }, { 249, 241, 165 },
            { 59, 120, 255 }, { 180, 0, 158 }, { 97, 214, 214 }, { 242, 242, 242 }
        };

        private static readonly byte[] CubeLevels = { 0, 95, 135, 175, 215, 255 };

        /// <summary>Returns the RGB value of a 16-colour index.</summary>
        public static void Get16(int index, out byte r, out byte g, out byte b) {
            int i = index & 15;
            r = Standard16[i, 0];
            g = Standard16[i, 1];
            b = Standard16[i, 2];
        }

        /// <summary>Returns the RGB value of a 256-colour index.</summary>
        public static void Get256(int index, out byte r, out byte g, out byte b) {
            index &= 255;
            if (index < 16) {
                Get16(index, out r, out g, out b);
                return;
            }
            if (index >= 232) {
                byte level = (byte)(8 + ((index - 232) * 10));
                r = level;
                g = level;
                b = level;
                return;
            }
            int cube = index - 16;
            r = CubeLevels[cube / 36];
            g = CubeLevels[(cube / 6) % 6];
            b = CubeLevels[cube % 6];
        }

        /// <summary>Finds the nearest 256-colour index (cube or greyscale ramp; indices 0-15 are skipped because their RGB varies by terminal).</summary>
        public static byte Nearest256(byte r, byte g, byte b) {
            int ri = NearestCubeIndex(r);
            int gi = NearestCubeIndex(g);
            int bi = NearestCubeIndex(b);
            int cubeIndex = 16 + (36 * ri) + (6 * gi) + bi;
            int cubeDistance = Distance(r, g, b, CubeLevels[ri], CubeLevels[gi], CubeLevels[bi]);

            int average = (r + g + b) / 3;
            int greyStep = average < 8 ? 0 : Math.Min(23, (average - 8 + 5) / 10);
            byte grey = (byte)(8 + (greyStep * 10));
            int greyDistance = Distance(r, g, b, grey, grey, grey);

            return greyDistance < cubeDistance ? (byte)(232 + greyStep) : (byte)cubeIndex;
        }

        /// <summary>Finds the nearest 16-colour index using a perceptual ("redmean") distance.</summary>
        public static byte Nearest16(byte r, byte g, byte b) {
            int best = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < 16; i++) {
                int d = Distance(r, g, b, Standard16[i, 0], Standard16[i, 1], Standard16[i, 2]);
                if (d < bestDistance) {
                    bestDistance = d;
                    best = i;
                }
            }
            return (byte)best;
        }

        private static int NearestCubeIndex(byte value) {
            if (value < 48) {
                return 0;
            }
            if (value < 115) {
                return 1;
            }
            return Math.Min(5, ((value - 35) / 40));
        }

        private static int Distance(int r1, int g1, int b1, int r2, int g2, int b2) {
            int rmean = (r1 + r2) / 2;
            int dr = r1 - r2;
            int dg = g1 - g2;
            int db = b1 - b2;
            return (((512 + rmean) * dr * dr) >> 8) + (4 * dg * dg) + (((767 - rmean) * db * db) >> 8);
        }
    }
}
