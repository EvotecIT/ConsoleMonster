using System;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        /// <summary>Shares compilation among live consumers without rooting a runner's previous input.</summary>
        private sealed class CustomPatternSlot {
            private readonly object _gate = new object();
            private WeakReference<Regex>? _pattern;

            internal Regex Get(string pattern) {
                lock (_gate) {
                    if (_pattern != null && _pattern.TryGetTarget(out Regex? existing) && existing != null) { return existing; }
                    var created = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled,
                        TimeSpan.FromMilliseconds(PatternTimeoutMilliseconds));
                    _pattern = new WeakReference<Regex>(created);
                    return created;
                }
            }
        }
    }
}
