using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    // Applying values to a form, shown or not: the view showing it, values kept for options still to load, and the values
    // applied while no view showed it (replayed by the next view after the remembered choices and a preset).
    public sealed partial class Form {
        // The view showing the form now (a form shown on its own, or a menu's form pane while the menu runs), if any.
        private FormView? _view;
        // Values for choice fields whose options are still to load, retaining raw versus persisted representation.
        private readonly Dictionary<string, object?> _pending = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        // Values applied while no view showed the form, in order, with whether each batch is in stored form.
        private readonly List<KeyValuePair<Dictionary<string, object?>, bool>> _detached = new List<KeyValuePair<Dictionary<string, object?>, bool>>();
        private readonly object _gate = new object();

        internal FormView? View {
            get {
                lock (_gate) {
                    return _view;
                }
            }
            set {
                lock (_gate) {
                    _view = value;
                }
            }
        }

        /// <summary>Forgets a view that has closed, unless another one has taken its place.</summary>
        internal void Detach(FormView view) {
            lock (_gate) {
                if (ReferenceEquals(_view, view)) {
                    _view = null;
                }
            }
        }

        /// <summary>
        /// Applies values to the form, whether or not it is shown: each named field takes its value, the shown text of
        /// every field follows (text and number boxes included), fields that depend on a changed field are reset as if the
        /// user had changed it (unless a value is given for them), and choice fields whose options load from a changed
        /// field load again (a value given for such a field is chosen once its options arrive). Fields not named keep
        /// their values. Unknown names and values a field cannot take (an option it does not have) are ignored. Values
        /// applied before the form is shown win over remembered choices and a preset. Call it from a synchronous menu
        /// action or handler, before showing the form, or from any thread inside a menu app (an asynchronous action): a
        /// call from another thread is queued for the thread that shows the form and applied on its next tick.
        /// </summary>
        /// <param name="values">Values by field name (ignoring case), as the fields take them: a string, a number, a bool, or a list for a multi-choice field.</param>
        /// <exception cref="InvalidOperationException">The form is shown on another thread by a view that cannot take calls from other threads.</exception>
        public void Apply(IDictionary<string, object?> values) {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            FormView? view = View;
            if (view != null) {
                view.ApplyFromApp(values);
                return;
            }
            ApplyDetached(values, false);
        }

        /// <summary>Applies the values of another run of the form (every field, shown or hidden); see <see cref="Apply(IDictionary{string, object})"/>.</summary>
        /// <param name="values">The values, for example <see cref="FormResult.Values"/>.</param>
        public void Apply(FormValues values) {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            Apply(values.AllValues());
        }

        /// <summary>
        /// Applies values while no view shows the form: dependent fields are reset, options that must load again are
        /// cleared, values for them are kept, and the batch is remembered so the next view applies it again after the
        /// remembered choices and a preset.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="stored">Whether the values are in stored form (a preset or remembered choices) rather than as the fields take them.</param>
        internal void ApplyDetached(IDictionary<string, object?> values, bool stored) {
            Dictionary<string, object?> copy = Copy(values);
            lock (_gate) {
                Dictionary<Field, object?> before = SnapshotValues();
                foreach (KeyValuePair<string, object?> pair in Assign(copy, stored)) {
                    _pending[pair.Key] = pair.Value;
                }
                foreach (Field dependent in StaleDependents(before)) {
                    bool given = TryGet(copy, dependent.Key, out object? wanted);
                    if (dependent is ChoiceField choice && choice.Loader != null) {
                        choice.Choices.Clear();
                        choice.Value = choice.Multiple ? new List<object?>() : null;
                        if (given) {
                            _pending[dependent.Key] = RestorationValue(dependent, wanted, stored);
                        } else {
                            _pending.Remove(dependent.Key);
                        }
                    } else if (!given) {
                        dependent.Value = Reset(dependent);
                    }
                }
                _detached.Add(new KeyValuePair<Dictionary<string, object?>, bool>(copy, stored));
            }
        }

        /// <summary>Takes the values kept for choice fields whose options were still to load.</summary>
        internal Dictionary<string, object?> TakePending() {
            lock (_gate) {
                Dictionary<string, object?> taken = new Dictionary<string, object?>(_pending, StringComparer.OrdinalIgnoreCase);
                _pending.Clear();
                return taken;
            }
        }

        /// <summary>Takes the batches of values applied while no view showed the form, oldest first.</summary>
        internal List<KeyValuePair<Dictionary<string, object?>, bool>> TakeDetached() {
            lock (_gate) {
                List<KeyValuePair<Dictionary<string, object?>, bool>> taken = new List<KeyValuePair<Dictionary<string, object?>, bool>>(_detached);
                _detached.Clear();
                return taken;
            }
        }

        /// <summary>
        /// Assigns values field by field and returns, with their raw or stored representation, choice values that are not
        /// loaded yet (they apply after loading). Stored values skip fields that are not remembered; values as the fields
        /// take them reach every field, secrets included.
        /// </summary>
        internal Dictionary<string, object?> Assign(IDictionary<string, object?> values, bool stored) {
            Dictionary<string, object?> pending = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> pair in values) {
                Field? field = Find(pair.Key);
                if (field == null || (stored && !field.Remember)) {
                    continue;
                }
                if (!stored && field is SecretField) {
                    field.Value = pair.Value as string;
                    continue;
                }
                object? value = RestorationValue(field, pair.Value, stored);
                if (field is ChoiceField choice && choice.Loader != null && choice.Choices.Count == 0) {
                    pending[field.Key] = value;
                    continue;
                }
                if (TryRestore(field, value, out object? restored)) {
                    field.Value = restored;
                }
            }
            return pending;
        }

        /// <summary>Keeps raw choices separate from persisted text, including while their options are loading.</summary>
        internal static object? RestorationValue(Field field, object? value, bool stored) =>
            !stored && field is ChoiceField choice ? new RawChoiceValue(value, choice) : stored || value == null ? value : field.ToStored(value);

        internal static bool TryRestore(Field field, object? wanted, out object? value) {
            if (wanted is RawChoiceValue raw && field is ChoiceField choice) {
                return raw.TryRestore(choice, out value);
            }
            return field.TryRestore(wanted, out value);
        }

        private sealed class RawChoiceValue {
            private readonly object? _value;

            public RawChoiceValue(object? value, ChoiceField field) {
                _value = field.CloneValue(value);
            }

            public bool TryRestore(ChoiceField field, out object? value) {
                value = null;
                if (!field.Multiple) {
                    Choice? match = field.Find(_value);
                    if (match == null || !match.IsEnabled) {
                        return false;
                    }
                    value = match.Value;
                    return true;
                }
                List<object?> selected = new List<object?>();
                if (_value is IEnumerable list && !(_value is string)) {
                    foreach (object? item in list) {
                        Choice? match = field.Find(item);
                        if (match != null && match.IsEnabled && !selected.Contains(match.Value)) {
                            selected.Add(match.Value);
                        }
                    }
                }
                value = selected;
                return true;
            }
        }

        /// <summary>The empty value a field takes when a field it depends on changes (an empty list for a multi-choice field).</summary>
        internal static object? Reset(Field field) => field is ChoiceField choice && choice.Multiple ? new List<object?>() : null;

        internal static bool TryGet(IDictionary<string, object?> values, string name, out object? value) {
            foreach (KeyValuePair<string, object?> pair in values) {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) {
                    value = pair.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        /// <summary>A copy of values by name, ignoring case (a later name wins over an earlier one that differs only in case).</summary>
        internal static Dictionary<string, object?> Copy(IDictionary<string, object?> values) {
            Dictionary<string, object?> copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> pair in values) {
                copy[pair.Key] = pair.Value;
            }
            return copy;
        }

        /// <summary>Copies live values for change detection without persistence's lossy choice and secret conversion.</summary>
        internal Dictionary<Field, object?> SnapshotValues() {
            Dictionary<Field, object?> signatures = new Dictionary<Field, object?>();
            foreach (Field field in Fields) {
                signatures[field] = field.CloneValue(field.Value);
            }
            return signatures;
        }

        /// <summary>
        /// The fields that depend (directly or through others) on a field whose value changed since
        /// <paramref name="before"/>: the ones a user's change would reset, choice fields with a loader among them.
        /// </summary>
        internal List<Field> StaleDependents(Dictionary<Field, object?> before) {
            Queue<Field> changed = new Queue<Field>();
            foreach (Field field in Fields) {
                if (before.TryGetValue(field, out object? value) && !SameValue(field, value, field.Value)) {
                    changed.Enqueue(field);
                }
            }
            List<Field> stale = new List<Field>();
            while (changed.Count > 0) {
                Field source = changed.Dequeue();
                foreach (Field field in Fields) {
                    if (!ReferenceEquals(field, source) && !stale.Contains(field) && DependsOnField(field, source.Key)) {
                        stale.Add(field);
                        changed.Enqueue(field);
                    }
                }
            }
            return stale;
        }

        internal static bool SameValue(Field field, object? left, object? right) {
            if (field is NumberField) {
                return Equals(Field.Normalize(left), Field.Normalize(right));
            }
            if (field is ChoiceField choice && !choice.Multiple) {
                return Equals(left, right);
            }
            if (left is IList a && right is IList b) {
                if (a.Count != b.Count) { return false; }
                for (int i = 0; i < a.Count; i++) {
                    if (!Equals(a[i], b[i])) { return false; }
                }
                return true;
            }
            return Equals(left, right);
        }

        private static bool DependsOnField(Field field, string name) {
            foreach (string dependency in field.DependsOn) {
                if (string.Equals(dependency, name, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }
    }
}
