using System;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Graphics {
    // Terminal image escape sequences. Kitty: https://sw.kovidgoyal.net/kitty/graphics-protocol/ ;
    // iTerm2: https://iterm2.com/documentation-images.html . Both scale the PNG to the requested cells.
    internal static class ImageSequences {
        // Kitty allows at most 4096 base64 bytes per chunk; every chunk but the last is a multiple of four.
        internal const int KittyChunk = 4096;

        // a=T transmits and displays, f=100 is PNG, q=2 suppresses replies (they would land in the shell's input),
        // C=1 keeps the cursor still, c and r fill exactly the reserved cells (the PNG is already shaped like them).
        // Continuation chunks carry only m and q, as the protocol requires.
        internal static string Kitty(byte[] png, int columns, int rows) {
            string data = Convert.ToBase64String(png);
            StringBuilder sb = new StringBuilder(data.Length + ((data.Length / KittyChunk) + 1) * 24);
            for (int start = 0; start < data.Length; start += KittyChunk) {
                int length = Math.Min(KittyChunk, data.Length - start);
                sb.Append("\u001b_G");
                if (start == 0) {
                    sb.Append("a=T,f=100,q=2,C=1,c=").Append(N(columns)).Append(",r=").Append(N(rows)).Append(',');
                } else {
                    sb.Append("q=2,");
                }
                sb.Append("m=").Append(start + length < data.Length ? '1' : '0').Append(';').Append(data, start, length).Append("\u001b\\");
            }
            return sb.ToString();
        }

        // OSC 1337 File with inline=1; width and height in cells, aspect ratio preserved inside that box.
        internal static string ITerm2(byte[] png, string name, int columns, int rows) {
            return "\u001b]1337;File=name=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name)) + ";size=" + N(png.Length)
                + ";width=" + N(columns) + ";height=" + N(rows) + ";inline=1:" + Convert.ToBase64String(png) + "\u0007";
        }

        // Reserve the rows first (scrolling if the prompt is at the bottom), draw at the top-left with the cursor saved,
        // then continue on the line under the image. Every protocol moves the cursor differently; this keeps them identical.
        // Like every print-once block, it starts at the beginning of a line.
        internal static string Place(string image, int rows) {
            StringBuilder sb = new StringBuilder(image.Length + (rows * 2) + 24);
            for (int i = 0; i < rows; i++) { sb.Append("\r\n"); }
            sb.Append("\u001b[").Append(N(rows)).Append('A').Append("\u001b7").Append(image).Append("\u001b8");
            sb.Append("\u001b[").Append(N(rows)).Append('B');
            return sb.ToString();
        }

        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
