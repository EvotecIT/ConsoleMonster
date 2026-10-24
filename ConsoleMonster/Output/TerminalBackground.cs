using System;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {

    /// <summary>Detects terminal background appearance using console, startup VT query and environment information.</summary>
    public static partial class TerminalBackground {
        /// <summary>Detects the background from console attributes, a cached input-startup query or the environment without reading input.</summary>
        /// <param name="info">The detected background or an unknown result.</param>
        /// <returns>True when a background classification was available.</returns>
        public static bool TryDetect(out TerminalBackgroundInfo info) {
            try {
                if (WindowsConsole.IsWindows && WindowsConsole.TryGetBackgroundColor(WindowsConsole.StandardOutput, out uint color, out int index)) {
                    byte red = (byte)(color & 0xFF);
                    byte green = (byte)((color >> 8) & 0xFF);
                    byte blue = (byte)((color >> 16) & 0xFF);
                    double luminance = CalculateLuminance(red, green, blue);
                    info = new TerminalBackgroundInfo(luminance > 0.5, true, red, green, blue, luminance,
                        "console color table", "background attribute " + index + " resolved from the console color table; ConPTY may expose a default table");
                    return true;
                }
                if (_oscBackground != null) { info = _oscBackground; return true; }
                string? colorFgbg = Environment.GetEnvironmentVariable("COLORFGBG");
                if (TryParseColorFgbg(colorFgbg, out bool isLight)) {
                    info = new TerminalBackgroundInfo(isLight, false, 0, 0, 0, null, "COLORFGBG",
                        "background classified from COLORFGBG");
                    return true;
                }
                info = new TerminalBackgroundInfo(false, false, 0, 0, 0, null, "unknown", "terminal background is unavailable from supported output-side or environment signals");
                return false;
            } catch (Exception ex) {
                info = new TerminalBackgroundInfo(false, false, 0, 0, 0, null, "unknown", "terminal background detection failed: " + ex.GetType().Name);
                return false;
            }
        }

        /// <summary>Calculates WCAG relative luminance from 8-bit sRGB channels.</summary>
        public static double CalculateLuminance(byte red, byte green, byte blue) {
            return 0.2126 * Linear(red / 255.0) + 0.7152 * Linear(green / 255.0) + 0.0722 * Linear(blue / 255.0);
        }

        /// <summary>Extracts the background palette index from console attributes.</summary>
        public static int GetAttributeIndex(ushort attributes) => (attributes >> 4) & 0x0F;

        /// <summary>Parses a COLORFGBG foreground/background value.</summary>
        /// <param name="value">A semicolon-separated foreground and background index.</param>
        /// <param name="isLight">Whether the valid background index denotes a light colour.</param>
        /// <returns>True when the background index was valid.</returns>
        public static bool TryParseColorFgbg(string? value, out bool isLight) {
            isLight = false;
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }
            string[] parts = value!.Split(';');
            if (parts.Length != 2 || !int.TryParse((parts.Length > 1 ? parts[1] : null)?.Trim(), out int background) || background < 0 || background > 15) {
                return false;
            }
            isLight = background == 7 || background == 15;
            return true;
        }

        private static double Linear(double value) => value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
