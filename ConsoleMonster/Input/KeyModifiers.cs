using System;

namespace ConsoleMonster.Input {
    /// <summary>Modifier keys held with a key press.</summary>
    [Flags]
    public enum KeyModifiers {
        /// <summary>No modifier.</summary>
        None = 0,

        /// <summary>Shift. Printable characters never carry it: the character itself already reflects Shift.</summary>
        Shift = 1,

        /// <summary>Alt (Option on macOS). AltGr is reported as the character it produces, without modifiers.</summary>
        Alt = 2,

        /// <summary>Control.</summary>
        Control = 4
    }
}
