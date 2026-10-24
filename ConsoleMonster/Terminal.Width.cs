using System;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster {
    public static partial class Terminal {
        /// <summary>Resolves an automatic width from the current output target; unknown widths use 100 cells.</summary>
        /// <param name="width">An explicit positive width, or zero for the current target.</param>
        /// <returns>A positive width in terminal cells.</returns>
        public static int ResolveWidth(int width = 0) {
            if (width < 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (width > 0) { return width; }
            int detected = GetCurrentProfile().Width;
            return detected > 0 ? detected : TerminalProfile.DefaultWidth;
        }
    }
}
