using System;

namespace ConsoleMonster.Live {
    /// <summary>
    /// Base class for typed live handles (<c>LanesHandle</c>, <c>LogHandle</c>...). A handle only builds events and posts
    /// them to its dispatcher, so it is safe to use from any thread once attached to a live session.
    /// </summary>
    public abstract class LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">Where events go (a live session, or an immediate dispatcher).</param>
        /// <param name="target">The target widget name.</param>
        protected LiveHandle(ILiveDispatcher dispatcher, string target) {
            Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>Where events go.</summary>
        public ILiveDispatcher Dispatcher { get; }

        /// <summary>The target widget name.</summary>
        public string Target { get; }

        /// <summary>Builds and posts an event for this handle's target.</summary>
        /// <param name="operation">The operation.</param>
        /// <param name="key">Item key.</param>
        /// <param name="text">Text.</param>
        /// <param name="state">State word.</param>
        /// <param name="value">Value.</param>
        /// <param name="total">Total.</param>
        protected void Post(string operation, string? key = null, string? text = null, string? state = null, double value = 0, double total = 0) {
            Dispatcher.Post(new LiveEvent(Target, operation, key, text, state, value, total));
        }
    }
}
