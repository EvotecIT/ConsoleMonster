using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A named place in a layout whose widget can be replaced while a live session runs: put it anywhere (inside a panel,
    /// a column, a dashboard), then swap its content with <see cref="LiveSession.Region"/> or a <see cref="SlotHandle"/>.
    /// The swap goes through the session's update queue, so it is thread-safe and lands between frames; the surrounding
    /// frame stays and only the changed cells are redrawn. Live widgets inside the new content are registered with the
    /// session under their names, replacing widgets of the same name.
    /// Live operations: <c>set</c> (Payload = the new widget), <c>clear</c>.
    /// </summary>
    public sealed class Slot : LiveWidget, IContainer {
        private IWidget _child;

        /// <summary>Creates a slot.</summary>
        /// <param name="name">The live name the slot is addressed by.</param>
        /// <param name="content">The first content, or null for an empty slot.</param>
        public Slot(string name, IWidget? content = null) : base(name) {
            _child = content ?? Empty.Instance;
        }

        /// <summary>The current content.</summary>
        public IWidget Content => _child;

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { _child };

        /// <summary>A handle that replaces the content through a dispatcher (a live session).</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public SlotHandle Handle(ILiveDispatcher dispatcher) => new SlotHandle(dispatcher, Name);

        /// <summary>Replaces the content directly (not thread-safe; use a handle while a session runs).</summary>
        /// <param name="content">The new content, or null to empty the slot.</param>
        public void Set(IWidget? content) => _child = content ?? Empty.Instance;

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            switch (liveEvent.Operation) {
                case "set":
                    if (liveEvent.Payload is IWidget widget) {
                        _child = widget;
                    } else if (liveEvent.Text != null) {
                        // A recording replays the text form of the content.
                        _child = new TextBlock(liveEvent.Text);
                    }
                    break;
                case "clear":
                    _child = Empty.Instance;
                    break;
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) => _child.Measure(constraints, context);

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) => _child.Render(region, context);

        private sealed class Empty : IWidget {
            internal static readonly Empty Instance = new Empty();

            public Size Measure(Constraints constraints, RenderContext context) => new Size(0, 0);

            public void Render(Region region, RenderContext context) {
            }
        }
    }

    /// <summary>Replaces the content of a <see cref="Slot"/> through the live session's update queue (thread-safe).</summary>
    public sealed class SlotHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher (a live session).</param>
        /// <param name="target">The slot name.</param>
        public SlotHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) {
        }

        /// <summary>Shows <paramref name="widget"/> in the slot from the next frame on.</summary>
        /// <param name="widget">The new content.</param>
        public void Set(IWidget widget) => PostPayload("set", widget ?? throw new ArgumentNullException(nameof(widget)));

        /// <summary>Shows <paramref name="widget"/> in the slot (same as <see cref="Set"/>).</summary>
        /// <param name="widget">The new content.</param>
        public void Replace(IWidget widget) => Set(widget);

        /// <summary>Empties the slot.</summary>
        public void Clear() => Post("clear");
    }
}
