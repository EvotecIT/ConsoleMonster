using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>A filtered multi-choice layout input. Space or Enter toggles a choice; standard list shortcuts remain available.</summary>
    internal sealed class MultiSelect : SelectionInputBase {
        /// <summary>Creates a multi-choice input honoring enabled choices marked as initially selected.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="choices">Available choices, configured before running.</param>
        public MultiSelect(string key, IEnumerable<Choice> choices) : base(key, choices, true) { }

        /// <summary>A detached, read-only snapshot in choice declaration order. Assignments replace the selection atomically.</summary>
        /// <exception cref="System.ArgumentNullException">The assigned list is null.</exception>
        /// <exception cref="System.ArgumentException">A value does not identify an enabled choice.</exception>
        public IReadOnlyList<object?> Value {
            get => ReadMultipleValues();
            set => SetMultipleValues(value);
        }
    }
}
