using System;
using System.Collections.Generic;

namespace ConsoleMonster.Input {
    /// <summary>
    /// A key combination used by key bindings: either a key with exact modifiers (<c>Ctrl+K</c>, <c>Shift+Tab</c>,
    /// <c>F1</c>, <c>PageDown</c>) or a typed character (<c>?</c>, <c>:</c>, <c>/</c>) that matches however it was typed.
    /// </summary>
    public readonly struct KeyChord : IEquatable<KeyChord> {
        private static readonly Dictionary<string, ConsoleKey> Names = BuildNames();

        /// <summary>Creates a key chord.</summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The modifiers.</param>
        public KeyChord(ConsoleKey key, KeyModifiers modifiers = KeyModifiers.None) {
            Key = key;
            Modifiers = modifiers;
            Char = '\0';
        }

        /// <summary>Creates a character chord.</summary>
        /// <param name="character">The character.</param>
        public KeyChord(char character) {
            Key = 0;
            Modifiers = KeyModifiers.None;
            Char = character;
        }

        /// <summary>The key, or <c>0</c> for a character chord.</summary>
        public ConsoleKey Key { get; }

        /// <summary>The character of a character chord, or <c>'\0'</c>.</summary>
        public char Char { get; }

        /// <summary>The modifiers of a key chord.</summary>
        public KeyModifiers Modifiers { get; }

        /// <summary>Whether this is a character chord.</summary>
        public bool IsCharacter => Char != '\0';

        /// <summary>Whether an event is this chord.</summary>
        /// <param name="input">The event.</param>
        /// <returns>True on a match.</returns>
        public bool Matches(InputEvent input) {
            if (input == null || input.Kind != InputEventKind.Key) {
                return false;
            }
            return IsCharacter ? input.IsChar(Char) : input.Key == Key && input.Modifiers == Modifiers;
        }

        /// <summary>
        /// Parses <c>Ctrl+K</c>, <c>Alt+Enter</c>, <c>Shift+Tab</c>, <c>F5</c>, <c>PgDn</c>, <c>Space</c> or a single
        /// character such as <c>?</c>. Modifier and key names are case-insensitive.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The chord.</returns>
        public static KeyChord Parse(string text) {
            if (!TryParse(text, out KeyChord chord)) {
                throw new FormatException("'" + text + "' is not a key such as Ctrl+K, Shift+Tab, F1 or ?.");
            }
            return chord;
        }

        /// <summary>Parses a chord; see <see cref="Parse"/>.</summary>
        /// <param name="text">The text.</param>
        /// <param name="chord">The chord.</param>
        /// <returns>Whether the text is a chord.</returns>
        public static bool TryParse(string? text, out KeyChord chord) {
            chord = default;
            if (string.IsNullOrEmpty(text)) {
                return false;
            }
            if (text!.Length == 1) {
                char c = text[0];
                if (c == ' ') {
                    chord = new KeyChord(ConsoleKey.Spacebar);
                    return true;
                }
                if (c < ' ') {
                    return false;
                }
                chord = new KeyChord(c);
                return true;
            }
            KeyModifiers modifiers = KeyModifiers.None;
            string rest = text;
            while (true) {
                int plus = rest.IndexOf('+', 1);
                if (plus < 0) {
                    break;
                }
                string part = rest.Substring(0, plus).Trim();
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) {
                    modifiers |= KeyModifiers.Control;
                } else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase) || part.Equals("Option", StringComparison.OrdinalIgnoreCase)) {
                    modifiers |= KeyModifiers.Alt;
                } else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) {
                    modifiers |= KeyModifiers.Shift;
                } else {
                    return false;
                }
                rest = rest.Substring(plus + 1);
            }
            rest = rest.Trim();
            if (rest.Length == 1) {
                char c = rest[0];
                // Ctrl+\ arrives as the US-layout backslash key on every input path.
                ConsoleKey key = c == '\\' ? ConsoleKey.Oem5 : InputEvent.KeyFor(c);
                if (modifiers == KeyModifiers.None) {
                    chord = c == ' ' ? new KeyChord(ConsoleKey.Spacebar) : new KeyChord(c);
                    return c >= ' ';
                }
                if (key == 0) {
                    return false;
                }
                chord = new KeyChord(key, modifiers);
                return true;
            }
            if (Names.TryGetValue(rest, out ConsoleKey named)) {
                chord = new KeyChord(named, modifiers);
                return true;
            }
            return false;
        }

        /// <summary>The chord as people write it: <c>Ctrl+K</c>, <c>Shift+Tab</c>, <c>?</c>.</summary>
        /// <returns>The name.</returns>
        public override string ToString() => IsCharacter ? Char.ToString() : Describe(Key, '\0', Modifiers);

        /// <inheritdoc />
        public bool Equals(KeyChord other) => Key == other.Key && Char == other.Char && Modifiers == other.Modifiers;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is KeyChord other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Key * 397) ^ (Char * 31) ^ (int)Modifiers;

        /// <summary>Equality.</summary>
        /// <param name="left">The left chord.</param>
        /// <param name="right">The right chord.</param>
        /// <returns>True when equal.</returns>
        public static bool operator ==(KeyChord left, KeyChord right) => left.Equals(right);

        /// <summary>Inequality.</summary>
        /// <param name="left">The left chord.</param>
        /// <param name="right">The right chord.</param>
        /// <returns>True when different.</returns>
        public static bool operator !=(KeyChord left, KeyChord right) => !left.Equals(right);

        internal static string Describe(ConsoleKey key, char character, KeyModifiers modifiers) {
            string name = KeyName(key, character);
            string prefix = string.Empty;
            if ((modifiers & KeyModifiers.Control) != 0) {
                prefix += "Ctrl+";
            }
            if ((modifiers & KeyModifiers.Alt) != 0) {
                prefix += "Alt+";
            }
            if ((modifiers & KeyModifiers.Shift) != 0) {
                prefix += "Shift+";
            }
            return prefix + name;
        }

        private static string KeyName(ConsoleKey key, char character) {
            if (key >= ConsoleKey.A && key <= ConsoleKey.Z) {
                return ((char)('A' + (key - ConsoleKey.A))).ToString();
            }
            if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) {
                return ((char)('0' + (key - ConsoleKey.D0))).ToString();
            }
            switch (key) {
                case ConsoleKey.Spacebar:
                    return "Space";
                case ConsoleKey.Escape:
                    return "Esc";
                case ConsoleKey.PageUp:
                    return "PgUp";
                case ConsoleKey.PageDown:
                    return "PgDn";
                case ConsoleKey.UpArrow:
                    return "Up";
                case ConsoleKey.DownArrow:
                    return "Down";
                case ConsoleKey.LeftArrow:
                    return "Left";
                case ConsoleKey.RightArrow:
                    return "Right";
                case ConsoleKey.Delete:
                    return "Del";
                case ConsoleKey.Insert:
                    return "Ins";
                case ConsoleKey.Oem5:
                    return "\\";
                case 0:
                    return character == '\0' ? "?" : InputEvent.Printable(character.ToString());
            }
            return key.ToString();
        }

        private static Dictionary<string, ConsoleKey> BuildNames() {
            Dictionary<string, ConsoleKey> names = new Dictionary<string, ConsoleKey>(StringComparer.OrdinalIgnoreCase) {
                ["Enter"] = ConsoleKey.Enter,
                ["Return"] = ConsoleKey.Enter,
                ["Tab"] = ConsoleKey.Tab,
                ["Esc"] = ConsoleKey.Escape,
                ["Escape"] = ConsoleKey.Escape,
                ["Space"] = ConsoleKey.Spacebar,
                ["Spacebar"] = ConsoleKey.Spacebar,
                ["Backspace"] = ConsoleKey.Backspace,
                ["Bksp"] = ConsoleKey.Backspace,
                ["Delete"] = ConsoleKey.Delete,
                ["Del"] = ConsoleKey.Delete,
                ["Insert"] = ConsoleKey.Insert,
                ["Ins"] = ConsoleKey.Insert,
                ["Home"] = ConsoleKey.Home,
                ["End"] = ConsoleKey.End,
                ["PageUp"] = ConsoleKey.PageUp,
                ["PgUp"] = ConsoleKey.PageUp,
                ["PageDown"] = ConsoleKey.PageDown,
                ["PgDn"] = ConsoleKey.PageDown,
                ["Up"] = ConsoleKey.UpArrow,
                ["UpArrow"] = ConsoleKey.UpArrow,
                ["Down"] = ConsoleKey.DownArrow,
                ["DownArrow"] = ConsoleKey.DownArrow,
                ["Left"] = ConsoleKey.LeftArrow,
                ["LeftArrow"] = ConsoleKey.LeftArrow,
                ["Right"] = ConsoleKey.RightArrow,
                ["RightArrow"] = ConsoleKey.RightArrow
            };
            for (int i = 1; i <= 24; i++) {
                names["F" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = (ConsoleKey)((int)ConsoleKey.F1 + i - 1);
            }
            return names;
        }
    }
}
