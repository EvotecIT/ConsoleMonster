using System;

namespace ConsoleMonster.Data {
    /// <summary>A named value whose getter is invoked only when the inspector reads it.</summary>
    public sealed class InspectMember {
        private readonly Func<object?> _read;

        /// <summary>Creates a member with a lazy value reader.</summary>
        public InspectMember(string name, Func<object?> read) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _read = read ?? throw new ArgumentNullException(nameof(read));
        }

        /// <summary>Creates a member with a fixed value.</summary>
        public InspectMember(string name, object? value)
            : this(name, () => value) { }

        /// <summary>The member name.</summary>
        public string Name { get; }

        /// <summary>Reads the member value; the read may throw.</summary>
        public object? Value => _read();

        /// <summary>Reads the member value; the read may throw.</summary>
        public object? ReadValue() => Value;
    }
}
