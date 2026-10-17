using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Data;

namespace ConsoleMonster.Changes {
    /// <summary>How an object or a value changed.</summary>
    public enum ChangeKind {
        /// <summary>No change.</summary>
        Unchanged,

        /// <summary>Only in the new set.</summary>
        Added,

        /// <summary>Only in the old set.</summary>
        Removed,

        /// <summary>In both, with different values.</summary>
        Changed
    }

    /// <summary>One changed property of an object (a nested path such as <c>Address.City</c> for nested objects).</summary>
    public sealed class PropertyChange {
        /// <summary>Creates a property change.</summary>
        /// <param name="path">The property path.</param>
        /// <param name="before">The old value.</param>
        /// <param name="after">The new value.</param>
        public PropertyChange(string path, object? before, object? after) {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Before = before;
            After = after;
        }

        /// <summary>The property path.</summary>
        public string Path { get; }

        /// <summary>The old value.</summary>
        public object? Before { get; }

        /// <summary>The new value.</summary>
        public object? After { get; }

        /// <inheritdoc />
        public override string ToString() => Path + ": " + ObjectDiff.Text(Before) + " -> " + ObjectDiff.Text(After);
    }

    /// <summary>One object of a comparison: added, removed, changed (with its property changes) or unchanged.</summary>
    public sealed class ObjectChange {
        /// <summary>Creates an object change.</summary>
        /// <param name="kind">The kind.</param>
        /// <param name="key">The key the objects were matched by.</param>
        /// <param name="before">The old object, or null when added.</param>
        /// <param name="after">The new object, or null when removed.</param>
        /// <param name="properties">The changed properties.</param>
        public ObjectChange(ChangeKind kind, string key, object? before, object? after, IReadOnlyList<PropertyChange>? properties = null) {
            Kind = kind;
            Key = key ?? string.Empty;
            Before = before;
            After = after;
            Properties = properties ?? new PropertyChange[0];
        }

        /// <summary>The kind.</summary>
        public ChangeKind Kind { get; }

        /// <summary>The key the objects were matched by.</summary>
        public string Key { get; }

        /// <summary>The old object, or null when added.</summary>
        public object? Before { get; }

        /// <summary>The new object, or null when removed.</summary>
        public object? After { get; }

        /// <summary>The changed properties (empty unless <see cref="Kind"/> is Changed).</summary>
        public IReadOnlyList<PropertyChange> Properties { get; }

        /// <summary>
        /// A short description of an added or removed object (its first compared properties, such as
        /// <c>Title: Admin; Enabled: True</c>), or the value itself for simple values; null when not described.
        /// </summary>
        public string? Summary { get; internal set; }

        /// <summary>
        /// The compared simple properties of an added or removed object (not its key, not empty), each with the value on its
        /// side: <see cref="PropertyChange.After"/> for added objects, <see cref="PropertyChange.Before"/> for removed ones.
        /// Empty for other kinds and for simple values.
        /// </summary>
        public IReadOnlyList<PropertyChange> Values { get; internal set; } = new PropertyChange[0];
    }

    /// <summary>The result of a comparison, with counts by kind.</summary>
    public sealed class ChangeSet {
        /// <summary>Creates a change set.</summary>
        /// <param name="changes">The object changes.</param>
        public ChangeSet(IReadOnlyList<ObjectChange> changes) {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            foreach (ObjectChange change in changes) {
                switch (change.Kind) {
                    case ChangeKind.Added:
                        Added++;
                        break;
                    case ChangeKind.Removed:
                        Removed++;
                        break;
                    case ChangeKind.Changed:
                        Changed++;
                        break;
                    default:
                        Unchanged++;
                        break;
                }
            }
        }

        /// <summary>Every object, in a stable order (changes first by kind, then unchanged ones).</summary>
        public IReadOnlyList<ObjectChange> Changes { get; }

        /// <summary>Objects only in the new set.</summary>
        public int Added { get; }

        /// <summary>Objects only in the old set.</summary>
        public int Removed { get; }

        /// <summary>Objects in both with different values.</summary>
        public int Changed { get; }

        /// <summary>Creates a change set that also counts unchanged objects that are not listed.</summary>
        /// <param name="changes">The object changes.</param>
        /// <param name="unchanged">The number of unchanged objects, listed or not.</param>
        public ChangeSet(IReadOnlyList<ObjectChange> changes, int unchanged) : this(changes) {
            Unchanged = Math.Max(Unchanged, unchanged);
        }

        /// <summary>Objects in both with the same values (counted even when not listed).</summary>
        public int Unchanged { get; private set; }

        /// <summary>Whether anything was added, removed or changed.</summary>
        public bool HasChanges => Added + Removed + Changed > 0;

        /// <summary>Whether the change removes objects: the case for a typed confirmation.</summary>
        public bool IsDestructive => Removed > 0;
    }

    /// <summary>Options for <see cref="ObjectDiff"/>.</summary>
    public sealed class DiffOptions {
        /// <summary>The members whose values identify an object (like Compare-Object -Property for matching). Empty: Id, Name, Key...</summary>
        public IList<string> Key { get; } = new List<string>();

        /// <summary>A function that returns an object's key, instead of <see cref="Key"/>.</summary>
        public Func<object, object?>? KeySelector { get; set; }

        /// <summary>The members to compare, or empty for all of them.</summary>
        public IList<string> Properties { get; } = new List<string>();

        /// <summary>Members (or paths such as <c>Address.City</c>, with <c>*</c> wildcards) left out of the comparison.</summary>
        public IList<string> Ignore { get; } = new List<string>();

        /// <summary>How deep nested objects are compared (default 3).</summary>
        public int Depth { get; set; } = 3;

        /// <summary>Whether unchanged objects are listed too (they are always counted).</summary>
        public bool IncludeUnchanged { get; set; }

        /// <summary>Member sources tried before reflection (PowerShell objects, dictionaries, JSON).</summary>
        public IList<IMemberSource> Sources { get; } = new List<IMemberSource>();
    }

    /// <summary>
    /// Compares objects and collections: objects are matched by key, then compared property by property (nested objects
    /// by path), giving added, removed and changed objects with counts. Show the result with <see cref="ChangeView"/> or
    /// ask before acting with <see cref="ChangePreview"/>.
    /// </summary>
    public static class ObjectDiff {
        private static readonly string[] DefaultKeys = { "Id", "Key", "Name", "FullName", "DistinguishedName", "SamAccountName", "DisplayName", "Path" };
        private static readonly IMemberSource Reflection = new ReflectionMemberSource();
        private static readonly IMemberSource Dictionaries = new DictionaryMemberSource();

        /// <summary>Compares two values: two collections item by item (matched by key), anything else as one object.</summary>
        /// <param name="before">The old value.</param>
        /// <param name="after">The new value.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The change set.</returns>
        public static ChangeSet Compare(object? before, object? after, DiffOptions? options = null) {
            if (IsCollection(before) || IsCollection(after)) {
                return Compare(Items(before), Items(after), options);
            }
            options ??= new DiffOptions();
            if (before == null && after == null) {
                return new ChangeSet(new ObjectChange[0]);
            }
            string key = before != null ? KeyOf(before, options, 0) : KeyOf(after!, options, 0);
            if (before == null) {
                return new ChangeSet(new[] { Described(new ObjectChange(ChangeKind.Added, key, null, after), after!, options) });
            }
            if (after == null) {
                return new ChangeSet(new[] { Described(new ObjectChange(ChangeKind.Removed, key, before, null), before, options) });
            }
            IReadOnlyList<PropertyChange> properties = CompareObjects(before, after, options);
            ObjectChange change = new ObjectChange(properties.Count > 0 ? ChangeKind.Changed : ChangeKind.Unchanged, key, before, after, properties);
            return new ChangeSet(change.Kind == ChangeKind.Unchanged && !options.IncludeUnchanged ? new ObjectChange[0] : new[] { change }).WithUnchanged(change.Kind == ChangeKind.Unchanged ? 1 : 0);
        }

        /// <summary>Compares two collections: items are matched by key and compared property by property.</summary>
        /// <param name="before">The old items.</param>
        /// <param name="after">The new items.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The change set.</returns>
        public static ChangeSet Compare(IEnumerable<object?> before, IEnumerable<object?> after, DiffOptions? options = null) {
            options ??= new DiffOptions();
            List<object> oldItems = (before ?? new object?[0]).Where(item => item != null).Select(item => item!).ToList();
            List<object> newItems = (after ?? new object?[0]).Where(item => item != null).Select(item => item!).ToList();
            Dictionary<string, Queue<object>> oldByKey = new Dictionary<string, Queue<object>>(StringComparer.Ordinal);
            for (int i = 0; i < oldItems.Count; i++) {
                string key = KeyOf(oldItems[i], options, i);
                if (!oldByKey.TryGetValue(key, out Queue<object>? queue)) {
                    oldByKey[key] = queue = new Queue<object>();
                }
                queue.Enqueue(oldItems[i]);
            }
            List<ObjectChange> added = new List<ObjectChange>();
            List<ObjectChange> changed = new List<ObjectChange>();
            List<ObjectChange> unchanged = new List<ObjectChange>();
            int same = 0;
            for (int i = 0; i < newItems.Count; i++) {
                string key = KeyOf(newItems[i], options, i);
                if (oldByKey.TryGetValue(key, out Queue<object>? queue) && queue.Count > 0) {
                    object old = queue.Dequeue();
                    IReadOnlyList<PropertyChange> properties = CompareObjects(old, newItems[i], options);
                    if (properties.Count > 0) {
                        changed.Add(new ObjectChange(ChangeKind.Changed, key, old, newItems[i], properties));
                    } else {
                        same++;
                        if (options.IncludeUnchanged) {
                            unchanged.Add(new ObjectChange(ChangeKind.Unchanged, key, old, newItems[i]));
                        }
                    }
                } else {
                    added.Add(Described(new ObjectChange(ChangeKind.Added, key, null, newItems[i]), newItems[i], options));
                }
            }
            List<ObjectChange> removed = new List<ObjectChange>();
            foreach (KeyValuePair<string, Queue<object>> left in oldByKey) {
                foreach (object old in left.Value) {
                    removed.Add(Described(new ObjectChange(ChangeKind.Removed, left.Key, old, null), old, options));
                }
            }
            List<ObjectChange> all = new List<ObjectChange>();
            all.AddRange(removed);
            all.AddRange(changed);
            all.AddRange(added);
            all.AddRange(unchanged);
            return new ChangeSet(all).WithUnchanged(same - unchanged.Count);
        }

        /// <summary>The changed properties of two objects (nested objects by path, up to <see cref="DiffOptions.Depth"/>).</summary>
        /// <param name="before">The old object.</param>
        /// <param name="after">The new object.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The changes.</returns>
        public static IReadOnlyList<PropertyChange> CompareObjects(object? before, object? after, DiffOptions? options = null) {
            options ??= new DiffOptions();
            List<PropertyChange> changes = new List<PropertyChange>();
            CompareInto(changes, string.Empty, before, after, options, 0);
            return changes;
        }

        /// <summary>A value as display text: null as empty, collections as <c>{a, b}</c>.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The text.</returns>
        public static string Text(object? value) {
            if (value == null) {
                return string.Empty;
            }
            if (value is string text) {
                return text;
            }
            if (value is IEnumerable sequence && !(value is IDictionary)) {
                List<string> parts = new List<string>();
                foreach (object? item in sequence) {
                    parts.Add(Text(item));
                }
                return "{" + string.Join(", ", parts) + "}";
            }
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        private static void CompareInto(List<PropertyChange> changes, string path, object? before, object? after, DiffOptions options, int depth) {
            if (IsLeaf(before) && IsLeaf(after) || depth >= options.Depth) {
                if (!ValuesEqual(before, after)) {
                    changes.Add(new PropertyChange(path.Length == 0 ? "(value)" : path, before, after));
                }
                return;
            }
            if (IsCollection(before) || IsCollection(after)) {
                if (!string.Equals(Text(before), Text(after), StringComparison.Ordinal)) {
                    changes.Add(new PropertyChange(path.Length == 0 ? "(value)" : path, before, after));
                }
                return;
            }
            Dictionary<string, object?> oldMembers = Members(before, options);
            Dictionary<string, object?> newMembers = Members(after, options);
            List<string> names = new List<string>(oldMembers.Keys);
            foreach (string name in newMembers.Keys) {
                if (!oldMembers.ContainsKey(name)) {
                    names.Add(name);
                }
            }
            foreach (string name in names) {
                string child = path.Length == 0 ? name : path + "." + name;
                if (depth == 0 && options.Properties.Count > 0 && !options.Properties.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase))) {
                    continue;
                }
                if (Ignored(child, name, options)) {
                    continue;
                }
                oldMembers.TryGetValue(name, out object? oldValue);
                newMembers.TryGetValue(name, out object? newValue);
                CompareInto(changes, child, oldValue, newValue, options, depth + 1);
            }
        }

        // An added or removed object's compared simple properties (not its key, not empty) with the value on its side, and
        // the short summary of the first few.
        private static ObjectChange Described(ObjectChange change, object value, DiffOptions options) {
            if (IsLeaf(value) || IsCollection(value)) {
                change.Summary = Text(value);
                return change;
            }
            bool added = change.Kind == ChangeKind.Added;
            List<PropertyChange> values = new List<PropertyChange>();
            foreach (KeyValuePair<string, object?> member in Members(value, options)) {
                if (options.Key.Any(k => string.Equals(k, member.Key, StringComparison.OrdinalIgnoreCase)) || Ignored(member.Key, member.Key, options)
                    || (options.Properties.Count > 0 && !options.Properties.Any(p => string.Equals(p, member.Key, StringComparison.OrdinalIgnoreCase)))
                    || !IsLeaf(member.Value) || member.Value == null || Text(member.Value).Length == 0) {
                    continue;
                }
                values.Add(new PropertyChange(member.Key, added ? null : member.Value, added ? member.Value : null));
            }
            change.Values = values;
            List<string> parts = values.Take(5).Select(v => v.Path + ": " + Text(added ? v.After : v.Before)).ToList();
            change.Summary = string.Join("; ", parts) + (values.Count > 5 ? "; …" : string.Empty);
            return change;
        }

        private static bool Ignored(string path, string name, DiffOptions options) {
            foreach (string pattern in options.Ignore) {
                if (Like(path, pattern) || Like(name, pattern)) {
                    return true;
                }
            }
            return false;
        }

        // A simple * wildcard match, case-insensitive.
        private static bool Like(string text, string pattern) {
            if (pattern.IndexOf('*') < 0) {
                return string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase);
            }
            string[] parts = pattern.Split('*');
            int index = 0;
            for (int i = 0; i < parts.Length; i++) {
                if (parts[i].Length == 0) {
                    continue;
                }
                int found = text.IndexOf(parts[i], index, StringComparison.OrdinalIgnoreCase);
                if (found < 0 || (i == 0 && found != 0)) {
                    return false;
                }
                index = found + parts[i].Length;
            }
            return parts[parts.Length - 1].Length == 0 || index == text.Length;
        }

        private static Dictionary<string, object?> Members(object? value, DiffOptions options) {
            Dictionary<string, object?> members = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (value == null) {
                return members;
            }
            IMemberSource source = options.Sources.FirstOrDefault(s => s.CanRead(value)) ?? (Dictionaries.CanRead(value) ? Dictionaries : Reflection);
            foreach (InspectMember member in source.GetMembers(value)) {
                try {
                    members[member.Name] = member.Value;
                } catch (Exception ex) when (!(ex is OutOfMemoryException)) {
                    members[member.Name] = null;
                }
            }
            return members;
        }

        private static string KeyOf(object item, DiffOptions options, int index) {
            if (options.KeySelector != null) {
                return Text(options.KeySelector(item));
            }
            if (IsLeaf(item)) {
                return Text(item);
            }
            Dictionary<string, object?> members = Members(item, options);
            if (options.Key.Count > 0) {
                return string.Join("|", options.Key.Select(name => members.TryGetValue(name, out object? value) ? Text(value) : string.Empty));
            }
            foreach (string name in DefaultKeys) {
                if (members.TryGetValue(name, out object? value) && value != null && IsLeaf(value)) {
                    return Text(value);
                }
            }
            return "#" + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static bool ValuesEqual(object? a, object? b) {
            if (a == null || b == null) {
                return a == null && b == null || (a == null ? Text(b).Length == 0 && b is string : Text(a).Length == 0 && a is string);
            }
            if (a.Equals(b)) {
                return true;
            }
            if (IsNumber(a) && IsNumber(b)) {
                return Convert.ToDouble(a, CultureInfo.InvariantCulture).Equals(Convert.ToDouble(b, CultureInfo.InvariantCulture));
            }
            return string.Equals(Text(a), Text(b), StringComparison.Ordinal) && a.GetType() == b.GetType();
        }

        private static bool IsLeaf(object? value) => value == null || ObjectInspector.IsScalarValue(value);

        private static bool IsCollection(object? value) => value is IEnumerable && !(value is string) && !(value is IDictionary);

        private static IEnumerable<object?> Items(object? value) {
            if (value == null) {
                yield break;
            }
            if (IsCollection(value)) {
                foreach (object? item in (IEnumerable)value) {
                    yield return item;
                }
            } else {
                yield return value;
            }
        }

        private static bool IsNumber(object value) => value is int || value is long || value is double || value is float || value is decimal
            || value is short || value is byte || value is uint || value is ulong || value is ushort || value is sbyte;

        // Unchanged objects not listed still count.
        private static ChangeSet WithUnchanged(this ChangeSet set, int extra) => extra <= 0 ? set : new ChangeSet(set.Changes, set.Unchanged + extra);
    }
}
