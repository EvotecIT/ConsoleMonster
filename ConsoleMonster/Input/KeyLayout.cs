using System;

namespace ConsoleMonster.Input {
    /// <summary>
    /// Finds the Latin letter or digit key behind a character typed on another keyboard layout, so a binding written as
    /// <c>q</c> or <c>1</c> still works when a Cyrillic layout types <c>й</c> or a French layout types <c>&amp;</c> on that key.
    /// </summary>
    internal static class KeyLayout {
        // The 26 Latin letter keys and the letters the standard Russian (ЙЦУКЕН) layout puts on them. Ukrainian and
        // Belarusian differ on two keys, listed after. Used when the input source does not report the key itself (VT input).
        private const string Latin = "qwertyuiopasdfghjklzxcvbnm" + "so";
        private const string Cyrillic = "йцукенгшщзфывапролдячсмить" + "іў";

        /// <summary>Whether a typed character is the character chord <paramref name="chord"/> on another layout.</summary>
        internal static bool SameKey(char chord, InputEvent input) {
            if (!input.IsText || IsLatinLetterOrDigit(input.Char)) { return false; }
            ConsoleKey want = InputEvent.KeyFor(chord);
            if (chord >= '0' && chord <= '9') {
                // A French layout types symbols on the unshifted digit row; a shifted symbol (US '!') is not a digit.
                ConsoleKey key = input.PhysicalKey;
                if (key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9) { key = ConsoleKey.D0 + (key - ConsoleKey.NumPad0); }
                return key == want && !input.PhysicalShift;
            }
            if (want == 0 || !char.IsLetter(input.Char) || char.IsUpper(chord) != char.IsUpper(input.Char)) { return false; }
            return (input.PhysicalKey != 0 ? input.PhysicalKey : LatinKey(input.Char)) == want;
        }

        private static ConsoleKey LatinKey(char typed) {
            int index = Cyrillic.IndexOf(char.ToLowerInvariant(typed));
            return index < 0 ? 0 : InputEvent.KeyFor(Latin[index]);
        }

        private static bool IsLatinLetterOrDigit(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
    }
}
