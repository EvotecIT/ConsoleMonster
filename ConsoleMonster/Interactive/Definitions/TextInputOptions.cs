using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>One definition for single-line and multiline persistent text controls.</summary>
    public sealed class TextInputOptions {
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
        private bool _multiline;
        /// <summary>Whether to create a multiline text area.</summary>
        public bool Multiline { get => _multiline; set { _multiline = value; _bound.Add(nameof(Multiline)); } }
        private string? _value;
        /// <summary>The initial text; null is empty.</summary>
        public string? Value { get => _value; set { _value = value; _bound.Add(nameof(Value)); } }
        private string? _placeholder;
        /// <summary>The hint shown while the value is empty.</summary>
        public string? Placeholder { get => _placeholder; set { _placeholder = value; _bound.Add(nameof(Placeholder)); } }
        private int _maxLength = int.MaxValue;
        /// <summary>The maximum number of UTF-16 units.</summary>
        public int MaxLength { get => _maxLength; set { _maxLength = value; _bound.Add(nameof(MaxLength)); } }
        private int _height = 6;
        /// <summary>Visible rows; valid only with Multiline.</summary>
        public int Height { get => _height; set { _height = value; _bound.Add(nameof(Height)); } }
        private int _scrollRow;
        /// <summary>The initial visible row; valid only with Multiline.</summary>
        public int ScrollRow { get => _scrollRow; set { _scrollRow = value; _bound.Add(nameof(ScrollRow)); } }
        private string? _mask;
        /// <summary>Single-line masking; null shows text and an empty string hides it.</summary>
        public string? Mask { get => _mask; set { _mask = value; _bound.Add(nameof(Mask)); } }
        private ThemeToken _role = ThemeToken.Text;
        /// <summary>The single-line entered-text role.</summary>
        public ThemeToken Role { get => _role; set { _role = value; _bound.Add(nameof(Role)); } }
    }
}
