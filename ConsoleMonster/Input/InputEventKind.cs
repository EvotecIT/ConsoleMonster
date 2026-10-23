namespace ConsoleMonster.Input {
    /// <summary>What an <see cref="InputEvent"/> describes.</summary>
    public enum InputEventKind {
        /// <summary>A key press (including printable characters).</summary>
        Key,

        /// <summary>Text pasted in one go (bracketed paste, or a burst of characters read together).</summary>
        Paste,

        /// <summary>The window changed size.</summary>
        Resize,

        /// <summary>The window gained keyboard focus.</summary>
        FocusGained,

        /// <summary>The window lost keyboard focus.</summary>
        FocusLost,

        /// <summary>A mouse button was pressed or released at a character-cell position.</summary>
        Mouse
    }
}
