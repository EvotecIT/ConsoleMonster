using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>A filtered single-choice layout input. Arrows move; Enter or Space commits the focused choice.</summary>
    public sealed class Select : SelectionInputBase {
        /// <summary>Creates a single-choice input with no committed value.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="choices">Available choices, configured before running.</param>
        public Select(string key, IEnumerable<Choice> choices) : base(key, choices, false) { }

        /// <summary>The committed value; null also represents an empty selection. Assign only enabled choices' values.</summary>
        /// <exception cref="System.ArgumentException">A non-null value does not identify an enabled choice.</exception>
        public object? Value {
            get => ReadSingleValue();
            set => SetSingleValue(value);
        }
    }
}
