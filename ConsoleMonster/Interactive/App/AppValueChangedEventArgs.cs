using System;

namespace ConsoleMonster.Interactive {
    /// <summary>A committed input or application-state value change.</summary>
    public sealed class AppValueChangedEventArgs : EventArgs {
        /// <summary>Creates a change notification.</summary>
        /// <param name="key">The state key or input name.</param>
        /// <param name="previousValue">The value before the change.</param>
        /// <param name="value">The value after the change.</param>
        public AppValueChangedEventArgs(string key, object? previousValue, object? value) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            PreviousValue = previousValue;
            Value = value;
        }

        /// <summary>The state key or input name.</summary>
        public string Key { get; }

        /// <summary>The value before the change.</summary>
        public object? PreviousValue { get; }

        /// <summary>The committed value.</summary>
        public object? Value { get; }
    }
}
