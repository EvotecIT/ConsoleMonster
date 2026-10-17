using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Tests.Live {
    internal class FakeTarget : IWidget, ILiveTarget, ILiveSummary {
        internal readonly List<LiveEvent> Events = new List<LiveEvent>();
        internal readonly HashSet<int> ApplyThreads = new HashSet<int>();
        internal Action<LiveEvent>? Applying;
        internal Action<RenderContext>? Rendering;
        internal Func<LiveEvent, string?> Description = e => e.Text;
        internal string? Summary;

        internal FakeTarget(string name = "work") { Name = name; }

        public string Name { get; }

        public void Apply(LiveEvent liveEvent) {
            Applying?.Invoke(liveEvent);
            Events.Add(liveEvent);
            ApplyThreads.Add(Thread.CurrentThread.ManagedThreadId);
        }

        public string? Describe(LiveEvent liveEvent) => Description(liveEvent);

        public string? GetSummaryLine() => Summary;

        public Size Measure(Constraints constraints, RenderContext context) => new Size(1, 1);

        public void Render(Region region, RenderContext context) { Rendering?.Invoke(context); }
    }
}
