using System;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class ValidatedHandleTests {
        [Fact]
        public void ImmediateAndQueuedHandlesRejectTheSameUnsupportedRequest() {
            var bar = new ProgressBar("Build", "build");
            var immediate = bar.Handle(new ImmediateDispatcher(bar));
            Assert.Throws<ConsoleUpdateException>(() => immediate.Update(new WidgetUpdate { Label = "Changed", Detail = "Unsupported" }));
            Assert.Equal("Build", bar.Label);
            using (var session = new LiveSession(bar)) {
                Assert.Throws<ConsoleUpdateException>(() => bar.Handle(session).Update(new WidgetUpdate { Label = "Changed", Detail = "Unsupported" }));
                Assert.Equal(0, session.EventsPosted);
            }
        }

        [Fact]
        public void TypedProgressRetainsZeroAndExplicitUnknownTotal() {
            var bar = new ProgressBar { Value = 3, Total = 10 };
            var handle = bar.Handle(new ImmediateDispatcher(bar));
            handle.Advance(0);
            Assert.Equal(3, bar.Value);
            handle.Set(5);
            Assert.Equal(10, bar.Total);
            handle.Set(0, 0);
            Assert.Equal(0, bar.Value);
            Assert.Equal(0, bar.Total);
            Assert.Throws<ArgumentOutOfRangeException>(() => handle.Advance(double.NaN));
        }

        [Fact]
        public void LaneCompletionRetainsAnExplicitOutcome() {
            var lanes = new Lanes();
            var handle = lanes.Handle(new ImmediateDispatcher(lanes));
            handle.Add("build", total: 2);
            handle.Advance("build", StatusKind.Pass, increment: 0);
            handle.Complete("build", StatusKind.Skip);
            Assert.Equal(StatusKind.Skip, Assert.Single(lanes.Items).Status);
        }

        [Fact]
        public void KeyValueHandleKeepsMarkupAndStatusInTheSharedRequest() {
            var list = new KeyValueList();
            list.Handle(new ImmediateDispatcher(list)).Set("Result", "[bold]Ready[/]", StatusKind.Pass);
            var item = Assert.Single(list.Items);
            Assert.Equal("[bold]Ready[/]", item.Value);
            Assert.Equal(StatusKind.Pass, item.Status);
        }

        [Fact]
        public void ValidationUsesQueuedRegionKindsBeforeTheFirstRender() {
            var region = new RegionWidget("main", new ProgressBar("Old", "work"));
            using (var session = new LiveSession(region)) {
                session.Region("main").Set(new Spinner("New", "work"));
                ConsoleUpdate.Apply(session, "work", new WidgetUpdate { Detail = "Queued spinner" });
                long posted = session.EventsPosted;
                Assert.Throws<ConsoleUpdateException>(() => ConsoleUpdate.Apply(session, "work", new WidgetUpdate { Label = "Invalid", Value = 5 }));
                Assert.Equal(posted, session.EventsPosted);
                session.Region("main").Clear();
                Assert.Throws<ConsoleUpdateException>(() => ConsoleUpdate.Apply(session, "work", new WidgetUpdate { Label = "Removed" }));
            }
        }

        [Fact]
        public void PendingNestedRegionsCanBeAddressedAndTheirChildrenUpdatedInOrder() {
            var region = new RegionWidget("main");
            using (var session = new LiveSession(region)) {
                session.Region("main").Set(new RegionWidget("nested"));
                session.Region("nested").Set(new ProgressBar("Build", "build"));
                ConsoleUpdate.Apply(session, "build", new WidgetUpdate { Value = 5 });
                Assert.Equal(3, session.EventsPosted);
            }
        }

        [Fact]
        public void ImmediateDispatcherRejectsAnIncorrectAddress() {
            var bar = new ProgressBar("Build", "build");
            Assert.Throws<ConsoleUpdateException>(() => new ProgressHandle(new ImmediateDispatcher(bar), "typo").Set(5));
            Assert.Equal(0, bar.Value);
        }
    }
}
