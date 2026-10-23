using System;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    public static partial class Terminal {
        private static readonly AsyncLocal<Func<TerminalProfile, TerminalProfile>?> ProfilePolicy = new AsyncLocal<Func<TerminalProfile, TerminalProfile>?>();

        /// <summary>
        /// Applies a host policy to profiles in the current asynchronous flow without replacing the target or pinning
        /// its dimensions. Prompts keep their keyboard and recording surface; console and live output keep ownership.
        /// </summary>
        /// <param name="policy">The profile transformation, applied after an enclosing policy when nested.</param>
        /// <returns>A scope that restores the previous policy.</returns>
        public static IDisposable UseProfilePolicy(Func<TerminalProfile, TerminalProfile> policy) {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            Func<TerminalProfile, TerminalProfile>? previous = ProfilePolicy.Value;
            ProfilePolicy.Value = previous == null ? policy : profile => policy(previous(profile));
            return new ProfilePolicyScope(previous);
        }

        internal static TerminalProfile ApplyProfilePolicy(TerminalProfile profile) => ProfilePolicy.Value?.Invoke(profile) ?? profile;

        internal static TerminalProfile GetTargetProfile(ITerminalTarget target) =>
            ApplyProfilePolicy(target is ConsoleTarget ? GetConsoleProfile() : target.Profile);

        private sealed class ProfilePolicyScope : IDisposable {
            private readonly Func<TerminalProfile, TerminalProfile>? _previous;
            private int _disposed;

            internal ProfilePolicyScope(Func<TerminalProfile, TerminalProfile>? previous) { _previous = previous; }

            public void Dispose() {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) ProfilePolicy.Value = _previous;
            }
        }
    }
}
