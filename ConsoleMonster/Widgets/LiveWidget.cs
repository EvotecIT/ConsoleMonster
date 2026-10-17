using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Base class for widgets that accept live updates. State is mutated only through <see cref="Apply"/>, which a live
    /// session calls on its render thread; typed handles post events from any thread.
    /// </summary>
    public abstract class LiveWidget : IWidget, ILiveTarget {
        /// <summary>Creates a live widget.</summary>
        /// <param name="name">The unique name events are addressed to.</param>
        protected LiveWidget(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A live widget needs a name.", nameof(name));
            }
            Name = name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public abstract Size Measure(Constraints constraints, RenderContext context);

        /// <inheritdoc />
        public abstract void Render(Region region, RenderContext context);

        /// <inheritdoc />
        public abstract void Apply(LiveEvent liveEvent);

        /// <inheritdoc />
        public virtual string? Describe(LiveEvent liveEvent) => null;

        /// <summary>A dispatcher that applies events to this widget immediately (no live session).</summary>
        /// <returns>The dispatcher.</returns>
        protected ILiveDispatcher Immediate() => new ImmediateDispatcher(this);
    }
}
