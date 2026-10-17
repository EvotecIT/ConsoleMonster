namespace ConsoleMonster.Live {
    /// <summary>
    /// Accepts live events. A running <c>LiveSession</c> queues them lock-free for its render thread; the
    /// <see cref="ImmediateDispatcher"/> applies them at once (definition phase, print-once use, tests).
    /// Implementations must make <see cref="Post"/> safe to call from any thread.
    /// </summary>
    public interface ILiveDispatcher {
        /// <summary>Posts an event.</summary>
        /// <param name="liveEvent">The event.</param>
        void Post(LiveEvent liveEvent);
    }
}
