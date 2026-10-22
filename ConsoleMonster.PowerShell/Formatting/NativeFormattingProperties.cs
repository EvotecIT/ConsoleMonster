using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.PowerShell {
    /// <summary>Leases native preferences by object identity across all formatting installations in this process.</summary>
    internal sealed class NativeFormattingProperties {
        private static readonly object Gate = new object();
        private static readonly ConditionalWeakTable<object, OwnerProperties> Owners = new ConditionalWeakTable<object, OwnerProperties>();
        private readonly List<Lease> _leases = new List<Lease>();

        /// <summary>A successful apply becomes the current owner, retaining the first baseline across overlapping leases.</summary>
        internal void Set(PSObject target, string name, object? value) {
            if (value == null) {
                return;
            }
            lock (Gate) {
                try {
                    PSPropertyInfo? property = target.Properties[name];
                    if (property == null || !property.IsGettable || !property.IsSettable) {
                        return;
                    }
                    object owner = target.BaseObject is PSCustomObject ? target : target.BaseObject;
                    OwnerProperties properties = Owners.GetValue(owner, _ => new OwnerProperties());
                    properties.Values.TryGetValue(property.Name, out PropertyOwnership? ownership);
                    object? original = ownership == null ? property.Value : null;
                    property.Value = value;
                    if (ownership == null) {
                        ownership = new PropertyOwnership(properties, property, original);
                        properties.Values.Add(property.Name, ownership);
                    }
                    Lease? lease = _leases.Find(candidate => ReferenceEquals(candidate.Ownership, ownership));
                    if (lease == null) {
                        lease = new Lease(ownership, property, value);
                        _leases.Add(lease);
                    } else {
                        ownership.Order.Remove(lease);
                        lease.Property = property;
                        lease.Value = value;
                    }
                    ownership.Order.Add(lease);
                } catch (Exception error) when (Unavailable(error)) {
                    // Hosts and optional modules can expose unavailable or rejected preference properties.
                }
            }
        }

        /// <summary>Releases this installation; the latest survivor wins, or the first exact baseline if none remain.</summary>
        internal void Restore() {
            lock (Gate) {
                foreach (Lease lease in _leases) {
                    PropertyOwnership ownership = lease.Ownership;
                    bool current = ReferenceEquals(ownership.Order[ownership.Order.Count - 1], lease);
                    ownership.Order.Remove(lease);
                    if (ownership.Order.Count == 0) {
                        ownership.Owner.Values.Remove(ownership.Property.Name);
                    }
                    if (!current) {
                        continue;
                    }
                    Lease? survivor = ownership.Order.Count == 0 ? null : ownership.Order[ownership.Order.Count - 1];
                    try {
                        (survivor?.Property ?? ownership.Property).Value = survivor == null ? ownership.Original : survivor.Value;
                    } catch (Exception error) when (Unavailable(error)) {
                        // A removed module or disconnected host can make a saved property unavailable.
                    }
                }
                _leases.Clear();
            }
        }

        internal static PSObject? ReadObject(object? target, string name) {
            if (target == null) {
                return null;
            }
            try {
                object? value = PSObject.AsPSObject(target).Properties[name]?.Value;
                return value == null ? null : PSObject.AsPSObject(value);
            } catch (Exception error) when (Unavailable(error)) {
                return null;
            }
        }

        internal static bool Unavailable(Exception error) => error is RuntimeException || error is NotImplementedException
            || error is NotSupportedException || error is InvalidOperationException || error is ArgumentException;

        private sealed class OwnerProperties {
            internal Dictionary<string, PropertyOwnership> Values { get; } = new Dictionary<string, PropertyOwnership>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class PropertyOwnership {
            internal PropertyOwnership(OwnerProperties owner, PSPropertyInfo property, object? original) {
                Owner = owner;
                Property = property;
                Original = original;
            }

            internal OwnerProperties Owner { get; }
            internal PSPropertyInfo Property { get; }
            internal object? Original { get; }
            internal List<Lease> Order { get; } = new List<Lease>();
        }

        private sealed class Lease {
            internal Lease(PropertyOwnership ownership, PSPropertyInfo property, object value) {
                Ownership = ownership;
                Property = property;
                Value = value;
            }

            internal PropertyOwnership Ownership { get; }
            internal PSPropertyInfo Property { get; set; }
            internal object Value { get; set; }
        }
    }
}
