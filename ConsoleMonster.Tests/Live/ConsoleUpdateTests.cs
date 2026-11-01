using System;
using System.Linq;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class ConsoleUpdateTests {
        private static void Apply(ILiveTarget target, WidgetUpdate update) {
            foreach (LiveEvent item in ConsoleUpdate.Events(target, update)) { target.Apply(item); }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void QueuedPhaseCreationMakesItsItemsAvailableBeforeRendering(int placement) {
            var phases = new Phases("phases");
            ConsoleMonster.Layout.IWidget root = placement == 0 ? (ConsoleMonster.Layout.IWidget)phases
                : placement == 1 ? new TextBlock("Host") : new RegionWidget("slot");
            using (var session = new LiveSession(root, LiveSessionTests.Options(new FakeOutput(false)))) {
                if (placement == 1) { session.Register(phases); }
                if (placement >= 2) { session.Region("slot").Set(phases); }
                var handle = phases.Handle(session);
                handle.Start("migrate");
                if (placement == 3) {
                    session.Region("slot").Clear();
                    session.Region("slot").Set(phases);
                }
                handle.Item("migrate", "alice", StatusKind.Running, "40%");
                long accepted = session.EventsPosted;
                Assert.Equal(0, session.EventsApplied);
                var error = Assert.Throws<ConsoleUpdateException>(() => handle.Item("unknown", "bob", StatusKind.Pass));
                Assert.Equal("ConsoleMonsterUnknownItem", error.ErrorId);
                Assert.Equal(accepted, session.EventsPosted);
                session.Start();
                session.Stop();
                Assert.Equal(0, session.ApplyErrors);
                Assert.Contains("alice 40%", Terminal.RenderText(phases, ConsoleMonster.Capabilities.TerminalProfile.Fixed(80, 20)));
            }
        }

        [Fact]
        public void ProgressTotalKeepsTheQueuedValueAndAcceptsUnknownTotal() {
            var progress = new ProgressBar { Value = 3, Total = 10 };
            Apply(progress, new WidgetUpdate { Value = 5 });
            Apply(progress, new WidgetUpdate { Total = 20 });
            Assert.Equal(5, progress.Value);
            Assert.Equal(20, progress.Total);
            Apply(progress, new WidgetUpdate { Total = 0 });
            Assert.Equal(5, progress.Value);
            Assert.Equal(0, progress.Total);
            Apply(progress, new WidgetUpdate { Value = 0 });
            Assert.Equal(0, progress.Value);
        }

        [Fact]
        public void ZeroIncrementDoesNotUseTheLegacyProtocolDefaultOfOne() {
            var progress = new ProgressBar { Value = 5 };
            Apply(progress, new WidgetUpdate { Increment = 0 });
            Assert.Equal(5, progress.Value);
            Apply(progress, new WidgetUpdate { Increment = 0.5 });
            Assert.Equal(5.5, progress.Value);
        }

        [Fact]
        public void NullAndEmptyLabelsClearRatherThanOmitTheChange() {
            var progress = new ProgressBar("Build");
            Apply(progress, new WidgetUpdate { Increment = 1 });
            Assert.Equal("Build", progress.Label);
            Apply(progress, new WidgetUpdate { Label = null });
            Assert.Equal(string.Empty, progress.Label);
            Apply(progress, new WidgetUpdate { Label = "" });
            Assert.Equal(string.Empty, progress.Label);
        }

        [Fact]
        public void InvalidSecondFieldDoesNotPublishTheFirstField() {
            var progress = new ProgressBar("Build", "build");
            using (var session = new LiveSession(progress)) {
                var error = Assert.Throws<ConsoleUpdateException>(() => ConsoleUpdate.Apply(session, "build", new WidgetUpdate { Label = "Changed", Detail = "Unsupported" }));
                Assert.Equal("ConsoleMonsterUpdateParameterNotSupported", error.ErrorId);
                Assert.Equal(0, session.EventsPosted);
                Assert.Equal("Build", progress.Label);
            }
        }

        [Fact]
        public void UnknownTargetNamesAvailableTargets() {
            using (var session = new LiveSession(new ProgressBar("Build", "build"))) {
                var error = Assert.Throws<ConsoleUpdateException>(() => ConsoleUpdate.Apply(session, "typo", new WidgetUpdate { Value = 1 }));
                Assert.Equal("ConsoleMonsterUnknownWidget", error.ErrorId);
                Assert.Contains("build", error.Message);
                Assert.Equal(0, session.EventsPosted);
            }
        }

        [Fact]
        public void KeyedTargetsNameTheWidgetWhenKeyIsMissing() {
            var list = new StatusList("checks");
            var error = Assert.Throws<ArgumentException>(() => Apply(list, new WidgetUpdate { Status = StatusKind.Pass }));
            Assert.Equal("StatusList 'checks' updates one item at a time: give the item's Key.", error.Message);
        }

        [Fact]
        public void CellBatchRejectsEveryIndexBeforeCreatingEvents() {
            var cells = new CellMap();
            Assert.Throws<ArgumentException>(() => Apply(cells, new WidgetUpdate { Index = new[] { 0, -1 }, Status = StatusKind.Pass }));
            Apply(cells, new WidgetUpdate { CellCount = 3 });
            Apply(cells, new WidgetUpdate { Index = new[] { 0, 2 }, Status = StatusKind.Pass });
            Apply(cells, new WidgetUpdate { AppendCells = true, Count = 2, Status = StatusKind.Warn });
            Assert.Equal(5, cells.Count);
        }

        [Fact]
        public void CellCapacityErrorsRejectTheWholeBatchBeforePublication() {
            var cells = new CellMap(widgetId: "cells");
            using (var session = new LiveSession(cells)) {
                Assert.Throws<ArgumentException>(() => ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { Index = new[] { 0, CellMap.MaxItems }, Status = StatusKind.Pass }));
                Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { CellCount = CellMap.MaxItems + 1 }));
                Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { AppendCells = true, Count = CellMap.MaxItems + 1 }));
                Assert.Throws<ArgumentException>(() => ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { CellCount = 3, Status = StatusKind.Pass }));
                Assert.Equal(0, session.EventsPosted);
                Assert.Equal(0, cells.Count);
                Assert.Single(ConsoleUpdate.Events(cells, new WidgetUpdate { Index = new[] { CellMap.MaxItems - 1 }, Status = StatusKind.Pass }));
                Assert.Single(ConsoleUpdate.Events(cells, new WidgetUpdate { CellCount = CellMap.MaxItems }));
            }
        }

        [Fact]
        public void CellAppendUsesAcceptedQueuedCapacityIncludingRawEvents() {
            var cells = new CellMap("cells").Resize(CellMap.MaxItems - 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Events(cells, new WidgetUpdate { AppendCells = true, Count = 2 }));
            using (var session = new LiveSession(new CellMap("queued"))) {
                ConsoleUpdate.Apply(session, "queued", new WidgetUpdate { CellCount = CellMap.MaxItems - 1 });
                Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Apply(session, "queued", new WidgetUpdate { AppendCells = true, Count = 2 }));
                Assert.Equal(1, session.EventsPosted);
                session.Post(new LiveEvent("queued", "add"));
                Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Apply(session, "queued", new WidgetUpdate { AppendCells = true }));
                Assert.Equal(2, session.EventsPosted);
                session.Post(new LiveEvent("queued", "count", value: 1));
                ConsoleUpdate.Apply(session, "queued", new WidgetUpdate { AppendCells = true, Count = 2 });
                Assert.Equal(5, session.EventsPosted);
                session.Post(new LiveEvent("queued", "set", key: (CellMap.MaxItems - 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Apply(session, "queued", new WidgetUpdate { AppendCells = true }));
                Assert.Equal(6, session.EventsPosted);
            }
        }

        [Fact]
        public void ConcurrentCellAppendsReserveCapacityBeforePublication() {
            using (var session = new LiveSession(new CellMap("cells"))) {
                ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { CellCount = CellMap.MaxItems - 1 });
                int accepted = 0, rejected = 0;
                System.Threading.Tasks.Parallel.For(0, 2, _ => {
                    try {
                        ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { AppendCells = true });
                        System.Threading.Interlocked.Increment(ref accepted);
                    } catch (ArgumentOutOfRangeException) { System.Threading.Interlocked.Increment(ref rejected); }
                });
                Assert.Equal(1, accepted);
                Assert.Equal(1, rejected);
                Assert.Equal(2, session.EventsPosted);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CombinedUpdatesPreserveExplicitClears(string? clear) {
            var dashboard = new Dashboard("Old") { Detail = "Old detail", Status = StatusKind.Running };
            var titleEvents = ConsoleUpdate.Events(dashboard, new WidgetUpdate { Label = clear });
            var detailEvents = ConsoleUpdate.Events(dashboard, new WidgetUpdate { Detail = clear });
            var statusEvents = ConsoleUpdate.Events(dashboard, new WidgetUpdate { Status = StatusKind.Pass });
            foreach (LiveEvent item in titleEvents.Concat(detailEvents).Concat(statusEvents)) { dashboard.Apply(item); }
            Assert.Equal(string.Empty, dashboard.Title);
            Assert.Equal(clear, dashboard.Detail);
            Assert.Equal(StatusKind.Pass, dashboard.Status);

            var spinner = new Spinner("Old") { Detail = "Old detail" };
            Apply(spinner, new WidgetUpdate { Restart = true, Label = clear, Status = StatusKind.Pass, Detail = clear });
            Assert.Equal(string.Empty, spinner.Label);
            Assert.Equal(clear, spinner.Detail);
            Assert.Equal(StatusKind.Pass, spinner.Status);

            var lanes = new Lanes();
            lanes.Add("build", "Old").Detail = "Old detail";
            Apply(lanes, new WidgetUpdate { Key = "build", Label = clear, Status = StatusKind.Pass, Detail = clear });
            Assert.Equal(string.Empty, lanes.Items[0].Label);
            Assert.Equal(clear, lanes.Items[0].Detail);
            lanes.Items[0].Detail = "Old detail";
            Apply(lanes, new WidgetUpdate { Key = "build", Result = StatusKind.Pass, Increment = 0, Complete = true, Detail = clear });
            Assert.Equal(clear, lanes.Items[0].Detail);

            var phases = new Phases().Set("build", StatusKind.Running, detail: "Old detail");
            Apply(phases, new WidgetUpdate { Key = "build", Status = StatusKind.Pass, Detail = clear });
            Assert.Equal(clear, phases.Items[0].Detail);
        }

        [Fact]
        public void RowPayloadRetainsTypedCellsAndIsCopiedBeforePublication() {
            var table = new LiveTable(new Table().AddColumn("Name").AddColumn("Count"));
            object?[] values = { "web01", 12 };
            LiveEvent item = Assert.Single(ConsoleUpdate.Events(table, new WidgetUpdate { Row = new ConsoleTableRow { Key = "web01", Values = values } }));
            values[1] = 99;
            Assert.Equal(12, ((object?[])item.Payload!)[1]);
            table.Apply(item);
            Assert.Throws<ArgumentException>(() => Apply(table, new WidgetUpdate { Row = new ConsoleTableRow { Key = "" } }));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void NonfiniteValuesFailBeforeLabelEvents(double invalid) {
            Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleUpdate.Events(new ProgressBar(), new WidgetUpdate { Label = "Changed", Value = invalid }));
        }

        [Fact]
        public void ValueAndIncrementAreMutuallyExclusiveEvenWhenBothAreZero() {
            Assert.Throws<ArgumentException>(() => ConsoleUpdate.Events(new ProgressBar(), new WidgetUpdate { Value = 0, Increment = 0 }));
        }

        [Fact]
        public void SampleAppendAndScalarIncrementAreDifferentOperations() {
            var sparkline = new Sparkline();
            Apply(sparkline, new WidgetUpdate { Append = true, Value = 0 });
            Apply(sparkline, new WidgetUpdate { Append = true, Value = 0.5 });
            Assert.Equal(new[] { 0.0, 0.5 }, sparkline.Values.ToArray());
            Assert.Throws<ConsoleUpdateException>(() => Apply(sparkline, new WidgetUpdate { Increment = 1 }));
        }

        [Fact]
        public void LabelOnlyStatusUpdateKeepsTheOutcomeAndCanClearItsLabel() {
            var list = new StatusList().Set("check", "Old", StatusKind.Pass, "Ready");
            Apply(list, new WidgetUpdate { Key = "check", Label = null });
            Assert.Equal(StatusKind.Pass, list.Items[0].Status);
            Assert.Equal(string.Empty, list.Items[0].Label);
            Assert.Equal("Ready", list.Items[0].Detail);
            Apply(list, new WidgetUpdate { Key = "check", Detail = null });
            Assert.Null(list.Items[0].Detail);
        }

        [Fact]
        public void ActivityDetailDoesNotResetPercentageOrLabel() {
            var activities = new ActivityList();
            Apply(activities, new WidgetUpdate { Key = "7", Label = "Compile", Value = 50 });
            Apply(activities, new WidgetUpdate { Key = "7", Detail = "Linking" });
            Assert.True(activities.TryGetTaskbarProgress(out _, out double fraction));
            Assert.Equal(0.5, fraction);
            Apply(activities, new WidgetUpdate { Key = "7", Complete = true });
            Assert.Equal(0, activities.Count);
        }

        [Fact]
        public void RunWaitTimeoutLeavesWorkOpenAndCompletedWorkCanBeFinalized() {
            var output = new FakeOutput(false);
            using (var run = new LiveRun(new[] { new RunItem("Build") }, new LiveRunOptions {
                Live = false, ProgressInterval = TimeSpan.Zero, Session = LiveSessionTests.Options(output)
            })) {
                Assert.False(run.Wait(TimeSpan.Zero));
                Assert.False(run.Snapshot().IsFinished);
                ConsoleUpdate.Apply(run, new RunUpdate { Index = 0, Status = StatusKind.Running, Label = "Compiling" });
                ConsoleUpdate.Apply(run, new RunUpdate { Index = 0, Status = StatusKind.Pass, Detail = "Ready", Duration = TimeSpan.FromSeconds(0.5) });
                Assert.True(run.Wait(TimeSpan.Zero));
                Assert.False(run.Snapshot().IsFinished);
                Assert.Equal(1, run.Complete().Passed);
            }
        }
    }
}
