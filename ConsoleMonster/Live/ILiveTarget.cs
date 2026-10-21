namespace ConsoleMonster.Live {
    /// <summary>
    /// A widget that accepts live updates. <see cref="Apply"/> and <see cref="Describe"/> are only ever called on the
    /// session's render thread during normal operation. After that thread joins, the stopping caller may perform the final
    /// drain. These phases are serialized, so implementations keep plain, unsynchronised state. Without a session, the
    /// definition thread owns updates.
    /// </summary>
    public interface ILiveTarget {
        /// <summary>The unique name events are addressed to.</summary>
        string Name { get; }

        /// <summary>Applies an event to the widget's state.</summary>
        /// <param name="liveEvent">The event.</param>
        void Apply(LiveEvent liveEvent);

        /// <summary>
        /// Returns a line describing the event for line-based outputs (Stream/Plain), or <c>null</c> when the event is not
        /// worth a line (for example a single counter increment). Called after <see cref="Apply"/>.
        /// </summary>
        /// <param name="liveEvent">The event that was applied.</param>
        /// <returns>A plain line or <c>null</c>.</returns>
        string? Describe(LiveEvent liveEvent);
    }
}
