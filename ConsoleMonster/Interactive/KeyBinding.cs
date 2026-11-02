using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A key binding: the keys, what they do and the words that describe them. Footers and the key map are generated from
    /// the active bindings, so what the screen says is always what the keys do.
    /// </summary>
    public sealed class KeyBinding {
        private readonly KeyChord[] _chords;
        private readonly Func<InputEvent, bool>? _handler;
        private readonly Func<string>? _keyTextSource;
        private readonly NumberEntry? _numbers;
        private string? _keyText;

        /// <summary>Creates a binding.</summary>
        /// <param name="label">What the keys do ("select", "toggle").</param>
        /// <param name="action">The action.</param>
        /// <param name="chords">The keys.</param>
        public KeyBinding(string label, Action action, params KeyChord[] chords) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            _chords = chords ?? throw new ArgumentNullException(nameof(chords));
        }

        private KeyBinding(string label, Action action, KeyChord[] chords, NumberEntry numbers) : this(label, action, chords) {
            _numbers = numbers;
            _handler = numbers.Handle;
            _keyTextSource = numbers.Describe;
        }

        /// <summary>
        /// Opens numbered items: <c>1</c>-<c>9</c> open at once, and with ten or more items the digits build the number
        /// (<c>1</c> then <c>2</c> opens 12). A digit that could still start a larger number waits; Enter opens it, Escape
        /// or any other key forgets it. The footer shows <c>1-N</c>, or the digits typed so far.
        /// </summary>
        /// <param name="label">What opening does ("open").</param>
        /// <param name="count">The current number of items; read on every key.</param>
        /// <param name="open">Opens the one-based item number.</param>
        /// <returns>The binding.</returns>
        public static KeyBinding Numbered(string label, Func<int> count, Action<int> open) {
            if (count == null) { throw new ArgumentNullException(nameof(count)); }
            if (open == null) { throw new ArgumentNullException(nameof(open)); }
            NumberEntry entry = new NumberEntry(count, open);
            KeyChord[] digits = new KeyChord[10];
            for (int i = 0; i < 10; i++) { digits[i] = new KeyChord((char)('0' + i)); }
            return new KeyBinding(label, () => entry.Open(1), digits, entry);
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
        public string? KeyText {
            get => _keyTextSource?.Invoke() ?? _keyText;
            set => _keyText = value;
        }

        /// <summary>
        /// Whether the binding can run now; null means always. An unavailable binding stays in the footer dimmed and its
        /// keys do nothing, so they never fall through to another binding the footer does not show.
        /// </summary>
        public Func<bool>? Available { get; set; }

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

        // One dispatch path for every host: unavailable bindings swallow their keys, the numbered helper keeps its digits.
        internal bool TryInvoke(InputEvent input) {
            if (_handler != null) { return IsAvailable ? _handler(input) : _chords.Length > 0 && Matches(input); }
            if (_chords.Length == 0 || !Matches(input)) { return false; }
            if (IsAvailable) { Action(); }
            return true;
        }

        internal bool IsAvailable => Available?.Invoke() ?? true;

        // The keys this binding takes right now: the numbered helper takes only digits that can start an item number,
        // and Enter and Escape while a digit waits.
        internal IEnumerable<KeyChord> Claims => _numbers?.Claims() ?? _chords;

        internal bool Binds(KeyChord chord) => Array.IndexOf(_chords, chord) >= 0;

        // A key taken before this binding saw it ends a half-typed number.
        internal void ForgetPending() => _numbers?.Forget();

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

        private sealed class NumberEntry {
            private readonly Func<int> _count;
            private readonly Action<int> _open;
            private int _pending;

            internal NumberEntry(Func<int> count, Action<int> open) { _count = count; _open = open; }

            internal void Open(int number) {
                _pending = 0;
                if (number >= 1 && number <= _count()) { _open(number); }
            }

            internal void Forget() => _pending = 0;

            internal IEnumerable<KeyChord> Claims() {
                int last = Math.Min(9, _count());
                for (int d = 1; d <= last; d++) { yield return new KeyChord((char)('0' + d)); }
                if (_pending > 0) {
                    yield return new KeyChord('0');
                    yield return new KeyChord(ConsoleKey.Enter);
                    yield return new KeyChord(ConsoleKey.Escape);
                }
            }

            internal string Describe() {
                if (_pending > 0) { return _pending.ToString(CultureInfo.InvariantCulture) + "_"; }
                int count = _count();
                return count <= 1 ? "1" : "1-" + count.ToString(CultureInfo.InvariantCulture);
            }

            internal bool Handle(InputEvent input) {
                int count = _count();
                int digit = Digit(input);
                if (digit < 0) {
                    if (_pending > 0 && input.Is(ConsoleKey.Enter)) { Open(_pending); return true; }
                    bool cancel = _pending > 0 && input.Is(ConsoleKey.Escape);
                    _pending = 0;
                    return cancel;
                }
                long number = _pending * 10L + digit;
                if (number < 1 || number > count) {
                    // A digit that cannot extend the typed number starts a new one.
                    bool had = _pending > 0;
                    _pending = 0;
                    if (!had || digit < 1 || digit > count) { return had; }
                    number = digit;
                }
                if (number * 10 > count) { Open((int)number); }
                else { _pending = (int)number; }
                return true;
            }

            private static int Digit(InputEvent input) {
                for (int d = 0; d <= 9; d++) {
                    if (new KeyChord((char)('0' + d)).Matches(input)) { return d; }
                }
                return -1;
            }
        }
    }
}
