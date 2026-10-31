using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The app's bindings by scope: the focused input's, the current screen's, then the app's. One list drives both the
    /// keys and the footer, so the footer cannot promise a key that something nearer takes first.
    /// </summary>
    internal sealed class AppKeyMap {
        private readonly IList<KeyBinding> _app;
        private readonly AppScreens? _screens;
        private readonly FocusManager _focus;
        private readonly RenderContext _context;

        internal AppKeyMap(IList<KeyBinding> app, AppScreens? screens, FocusManager focus, RenderContext context) {
            _app = app; _screens = screens; _focus = focus; _context = context;
        }

        // The focused input's own bindings run before the input, so Enter on a list can open the selected row.
        internal bool DispatchFocused(InputEvent input) {
            InputControl? focused = Focused();
            if (focused == null) { return false; }
            foreach (KeyBinding binding in focused.Bindings) {
                if (binding.TryInvoke(input)) { return true; }
            }
            return false;
        }

        // Keys the focused input left unused: the screen's, then the app's.
        internal bool Dispatch(InputEvent input) {
            foreach (KeyBinding binding in Later()) {
                if (binding.TryInvoke(input)) { ForgetPending(binding); return true; }
            }
            return false;
        }

        internal void ForgetPending(KeyBinding? except = null) {
            InputControl? focused = Focused();
            foreach (KeyBinding binding in (focused?.Bindings ?? Enumerable.Empty<KeyBinding>()).Concat(Later())) {
                if (!ReferenceEquals(binding, except)) { binding.ForgetPending(); }
            }
        }

        /// <summary>
        /// Footer hints, nearest scope first, then <paramref name="host"/>. A binding is listed under the keys still free
        /// when it is reached: keys the host matches before any binding (<paramref name="hostFirst"/>), keys a nearer
        /// binding took and keys the focused input takes itself are not free. Hints are ordered by
        /// <see cref="KeyBinding.Order"/>, with the key-map binding kept last.
        /// </summary>
        internal IReadOnlyList<KeyHint> Hints(IEnumerable<KeyBinding> host, ICollection<KeyChord> hostFirst, bool scoped, out bool keepLast) {
            HashSet<KeyChord> claimed = new HashSet<KeyChord>();
            List<KeyValuePair<KeyBinding, string>> shown = new List<KeyValuePair<KeyBinding, string>>();
            KeyValuePair<KeyBinding, string>? help = null;
            InputControl? focused = scoped ? Focused() : null;
            void Consider(KeyBinding binding, bool hostOwned, bool beforeInput) {
                List<KeyChord> claims = binding.Claims.ToList();
                List<KeyChord> free = claims.Where(chord => !claimed.Contains(chord)
                    && (hostOwned || !hostFirst.Contains(chord))
                    && (beforeInput || focused == null || !focused.Takes(chord, _context))).ToList();
                foreach (KeyChord chord in free) { claimed.Add(chord); }
                if ((free.Count == 0 && binding.Chords.Count > 0) || !binding.InFooter || string.IsNullOrEmpty(binding.Label)) { return; }
                string keys = free.Count == claims.Count || binding.KeyText != null ? binding.DisplayKeys() : KeyBinding.Display(free[0]);
                if (binding.Order >= KeyFooter.HelpOrder) { help = new KeyValuePair<KeyBinding, string>(binding, keys); }
                else { shown.Add(new KeyValuePair<KeyBinding, string>(binding, keys)); }
            }
            if (focused != null) {
                foreach (KeyBinding binding in focused.Bindings) { Consider(binding, false, true); }
                List<KeyBinding> own = new List<KeyBinding>();
                focused.AddOwnKeys(own, _context);
                foreach (KeyBinding binding in own) { Consider(binding, false, true); }
            }
            if (scoped) { foreach (KeyBinding binding in Later()) { Consider(binding, false, false); } }
            foreach (KeyBinding binding in host) { Consider(binding, true, false); }
            List<KeyHint> hints = shown.OrderBy(pair => pair.Key.Order).Select(pair => Hint(pair.Key, pair.Value)).ToList();
            if (help.HasValue) { hints.Add(Hint(help.Value.Key, help.Value.Value)); }
            keepLast = help.HasValue;
            return hints;
        }

        // Focus keys and Ctrl+C reach the host before any binding, so a binding on them could never run.
        internal void Validate(IEnumerable<KeyChord> reserved) {
            HashSet<KeyChord> host = new HashSet<KeyChord>(reserved);
            IEnumerable<KeyBinding> inputs = _focus.Controls.SelectMany(control => control.Bindings);
            IEnumerable<KeyBinding> screens = _screens?.Screens.SelectMany(screen => screen.Bindings) ?? Enumerable.Empty<KeyBinding>();
            foreach (KeyBinding binding in inputs.Concat(screens).Concat(_app)) {
                foreach (KeyChord chord in binding.Chords) {
                    if (host.Contains(chord)) {
                        throw new ArgumentException("'" + binding.Label + "' is bound to " + chord + ", which the app uses for focus or exit, so it could never run. Choose another key or free it in ConsoleAppOptions.Keys.");
                    }
                }
            }
        }

        private static KeyHint Hint(KeyBinding binding, string keys) => new KeyHint(keys, binding.Label, binding.IsAvailable);

        private InputControl? Focused() {
            InputControl? current = _focus.Current;
            return current == null || current.Disabled ? null : current;
        }

        private IEnumerable<KeyBinding> Later() {
            if (_screens != null) {
                foreach (KeyBinding binding in _screens.Bindings()) { yield return binding; }
            }
            foreach (KeyBinding binding in _app) { yield return binding; }
        }
    }
}
