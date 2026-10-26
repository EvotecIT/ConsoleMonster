namespace ConsoleMonster.Live {
    /// <summary>A dispatcher that validates a complete update against its registered target before publishing events.</summary>
    public interface IWidgetUpdateDispatcher : ILiveDispatcher {
        /// <summary>Applies one presence-aware update, rejecting unsupported fields before any event is published.</summary>
        /// <param name="widgetId">The target identifier.</param>
        /// <param name="update">The explicitly supplied fields.</param>
        void Update(string widgetId, WidgetUpdate update);
    }
}
