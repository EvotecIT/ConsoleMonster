using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>One definition for single and multiple persistent selection controls.</summary>
    public sealed class SelectInputOptions : AppInputOptions {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<string> Bound => _bound;
        internal bool Has(string field) => _bound.Contains(field);
        private bool _multiple;
        /// <summary>Whether the committed value is a collection.</summary>
        public bool Multiple { get => _multiple; set { _multiple = value; _bound.Add(nameof(Multiple)); } }
        private object? _value;
        /// <summary>The initial scalar or collection; omission preserves choices marked Selected.</summary>
        public object? Value { get => _value; set { _value = value; _bound.Add(nameof(Value)); } }
        /// <summary>Visible choice rows; null keeps the control default.</summary>
        public int? PageSize { get; set; }
        /// <summary>Shows a single-choice list with radio indicators.</summary>
        public bool Radio { get; set; }
        /// <summary>Shows one row with the current choice and opens the list over the content below.</summary>
        public bool Dropdown { get; set; }
        /// <summary>The prepared choices, retaining original values and enabled/selected metadata.</summary>
        public IList<Choice> Choices { get; } = new List<Choice>();
    }
}
