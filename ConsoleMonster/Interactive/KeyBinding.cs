using System;
using System.Collections.Generic;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A key binding: the keys, what they do and the words that describe them. Footers and the key map are generated from
    /// the active bindings, so what the screen says is always what the keys do.
    /// </summary>
    public sealed class KeyBinding {
        private readonly KeyChord[] _chords;

        /// <summary>Creates a binding.</summary>
        /// <param name="label">What the keys do ("select", "toggle").</param>
        /// <param name="action">The action.</param>
        /// <param name="chords">The keys.</param>
        public KeyBinding(string label, Action action, params KeyChord[] chords) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            _chords = chords ?? throw new ArgumentNullException(nameof(chords));
        }

        /// <summary>Creates a binding from key names (<c>"Ctrl+K"</c>, <c>":"</c>).</summary>
        /// <param name="keys">The keys, as <see cref="KeyChord.Parse"/> accepts them.</param>
        /// <param name="label">What the keys do.</param>
        /// <param name="action">The action.</param>
        /// <returns>The binding.</returns>
        public static KeyBinding Create(string keys, string label, Action action) {
            if (keys == null) {
                throw new ArgumentNullException(nameof(keys));
            }
            List<KeyChord> chords = new List<KeyChord>();
            foreach (string key in keys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                chords.Add(KeyChord.Parse(key.Trim().Length == 0 ? key : key.Trim()));
            }
            return new KeyBinding(label, action, chords.ToArray());
        }

        /// <summary>The keys.</summary>
        public IReadOnlyList<KeyChord> Chords => _chords;

        /// <summary>What the keys do.</summary>
        public string Label { get; }

        /// <summary>The action.</summary>
        public Action Action { get; }

        /// <summary>How the keys are shown (for example <c>↑↓</c>); null shows the first chord.</summary>
        public string? KeyText { get; set; }

        /// <summary>The heading the binding is listed under in the key map.</summary>
        public string Group { get; set; } = "General";

        /// <summary>Footer priority: lower values are shown first and dropped last when space runs out.</summary>
        public int Order { get; set; } = 50;

        /// <summary>Whether the footer shows the binding (the key map always lists it).</summary>
        public bool InFooter { get; set; } = true;

        /// <summary>Whether an event triggers this binding.</summary>
        /// <param name="input">The event.</param>
        /// <returns>True on a match.</returns>
        public bool Matches(InputEvent input) {
            foreach (KeyChord chord in _chords) {
                if (chord.Matches(input)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The key text as shown in footers: lower-case names (<c>enter</c>, <c>ctrl+a</c>), characters as typed.</summary>
        /// <returns>The text.</returns>
        public string DisplayKeys() {
            if (KeyText != null) {
                return KeyText;
            }
            if (_chords.Length == 0) {
                return string.Empty;
            }
            return Display(_chords[0]);
        }

        internal static string Display(KeyChord chord) {
            if (chord.IsCharacter) {
                return chord.Char.ToString();
            }
            return chord.ToString().ToLowerInvariant();
        }
    }
}
