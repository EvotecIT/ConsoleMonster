using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class TerminalScopeTests {
        [Fact]
        public async Task ScopeFlowsAcrossAwaitAndTaskRunAndRestoresNestedScopes() {
            FakeTarget target = new FakeTarget();
            FakeTarget nested = new FakeTarget("nested");
            FakeLogTarget log = new FakeLogTarget();
            using (LiveSession session = new LiveSession(new FakeContainer(target, nested, log), LiveSessionTests.Options(new FakeOutput(false)))) {
                session.Start();
                Assert.False(TerminalScope.IsActive);
                using (session.Scope("work", "item")) {
                    Assert.Equal("work", TerminalScope.Target);
                    Assert.Equal("item", TerminalScope.Key);
                    await Task.Yield();
                    TerminalScope.Report("awaited");
                    await Task.Run(() => {
                        Assert.Equal("item", TerminalScope.Key);
                        TerminalScope.Advance(2);
                        using (TerminalScope.Begin(session, "nested", "inner")) {
                            TerminalScope.SetState("pass", "done");
                            TerminalScope.Log("log", "warn");
                        }
                        Assert.Equal("work", TerminalScope.Target);
                    });
                    Assert.Equal("work", TerminalScope.Target);
                    Assert.Equal("item", TerminalScope.Key);
                }
                Assert.False(TerminalScope.IsActive);
                Assert.Null(TerminalScope.Target);
                TerminalScope.Report("ignored");
                TerminalScope.Advance();
                TerminalScope.SetState("ignored");
                TerminalScope.Log("ignored");
                session.Stop();
                Assert.Equal(new[] { "detail", "advance" }, target.Events.Select(e => e.Operation));
                Assert.All(target.Events, e => Assert.Equal("item", e.Key));
                Assert.Equal(2, target.Events[1].Value);
                LiveEvent state = Assert.Single(nested.Events);
                Assert.Equal("inner", state.Key);
                Assert.Equal("pass", state.Status);
                Assert.Equal("done", state.Text);
                LiveEvent line = Assert.Single(log.Events);
                Assert.Equal("write", line.Operation);
                Assert.Equal("warn", line.Status);
                Assert.Equal("log", line.Text);
            }
        }
    }
}
