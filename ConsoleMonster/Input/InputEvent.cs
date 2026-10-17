using System;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Input {
    /// <summary>
    /// One input event from any source (Windows console records, VT sequences, the PowerShell host, a script in tests),
    /// normalized so controls never care where it came from:
    /// <list type="bullet">
    /// <item>printable characters have <see cref="Char"/> set, no Shift modifier, and <see cref="Key"/> derived from the
    /// character where one exists (letters, digits, space);</item>
    /// <item>Enter, Tab, Backspace and Escape carry their control character in <see cref="Char"/>;</item>
    /// <item>Ctrl+letter has <see cref="Key"/> set to the letter, <see cref="KeyModifiers.Control"/> and no character;</item>
    /// <item>navigation and function keys have no character.</item>
    /// </list>
    /// </summary>
    public sealed class InputEvent {
        private InputEvent(InputEventKind kind, ConsoleKey key, char character, KeyModifiers modifiers, string? text, int width, int height) {
            Kind = kind;
            Key = key;
            Char = character;
            Modifiers = modifiers;
            Text = text;
            Width = width;
            Height = height;
        }

        /// <summary>What the event describes.</summary>
        public InputEventKind Kind { get; }

        /// <summary>The key, or <c>0</c> for characters without a key of their own (punctuation, letters outside A–Z).</summary>
        public ConsoleKey Key { get; }

        /// <summary>The character the key produced, or <c>'\0'</c>.</summary>
        public char Char { get; }

        /// <summary>The modifiers held.</summary>
        public KeyModifiers Modifiers { get; }

        /// <summary>The pasted text (<see cref="InputEventKind.Paste"/>).</summary>
        public string? Text { get; }

        /// <summary>The new window width (<see cref="InputEventKind.Resize"/>).</summary>
        public int Width { get; }

        /// <summary>The new window height (<see cref="InputEventKind.Resize"/>).</summary>
        public int Height { get; }

        /// <summary>Whether this is a printable character typed without Ctrl or Alt.</summary>
        public bool IsText => Kind == InputEventKind.Key && Char >= ' ' && Char != '\u007f' && (Modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0;

        /// <summary>Creates a key press.</summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The modifiers.</param>
        /// <param name="character">The character, or <c>'\0'</c> to use the key's usual control character (Enter, Tab, Backspace, Escape).</param>
        /// <returns>The event.</returns>
        public static InputEvent Press(ConsoleKey key, KeyModifiers modifiers = KeyModifiers.None, char character = '\0') {
            if (character == '\0' && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0) {
                character = ControlCharacter(key);
            }
            return new InputEvent(InputEventKind.Key, key, character, modifiers, null, 0, 0);
        }

        /// <summary>Creates a typed character. Shift is dropped (the character reflects it); Alt and Control are kept.</summary>
        /// <param name="character">The character.</param>
        /// <param name="modifiers">The modifiers.</param>
        /// <returns>The event.</returns>
        public static InputEvent Character(char character, KeyModifiers modifiers = KeyModifiers.None) {
            return new InputEvent(InputEventKind.Key, KeyFor(character), character, modifiers & ~KeyModifiers.Shift, null, 0, 0);
        }

        /// <summary>Creates a paste.</summary>
        /// <param name="text">The pasted text.</param>
        /// <returns>The event.</returns>
        public static InputEvent Paste(string text) {
            return new InputEvent(InputEventKind.Paste, 0, '\0', KeyModifiers.None, text ?? throw new ArgumentNullException(nameof(text)), 0, 0);
        }

        /// <summary>Creates a resize notification.</summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        /// <returns>The event.</returns>
        public static InputEvent Resized(int width, int height) => new InputEvent(InputEventKind.Resize, 0, '\0', KeyModifiers.None, null, width, height);

        /// <summary>Creates a focus notification.</summary>
        /// <param name="gained">Whether focus was gained (true) or lost.</param>
        /// <returns>The event.</returns>
        public static InputEvent Focus(bool gained) => new InputEvent(gained ? InputEventKind.FocusGained : InputEventKind.FocusLost, 0, '\0', KeyModifiers.None, null, 0, 0);

        /// <summary>Whether this is a press of <paramref name="key"/> with exactly <paramref name="modifiers"/>.</summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The modifiers.</param>
        /// <returns>True on a match.</returns>
        public bool Is(ConsoleKey key, KeyModifiers modifiers = KeyModifiers.None) => Kind == InputEventKind.Key && Key == key && Modifiers == modifiers;

        /// <summary>Whether this is the typed character <paramref name="character"/> (no Ctrl or Alt).</summary>
        /// <param name="character">The character.</param>
        /// <returns>True on a match.</returns>
        public bool IsChar(char character) => IsText && Char == character;

        /// <summary>A readable name such as <c>Ctrl+K</c>, <c>Shift+Tab</c>, <c>a</c> or <c>paste (12 chars)</c>.</summary>
        /// <returns>The name.</returns>
        public override string ToString() {
            switch (Kind) {
                case InputEventKind.Paste:
                    return "paste (" + Text!.Length.ToString(CultureInfo.InvariantCulture) + " chars)";
                case InputEventKind.Resize:
                    return "resize " + Width.ToString(CultureInfo.InvariantCulture) + "x" + Height.ToString(CultureInfo.InvariantCulture);
                case InputEventKind.FocusGained:
                    return "focus gained";
                case InputEventKind.FocusLost:
                    return "focus lost";
            }
            if (IsText) {
                return Char == ' ' ? "Space" : Char.ToString();
            }
            return KeyChord.Describe(Key, Char, Modifiers);
        }

        internal static ConsoleKey KeyFor(char character) {
            if (character >= 'a' && character <= 'z') {
                return (ConsoleKey)(character - 'a' + (int)ConsoleKey.A);
            }
            if (character >= 'A' && character <= 'Z') {
                return (ConsoleKey)(character - 'A' + (int)ConsoleKey.A);
            }
            if (character >= '0' && character <= '9') {
                return (ConsoleKey)(character - '0' + (int)ConsoleKey.D0);
            }
            switch (character) {
                case ' ':
                    return ConsoleKey.Spacebar;
                case '\r':
                case '\n':
                    return ConsoleKey.Enter;
                case '\t':
                    return ConsoleKey.Tab;
                case '\b':
                case '\u007f':
                    return ConsoleKey.Backspace;
                case '\u001b':
                    return ConsoleKey.Escape;
            }
            return 0;
        }

        internal static char ControlCharacter(ConsoleKey key) {
            switch (key) {
                case ConsoleKey.Enter:
                    return '\r';
                case ConsoleKey.Tab:
                    return '\t';
                case ConsoleKey.Backspace:
                    return '\b';
                case ConsoleKey.Escape:
                    return '\u001b';
                case ConsoleKey.Spacebar:
                    return ' ';
            }
            return '\0';
        }

        internal static string Printable(string text) {
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text) {
                sb.Append(c < ' ' ? '?' : c);
            }
            return sb.ToString();
        }
    }
}
