using System;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>Host gestures for one app. Null releases a gesture to the app registry. Ctrl+C always remains an exit/cancel fallback.</summary>
    public sealed class AppHostKeys {
        /// <summary>Focus the next input; null lets the registry claim Tab.</summary>
        public KeyChord? NextFocus { get; set; } = new KeyChord(ConsoleKey.Tab);
        /// <summary>Focus the previous input.</summary>
        public KeyChord? PreviousFocus { get; set; } = new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift);
        /// <summary>Leave the app when the focused input does not consume this key.</summary>
        public KeyChord? Exit { get; set; } = new KeyChord(ConsoleKey.Escape);
        /// <summary>Cancel the active operation before ordinary routing. Null uses Ctrl+C alone.</summary>
        public KeyChord? CancelAction { get; set; } = new KeyChord(ConsoleKey.F6);
        /// <summary>Open the action palette.</summary>
        public KeyChord? Palette { get; set; } = new KeyChord(ConsoleKey.K, KeyModifiers.Control);
        /// <summary>Focus the action buttons.</summary>
        public KeyChord? Actions { get; set; } = new KeyChord(ConsoleKey.F8);

        internal bool Reserved(KeyChord chord) => chord.Equals(new KeyChord(ConsoleKey.C, KeyModifiers.Control))
            || Equals(NextFocus, chord) || Equals(PreviousFocus, chord) || Equals(Exit, chord)
            || Equals(CancelAction, chord) || Equals(Palette, chord) || Equals(Actions, chord);
        internal static bool Match(KeyChord? chord, InputEvent input) => chord.HasValue && chord.Value.Matches(input);
    }
}
