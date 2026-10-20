using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The values of a form by field name: typed access (<see cref="Get{T}"/>), multi-selections as lists, and only the
    /// fields that are visible in <see cref="Visible"/>.
    /// </summary>
    public sealed class FormValues : IEnumerable<KeyValuePair<string, object?>> {
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal void Set(string name, object? value, bool visible) {
            _values[name] = value;
            if (visible) {
                _visible.Add(name);
            } else {
                _visible.Remove(name);
            }
        }

        /// <summary>The value of a field, or null.</summary>
        /// <param name="name">The field name.</param>
        /// <returns>The value.</returns>
        public object? this[string name] => _values.TryGetValue(name, out object? value) ? value : null;

        /// <summary>Whether a field exists.</summary>
        /// <param name="name">The field name.</param>
        /// <returns>True when it exists.</returns>
        public bool Contains(string name) => _values.ContainsKey(name);

        /// <summary>Whether a field is currently shown (its condition holds).</summary>
        /// <param name="name">The field name.</param>
        /// <returns>True when visible.</returns>
        public bool IsVisible(string name) => _visible.Contains(name);

        /// <summary>The names of the visible fields.</summary>
        public IReadOnlyCollection<string> Visible => _visible;

        /// <summary>The value converted to <typeparamref name="T"/> (lists for multi-selections), or the default.</summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="name">The field name.</param>
        /// <param name="fallback">Returned when the field is empty.</param>
        /// <returns>The value.</returns>
        public T Get<T>(string name, T fallback = default!) {
            object? value = this[name];
            if (value == null) {
                return fallback;
            }
            if (value is T typed) {
                return typed;
            }
            if (typeof(T) == typeof(string)) {
                return (T)(object)(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty);
            }
            try {
                return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
            } catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException) {
                return fallback;
            }
        }

        /// <summary>A multi-selection (or a single value) as a typed list.</summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <param name="name">The field name.</param>
        /// <returns>The items that have the type.</returns>
        public IReadOnlyList<TItem> GetList<TItem>(string name) {
            List<TItem> result = new List<TItem>();
            object? value = this[name];
            if (value is IEnumerable list && !(value is string)) {
                foreach (object? item in list) {
                    if (item is TItem typed) {
                        result.Add(typed);
                    }
                }
            } else if (value is TItem single) {
                result.Add(single);
            }
            return result;
        }

        /// <summary>A plain dictionary of the visible values.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, object?> ToDictionary() {
            Dictionary<string, object?> copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> pair in _values) {
                if (_visible.Contains(pair.Key)) {
                    copy[pair.Key] = pair.Value;
                }
            }
            return copy;
        }

        /// <summary>A copy of every value, hidden fields included.</summary>
        internal Dictionary<string, object?> AllValues() => new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ToDictionary().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
