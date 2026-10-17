using System;

namespace ConsoleMonster.Styling {
    /// <summary>Text decorations, combinable as flags.</summary>
    [Flags]
    public enum TextDecoration : byte {
        /// <summary>No decoration.</summary>
        None = 0,

        /// <summary>Bold (or bright on terminals that map bold to intensity).</summary>
        Bold = 1,

        /// <summary>Dim / faint.</summary>
        Dim = 2,

        /// <summary>Italic.</summary>
        Italic = 4,

        /// <summary>Underline.</summary>
        Underline = 8,

        /// <summary>Strikethrough.</summary>
        Strikethrough = 16,

        /// <summary>Swap foreground and background.</summary>
        Reverse = 32
    }
}
