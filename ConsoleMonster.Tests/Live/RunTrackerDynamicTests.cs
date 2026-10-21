using System;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed class RunTrackerDynamicTests {
        [Fact]
        public void Add_AfterWorkHasStarted_PreservesStatesAndIndependentSnapshots() {
            RunTracker tracker = new RunTracker(1);
            tracker.Started(0, "discovery");
            RunSnapshot before = tracker.Snapshot();
            int index = tracker.Add();
            tracker.Finished(0, StatusKind.Pass, "discovery");
            tracker.Started(index, "discovered item");

            Assert.Equal(1, index);
            Assert.Equal(new[] { StatusKind.Running }, before.States);
            RunSnapshot current = tracker.Snapshot();
            Assert.Equal(new[] { StatusKind.Pass, StatusKind.Running }, current.States);
            Assert.Equal("discovered item", Assert.Single(current.Running).Label);
        }

        [Fact]
        public void Add_ConcurrentProducers_AssignsUniqueIndicesAndTracksEveryOutcome() {
            RunTracker tracker = new RunTracker(0);
            Parallel.For(0, 64, item => {
                int index = tracker.Add();
                tracker.Started(index, item.ToString());
                tracker.Finished(index, StatusKind.Pass, item.ToString());
            });
            tracker.Complete();

            RunSnapshot snapshot = tracker.Snapshot();
            Assert.Equal(64, tracker.Total);
            Assert.Equal(64, snapshot.Passed);
            Assert.Empty(snapshot.Running);
            Assert.All(snapshot.States, state => Assert.Equal(StatusKind.Pass, state));
        }

        [Fact]
        public void Add_AfterCompletion_RejectsNewWorkAndPreservesFinalSnapshot() {
            RunTracker tracker = new RunTracker(0);
            tracker.Add();
            tracker.Complete();

            Assert.Throws<InvalidOperationException>(() => tracker.Add());
            Assert.Equal(new[] { StatusKind.Skip }, tracker.Snapshot().States);
            Assert.True(tracker.Snapshot().IsFinished);
        }
    }
}
