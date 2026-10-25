using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>One definition for scalar and multiple-choice questions, retaining the original values.</summary>
    public sealed class SelectQuestionOptions {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal bool Has(string name) => _bound.Contains(name);
        /// <summary>The question.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Help text below the question.</summary>
        public string? Description { get; set; }
        /// <summary>The caller parameter named when an answer is missing.</summary>
        public string? ParameterName { get; set; }
        /// <summary>Whether the answer is a collection instead of one value.</summary>
        public bool Multiple { get; set; }
        /// <summary>The prepared choices; their values, enabled state and initial selection are retained.</summary>
        public IList<Choice> Choices { get; } = new List<Choice>();
        private object? _default;
        /// <summary>The default scalar or collection. Omission differs from an explicitly empty selection.</summary>
        public object? Default { get => _default; set { _default = value; _bound.Add(nameof(Default)); } }
        private int? _minimum;
        /// <summary>The fewest answers; valid only with Multiple.</summary>
        public int? Minimum { get => _minimum; set { _minimum = value; _bound.Add(nameof(Minimum)); } }
        private int? _maximum;
        /// <summary>The most answers; valid only with Multiple.</summary>
        public int? Maximum { get => _maximum; set { _maximum = value; _bound.Add(nameof(Maximum)); } }
        private bool _collapsed;
        /// <summary>Whether groups start collapsed; valid only with Multiple.</summary>
        public bool Collapsed { get => _collapsed; set { _collapsed = value; _bound.Add(nameof(Collapsed)); } }
        /// <summary>The preferred number of visible choices.</summary>
        public int PageSize { get; set; } = 10;
        /// <summary>Preview content for the focused value.</summary>
        public Func<object?, IWidget?>? Preview { get; set; }
    }
}
