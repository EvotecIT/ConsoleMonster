using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A literal labelled hop with a stable identity, optional status and category chips.</summary>
    public sealed class PathHop {
        /// <summary>Creates a path hop.</summary>
        /// <param name="key">Identity used to recognise shared hops across chains.</param>
        /// <param name="label">The plain text label.</param>
        public PathHop(string key, string label) {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A hop needs an identity.", nameof(key));
            Key = key;
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        /// <summary>The ordinal, case-sensitive hop identity.</summary>
        public string Key { get; }

        /// <summary>The plain text label.</summary>
        public string Label { get; }

        /// <summary>An optional status, rendered with its glyph and theme role.</summary>
        public StatusKind? Status { get; set; }

        /// <summary>Optional label role, independent of the health marker.</summary>
        public ThemeToken? Role { get; set; }

        /// <summary>Whether the hop label is bold.</summary>
        public bool Bold { get; set; }

        /// <summary>Plain text categories rendered by the existing chip presenter.</summary>
        public IList<string> Chips { get; } = new List<string>();
    }
}
