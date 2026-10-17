using System;
using System.Threading;

namespace ConsoleMonster.Live {
    /// <summary>Ambient progress reporting that flows with the asynchronous execution context.</summary>
    public static class TerminalScope {
        private static readonly AsyncLocal<ScopeState?> Ambient = new AsyncLocal<ScopeState?>();

        /// <summary>Whether the current execution context has a progress scope.</summary>
        public static bool IsActive => Ambient.Value != null;

        /// <summary>The ambient target, or null outside a scope.</summary>
        public static string? Target => Ambient.Value?.Target;

        /// <summary>The ambient item key, or null when unset.</summary>
        public static string? Key => Ambient.Value?.Key;

        /// <summary>Begins a scope. Dispose it in the same execution context to restore the previous scope.</summary>
        /// <param name="dispatcher">Event destination.</param>
        /// <param name="target">Target name.</param>
        /// <param name="key">Optional item key.</param>
        /// <returns>The scope lifetime.</returns>
        public static IDisposable Begin(ILiveDispatcher dispatcher, string target, string? key = null) {
            if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher)); }
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
            ScopeState state = new ScopeState(dispatcher, target, key, Ambient.Value);
            Ambient.Value = state;
            return state;
        }

        /// <summary>Reports detail to the ambient item.</summary>
        /// <param name="text">Detail text.</param>
        public static void Report(string text) => Send("detail", text: text);

        /// <summary>Advances the ambient item.</summary>
        /// <param name="amount">Increment, default one.</param>
        public static void Advance(double amount = 1) => Send("advance", value: amount);

        /// <summary>Changes the ambient item's state.</summary>
        /// <param name="state">State word.</param>
        /// <param name="text">Optional detail.</param>
        public static void SetState(string state, string? text = null) => Send("state", text, state);

        /// <summary>Writes to the ambient dispatcher's log target.</summary>
        /// <param name="text">Log line.</param>
        /// <param name="level">Log level.</param>
        public static void Log(string text, string level = "info") => Send("write", text, level, log: true);

        private static void Send(string operation, string? text = null, string? state = null, double value = 0, bool log = false) {
            ScopeState? scope = Ambient.Value;
            scope?.Dispatcher.Post(new LiveEvent(log ? "$log" : scope.Target, operation, scope.Key, text, state, value));
        }

        private sealed class ScopeState : IDisposable {
            internal readonly ILiveDispatcher Dispatcher;
            internal readonly string Target;
            internal readonly string? Key;
            private readonly ScopeState? _previous;

            internal ScopeState(ILiveDispatcher dispatcher, string target, string? key, ScopeState? previous) {
                Dispatcher = dispatcher;
                Target = target;
                Key = key;
                _previous = previous;
            }

            public void Dispose() {
                if (ReferenceEquals(Ambient.Value, this)) { Ambient.Value = _previous; }
            }
        }
    }
}
