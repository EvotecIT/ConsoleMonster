using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>One definition for single and multiple persistent selection controls.</summary>
    public sealed class SelectInputOptions {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<string> Bound => _bound;
        internal bool Has(string field) => _bound.Contains(field);
        private string _key = string.Empty;
        /// <summary>The unique application-state key.</summary>
        public string Key { get => _key; set { _key = value; _bound.Add(nameof(Key)); } }
        private string? _label;
        /// <summary>The visible label, or null to use the key.</summary>
        public string? Label { get => _label; set { _label = value; _bound.Add(nameof(Label)); } }
        private bool _disabled;
        /// <summary>Whether input and keyboard focus are disabled.</summary>
        public bool Disabled { get => _disabled; set { _disabled = value; _bound.Add(nameof(Disabled)); } }
        private int _tabIndex;
        /// <summary>Keyboard focus order; equal values preserve declaration order.</summary>
        public int TabIndex { get => _tabIndex; set { _tabIndex = value; _bound.Add(nameof(TabIndex)); } }
        private string? _validationMessage;
        /// <summary>The initial validation message.</summary>
        public string? ValidationMessage { get => _validationMessage; set { _validationMessage = value; _bound.Add(nameof(ValidationMessage)); } }
        private EventHandler<AppValueChangedEventArgs>? _onChange;
        /// <summary>Runs after a committed value change on the application input thread.</summary>
        public EventHandler<AppValueChangedEventArgs>? OnChange { get => _onChange; set { _onChange = value; _bound.Add(nameof(OnChange)); } }
        private bool _multiple;
        /// <summary>Whether the committed value is a collection.</summary>
        public bool Multiple { get => _multiple; set { _multiple = value; _bound.Add(nameof(Multiple)); } }
        private object? _value;
        /// <summary>The initial scalar or collection; omission preserves choices marked Selected.</summary>
        public object? Value { get => _value; set { _value = value; _bound.Add(nameof(Value)); } }
        /// <summary>The prepared choices, retaining original values and enabled/selected metadata.</summary>
        public IList<Choice> Choices { get; } = new List<Choice>();
    }
}
