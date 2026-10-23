using System;

namespace ConsoleMonster.Interactive {
    /// <summary>A committed input or application-state value change.</summary>
    public sealed class AppValueChangedEventArgs : EventArgs {
        /// <summary>Creates a change notification.</summary>
        /// <param name="name">The state key or input name.</param>
        /// <param name="previousValue">The value before the change.</param>
        /// <param name="value">The value after the change.</param>
        public AppValueChangedEventArgs(string name, object? previousValue, object? value) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            PreviousValue = previousValue;
            Value = value;
        }

        /// <summary>The state key or input name.</summary>
        public string Name { get; }

        /// <summary>The value before the change.</summary>
        public object? PreviousValue { get; }

        /// <summary>The committed value.</summary>
        public object? Value { get; }
    }
}
