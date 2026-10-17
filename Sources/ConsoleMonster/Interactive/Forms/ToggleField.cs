using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>An on/off switch (a switch parameter in the equivalent command).</summary>
    public sealed class ToggleField : Field {
        /// <summary>Creates a toggle.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        public ToggleField(string name, string? label = null) : base(name, label) {
            Default = false;
        }

        /// <inheritdoc />
        public override FieldKind Kind => FieldKind.Toggle;

        /// <summary>Text after the box when on; null uses "Yes".</summary>
        public string? OnText { get; set; }

        /// <summary>Text after the box when off; null uses "No".</summary>
        public string? OffText { get; set; }

        /// <inheritdoc />
        public override string Display(object? value, PromptLabels labels) => value is true ? OnText ?? labels.Yes : OffText ?? labels.No;

        internal override bool IsEmpty(object? value) => false;

        internal override bool TryRestore(object? stored, out object? value) {
            value = stored is true || (stored is string s && bool.TryParse(s, out bool b) && b);
            return true;
        }
    }
}
