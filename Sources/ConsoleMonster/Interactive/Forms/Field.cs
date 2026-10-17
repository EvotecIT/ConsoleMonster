using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A form field: a name (the key in <see cref="FormValues"/>), a label, help, a default, validation, a condition for
    /// showing it, the fields it depends on, and the command-line parameter it maps to for the equivalent command.
    /// </summary>
    public abstract class Field {
        /// <summary>Creates a field.</summary>
        /// <param name="name">The key in the form values.</param>
        /// <param name="label">The label; null uses the name.</param>
        protected Field(string name, string? label) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Label = label ?? name;
        }

        /// <summary>The key in the form values.</summary>
        public string Name { get; }

        /// <summary>The label.</summary>
        public string Label { get; set; }

        /// <summary>Help shown while the field has focus.</summary>
        public string? Help { get; set; }

        /// <summary>
        /// The command-line parameter this field supplies (<c>Domain</c>, <c>-Domain</c>, <c>--domain</c>); null leaves the
        /// field out of the equivalent command.
        /// </summary>
        public string? Parameter { get; set; }

        /// <summary>Whether an empty value is refused.</summary>
        public bool Required { get; set; }

        private object? _default;
        private object? _value;
        private bool _valueSet;

        /// <summary>
        /// The declared default: the starting value until a value is set, and values equal to it are left out of the
        /// equivalent command.
        /// </summary>
        public object? Default {
            get => _default;
            set {
                _default = value;
                if (!_valueSet) {
                    _value = CloneValue(value);
                }
            }
        }

        /// <summary>The current value.</summary>
        public object? Value {
            get => _value;
            set {
                _value = value;
                _valueSet = true;
            }
        }

        internal static object? CloneValue(object? value) {
            if (value is IEnumerable list && !(value is string)) {
                List<object?> copy = new List<object?>();
                foreach (object? item in list) {
                    copy.Add(item);
                }
                return copy;
            }
            return value;
        }

        /// <summary>Shows the field only while this returns true.</summary>
        public Func<FormValues, bool>? VisibleWhen { get; set; }

        /// <summary>Fields whose change resets this field (and reloads its options).</summary>
        public IList<string> DependsOn { get; } = new List<string>();

        /// <summary>A validator: returns an error message, or null when the value is fine.</summary>
        public Func<object?, FormValues, string?>? Validator { get; set; }

        /// <summary>Whether the value is kept in presets and remembered choices (never for secrets).</summary>
        public bool Remember { get; set; } = true;

        /// <summary>The kind.</summary>
        public abstract FieldKind Kind { get; }

        /// <summary>The value as shown in the form and on the review page.</summary>
        /// <param name="value">The value.</param>
        /// <param name="labels">The labels.</param>
        /// <returns>The text.</returns>
        public virtual string Display(object? value, PromptLabels labels) {
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        internal virtual bool IsEmpty(object? value) {
            if (value == null) {
                return true;
            }
            if (value is string s) {
                return s.Length == 0;
            }
            if (value is ICollection c) {
                return c.Count == 0;
            }
            return false;
        }

        internal virtual string? Check(object? value, FormValues values, PromptLabels labels) {
            if (Required && IsEmpty(value)) {
                return labels.Required;
            }
            return Validator?.Invoke(value, values);
        }

        /// <summary>The value given to the command line (lists, strings, numbers, a bool for switches).</summary>
        internal virtual object? ArgumentValue(object? value) => value;

        /// <summary>The value as stored in presets: a string, a bool, or a list of strings.</summary>
        internal virtual object? ToStored(object? value) {
            if (value == null) {
                return null;
            }
            if (value is bool b) {
                return b;
            }
            return CommandLine.Format(value);
        }

        /// <summary>Restores a stored value; returns false when it no longer fits (an option that disappeared).</summary>
        internal virtual bool TryRestore(object? stored, out object? value) {
            value = stored;
            return true;
        }

        internal bool IsDefault(object? value) {
            if (Default == null) {
                return IsEmpty(value);
            }
            if (value is IEnumerable a && !(value is string) && Default is IEnumerable b && !(Default is string)) {
                List<string> left = new List<string>();
                List<string> right = new List<string>();
                foreach (object? x in a) {
                    left.Add(Convert.ToString(x, CultureInfo.InvariantCulture) ?? string.Empty);
                }
                foreach (object? x in b) {
                    right.Add(Convert.ToString(x, CultureInfo.InvariantCulture) ?? string.Empty);
                }
                left.Sort(StringComparer.Ordinal);
                right.Sort(StringComparer.Ordinal);
                return string.Join("\u0001", left.ToArray()) == string.Join("\u0001", right.ToArray());
            }
            return Equals(Normalize(Default), Normalize(value));
        }

        private static object? Normalize(object? value) {
            if (value is int i) {
                return (decimal)i;
            }
            if (value is long l) {
                return (decimal)l;
            }
            if (value is double d) {
                return (decimal)d;
            }
            return value;
        }

        /// <inheritdoc />
        public override string ToString() => Label;
    }
}
