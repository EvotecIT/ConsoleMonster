using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>One validated definition for ordinary, multiline and secret text questions.</summary>
    public sealed class TextQuestionOptions {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<string> Bound => _bound;
        internal bool Has(string field) => _bound.Contains(field);
        private string _title = string.Empty;
        /// <summary>The question.</summary>
        public string Title { get => _title; set { _title = value; _bound.Add(nameof(Title)); } }
        private string? _description;
        /// <summary>Help text below the question.</summary>
        public string? Description { get => _description; set { _description = value; _bound.Add(nameof(Description)); } }
        private string? _parameterName;
        /// <summary>The caller parameter named when a non-interactive answer is missing.</summary>
        public string? ParameterName { get => _parameterName; set { _parameterName = value; _bound.Add(nameof(ParameterName)); } }
        private bool _asSecret;
        /// <summary>Uses the secret editor, without history or defaults.</summary>
        public bool AsSecret { get => _asSecret; set { _asSecret = value; _bound.Add(nameof(AsSecret)); } }
        private bool _multiline;
        /// <summary>Uses the multiline editor; unavailable with AsSecret.</summary>
        public bool Multiline { get => _multiline; set { _multiline = value; _bound.Add(nameof(Multiline)); } }
        private string? _default;
        /// <summary>The default answer; setting null still explicitly supplies a default.</summary>
        public string? Default { get => _default; set { _default = value; _bound.Add(nameof(Default)); } }
        private string? _placeholder;
        /// <summary>The hint shown while empty.</summary>
        public string? Placeholder { get => _placeholder; set { _placeholder = value; _bound.Add(nameof(Placeholder)); } }
        private bool _required;
        /// <summary>Whether ordinary text must be nonempty.</summary>
        public bool Required { get => _required; set { _required = value; _bound.Add(nameof(Required)); } }
        private int _minLength;
        /// <summary>The minimum accepted character count.</summary>
        public int MinLength { get => _minLength; set { _minLength = value; _bound.Add(nameof(MinLength)); } }
        private int _maxLength = int.MaxValue;
        /// <summary>The maximum character count for ordinary text.</summary>
        public int MaxLength { get => _maxLength; set { _maxLength = value; _bound.Add(nameof(MaxLength)); } }
        private string? _pattern;
        /// <summary>A pattern which the entire single-line answer must match.</summary>
        public string? Pattern { get => _pattern; set { _pattern = value; _bound.Add(nameof(Pattern)); } }
        private string? _patternMessage;
        /// <summary>The error for an unmatched pattern.</summary>
        public string? PatternMessage { get => _patternMessage; set { _patternMessage = value; _bound.Add(nameof(PatternMessage)); } }
        private Func<string, string?>? _validator;
        /// <summary>Returns an error, or null to accept; secrets must not be logged.</summary>
        public Func<string, string?>? Validator { get => _validator; set { _validator = value; _bound.Add(nameof(Validator)); } }
        private bool _prefillDefault;
        /// <summary>Puts the single-line default in the editor.</summary>
        public bool PrefillDefault { get => _prefillDefault; set { _prefillDefault = value; _bound.Add(nameof(PrefillDefault)); } }
        private int _maxLines = int.MaxValue;
        /// <summary>Maximum lines for multiline text.</summary>
        public int MaxLines { get => _maxLines; set { _maxLines = value; _bound.Add(nameof(MaxLines)); } }
        private int _height = 6;
        /// <summary>Visible multiline rows.</summary>
        public int Height { get => _height; set { _height = value; _bound.Add(nameof(Height)); } }
        private bool _hidden;
        /// <summary>Hides even the secret length.</summary>
        public bool Hidden { get => _hidden; set { _hidden = value; _bound.Add(nameof(Hidden)); } }
        private bool _allowEmpty;
        /// <summary>Allows an empty secret; secrets are required by default.</summary>
        public bool AllowEmpty { get => _allowEmpty; set { _allowEmpty = value; _bound.Add(nameof(AllowEmpty)); } }
        /// <summary>Earlier single-line answers, oldest first; forbidden for secrets.</summary>
        public IList<string> History { get; } = new List<string>();
    }
}
