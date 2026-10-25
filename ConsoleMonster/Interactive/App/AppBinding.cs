using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A display widget rebuilt from state on the application's input thread, then swapped through the live dispatcher.</summary>
    public sealed class AppBinding : IContainer {
        private static long _next;
        private readonly RegionWidget _slot;
        private readonly Func<AppState, IWidget> _build;

        /// <summary>Creates a dependent display view.</summary>
        /// <param name="content">Creates a fresh display widget from the current state; inputs belong in the stable outer layout.</param>
        /// <param name="widgetId">An optional stable live-region name.</param>
        public AppBinding(Func<AppState, IWidget> content, string? widgetId = null) {
            _build = content ?? throw new ArgumentNullException(nameof(content));
            _slot = new RegionWidget(widgetId ?? nameof(AppBinding) + Interlocked.Increment(ref _next));
        }

        /// <summary>The stable live-region identifier.</summary>
        public string WidgetId => _slot.WidgetId;

        /// <summary>The backing live region.</summary>
        public IReadOnlyList<IWidget> Children => new IWidget[] { _slot };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => _slot.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => _slot.Render(region, context);

        internal void Refresh(AppState state, LiveSession? session) {
            IWidget content = _build(state) ?? throw new InvalidOperationException(AppMessages.Get("EmptyBinding"));
            using (IEnumerator<InputControl> inputs = FocusManager.Discover(content).GetEnumerator()) {
                if (inputs.MoveNext()) { throw new InvalidOperationException(AppMessages.Get("BoundInputs")); }
            }
            if (session == null) { _slot.Set(content); }
            else { _slot.Handle(session).Set(content); }
        }
    }
}
