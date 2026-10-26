using System;

namespace ConsoleMonster {
    /// <summary>Creates operation options once before handing them to their existing owner.</summary>
    internal static class OptionConfiguration {
        internal static T Create<T>(Action<T> configure) where T : new() {
            if (configure == null) { throw new ArgumentNullException(nameof(configure)); }
            var options = new T();
            configure(options);
            return options;
        }
    }
}
