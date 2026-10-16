using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>GetCounters exposes what went wrong behind the live screen: unknown targets, failed updates, an early end.</summary>
    [Collection("Live sessions")]
    public sealed class SessionCountersTests {
        [Fact]
        public void GetCounters_UpdatesForUnknownTargets_AreCountedByName() {
            FakeOutput output = new FakeOutput();
            LiveSessionCounters counters;
            using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(output))) {
                session.Start();
                for (int i = 0; i < 3; i++) {
                    session.Post(new LiveEvent("lanez", "note", text: "typo"));
                }
                session.Post(new LiveEvent("work", "note", text: "fine"));
                session.Stop();
                counters = session.GetCounters();
            }
            Assert.Equal(3, counters.UnknownTargets["lanez"]);
            Assert.Equal(3, counters.UnknownTargetEvents);
            Assert.Equal(3, counters.EventsDropped);
            Assert.Equal(1, counters.EventsApplied);
            Assert.True(counters.HasProblems);
            Assert.Null(counters.Failure);
        }

        [Fact]
        public void GetCounters_RenderThreadFailure_IsReportedAsTheFailure() {
            FakeOutput output = new FakeOutput();
            using (ManualResetEventSlim ended = new ManualResetEventSlim()) {
                output.Frame = () => throw new IOException("broken pipe");
                output.Ending = ended.Set;
                using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(output))) {
                    session.Start();
                    Assert.True(ended.Wait(TimeSpan.FromSeconds(10)));
                    session.Stop();
                    LiveSessionCounters counters = session.GetCounters();
                    Assert.Equal("broken pipe", Assert.IsType<IOException>(counters.Failure).Message);
                    Assert.True(counters.HasProblems);
                }
            }
        }

        [Fact]
        public void GetCounters_CleanRun_HasNoProblems() {
            using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(new FakeOutput()))) {
                session.Start();
                session.Post(new LiveEvent("work", "note", text: "fine"));
                session.Stop();
                Assert.False(session.GetCounters().HasProblems);
            }
        }
    }
}
