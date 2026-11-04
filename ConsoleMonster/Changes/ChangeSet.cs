using System;
using System.Collections.Generic;
using System.Linq;

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
        public PropertyChange(string path, object? before, object? after) : this(path, before, after, true, true) { }

        /// <summary>Creates a property comparison with explicit presence and case-only information.</summary>
        /// <param name="path">The property path.</param>
        /// <param name="before">The original old value.</param>
        /// <param name="after">The original new value.</param>
        /// <param name="hasBefore">Whether the old property exists, even when its value is null.</param>
        /// <param name="hasAfter">Whether the new property exists.</param>
        /// <param name="isCaseOnly">Whether case-insensitive comparison found only a case difference.</param>
        public PropertyChange(string path, object? before, object? after, bool hasBefore = true, bool hasAfter = true, bool isCaseOnly = false) {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Before = before;
            After = after;
            HasBefore = hasBefore;
            HasAfter = hasAfter;
            IsCaseOnly = isCaseOnly;
        }

        /// <summary>The property path.</summary>
        public string Path { get; }

        /// <summary>The old value.</summary>
        public object? Before { get; }

        /// <summary>The new value.</summary>
        public object? After { get; }

        /// <summary>Whether the old property exists; null is a present value.</summary>
        public bool HasBefore { get; }

        /// <summary>Whether the new property exists; null is a present value.</summary>
        public bool HasAfter { get; }

        /// <summary>A visible case-only difference ignored for changed counts.</summary>
        public bool IsCaseOnly { get; }

        /// <summary>The old value as display text; dates that differ only below a minute show their seconds.</summary>
        public string BeforeText => HasBefore ? ObjectDiff.PairText(Before, After) : Localization.WidgetLabels.Current.ChangesMissing;

        /// <summary>The new value as display text; dates that differ only below a minute show their seconds.</summary>
        public string AfterText => HasAfter ? ObjectDiff.PairText(After, Before) : Localization.WidgetLabels.Current.ChangesMissing;

        /// <inheritdoc />
        public override string ToString() => Path + ": " + BeforeText + " -> " + AfterText;
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

        /// <summary>The changed properties, including case-only differences on unchanged objects.</summary>
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

        /// <summary>The number of listed property differences ignored because only letter case differs.</summary>
        public int CaseOnly => Changes.Sum(c => c.Properties.Count(p => p.IsCaseOnly));

        /// <summary>Whether anything was added, removed or changed.</summary>
        public bool HasChanges => Added + Removed + Changed > 0;

        /// <summary>Whether the change removes objects: the case for a typed confirmation.</summary>
        public bool IsDestructive => Removed > 0;

        /// <summary>The member paths that were not compared because reading them stalled (neither changed nor the same).</summary>
        public IReadOnlyList<string> NotCompared { get; internal set; } = new string[0];
    }

}
