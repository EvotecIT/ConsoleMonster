using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class DiagramLiveTests {
        [Fact]
        public void FluentSession_DiscoversTheDiagramForAddressedUpdates() {
            DiagramBuilder builder = Ui.Diagram("Monitor").Node("VPN", node => node.Subtitle("vpn.contoso.com"));
            using var session = builder.CreateSession(LiveSessionTests.Options(new FakeOutput(false)));
            builder.Build.Handle(session).Value("VPN", "180 ms", StatusKind.Warn);
            session.Start(); session.Stop();
            Assert.Equal(0, session.ApplyErrors);
            Assert.Equal(0, session.RenderErrors);
            Assert.Equal("180 ms", builder.Build.Values["VPN"]);
            Assert.Equal(StatusKind.Warn, builder.Build.Nodes.Single().Status);
        }

        [Fact]
        public void FirstRenderAndLiveValidation_CreateEachImplicitEndpointOnce() {
            for (int attempt = 0; attempt < 16; attempt++) {
                var diagram = new Diagram();
                for (int index = 0; index < 32; index++) diagram.Links.Add(new DiagramLink("Source " + index, "Target " + index));
                using var session = diagram.CreateSession(LiveSessionTests.Options(new FakeOutput(false)));
                using var ready = new Barrier(2);
                Parallel.Invoke(
                    () => { ready.SignalAndWait(); Terminal.RenderText(diagram, TerminalProfile.Fixed(60, 100)); },
                    () => { ready.SignalAndWait(); diagram.Handle(session).Value("Target 31", "ready", StatusKind.Pass); });
                session.Start(); session.Stop();
                Assert.Equal(64, diagram.Nodes.Count);
                Assert.Equal(64, diagram.Nodes.Select(node => node.Id).Distinct().Count());
                Assert.Equal(0, session.ApplyErrors);
                Assert.Equal(0, session.RenderErrors);
                Assert.Equal("ready", diagram.Values["Target 31"]);
            }
        }
    }
}
