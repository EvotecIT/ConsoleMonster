using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Tracks successfully written native preferences, retaining the first value for each property.</summary>
    internal sealed class NativeFormattingProperties {
        private readonly List<SavedProperty> _saved = new List<SavedProperty>();

        internal void Set(PSObject target, string name, object? value) {
            if (value == null) {
                return;
            }
            try {
                PSPropertyInfo? property = target.Properties[name];
                if (property == null || !property.IsGettable || !property.IsSettable) {
                    return;
                }
                object? original = property.Value;
                property.Value = value;
                object owner = target.BaseObject is PSCustomObject ? target : target.BaseObject;
                if (!_saved.Exists(saved => ReferenceEquals(saved.Owner, owner) && saved.Property.Name == name)) {
                    _saved.Add(new SavedProperty(owner, property, original));
                }
            } catch (Exception error) when (Unavailable(error)) {
                // Hosts and optional modules can expose unavailable or rejected preference properties.
            }
        }

        /// <summary>Restores owned values even if a caller edited them after installation.</summary>
        internal void Restore() {
            foreach (SavedProperty saved in _saved) {
                try {
                    saved.Property.Value = saved.Value;
                } catch (Exception error) when (Unavailable(error)) {
                    // A removed module or disconnected host can make a saved property unavailable.
                }
            }
            _saved.Clear();
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

        private sealed class SavedProperty {
            internal SavedProperty(object owner, PSPropertyInfo property, object? value) {
                Owner = owner;
                Property = property;
                Value = value;
            }

            internal object Owner { get; }
            internal PSPropertyInfo Property { get; }
            internal object? Value { get; }
        }
    }
}
