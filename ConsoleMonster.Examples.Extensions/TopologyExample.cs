using System;
using System.Globalization;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ConsoleMonster.Graphics;
using ConsoleMonster.Live;

namespace ConsoleMonster.Examples.Extensions {
    /// <summary>
    /// AD replication on a ChartForgeX topology: <c>topology</c> plays the "replicate a change" use case with keys,
    /// <c>topology --live [seconds]</c> shows replication lag updating live on the domain controllers.
    /// </summary>
    internal static class TopologyExample {
        public static int Run(string[] args) {
            TopologyDiagram view = new TopologyDiagram(Replication());
            if (args.Length > 0 && args[0] == "--live") {
                int seconds = args.Length > 1 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 10;
                Live(view, seconds);
                return 0;
            }

            view.Run();
            return 0;
        }

        // A live dashboard: the view stays the same, only node values and statuses change.
        private static void Live(TopologyDiagram view, int seconds) {
            view.SelectScenario(null);
            using (LiveSession session = new LiveSession(view, new LiveSessionOptions())) {
                session.Start();
                TopologyDiagramHandle handle = view.Handle(session);
                Random random = new Random(7);
                for (int tick = 0; tick < seconds * 2; tick++) {
                    foreach (string dc in new[] { "dc01", "dc02", "dc03", "dc04" }) {
                        int lag = dc == "dc03" ? 20 + random.Next(40) : random.Next(5);
                        TopologyHealthStatus status = lag > 45 ? TopologyHealthStatus.Critical : lag > 15 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
                        handle.Value(dc, "lag " + lag.ToString(CultureInfo.InvariantCulture) + " min", status);
                    }

                    Thread.Sleep(500);
                }
            }
        }

        private static TopologyChart Replication() {
            TopologyChart chart = TopologyChart.Create()
                .WithTitle("Active Directory replication")
                .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
                .WithViewport(1660, 330, 28)
                .AddGroup("hq", "Site: HQ", 0, 0, 0, 0)
                .AddGroup("warsaw", "Site: Warsaw", 0, 0, 0, 0)
                .AddAutoNode("admin", "Admin", TopologyNodeKind.Person)
                .AddAutoNode("dc01", "DC01", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "hq", "PDC emulator")
                .AddAutoNode("dc02", "DC02", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "hq", "Bridgehead")
                .AddAutoNode("dc03", "DC03", TopologyNodeKind.Server, TopologyHealthStatus.Warning, "warsaw", "Bridgehead")
                .AddAutoNode("dc04", "DC04", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "warsaw", "Branch DC")
                .AddEdge("admin-dc01", "admin", "dc01", "LDAP", direction: VisualLinkDirection.Forward)
                .AddEdge("dc01-dc02", "dc01", "dc02", "RPC", direction: VisualLinkDirection.Forward)
                .AddEdge("dc02-dc03", "dc02", "dc03", "Site link", direction: VisualLinkDirection.Forward)
                .AddEdge("dc03-dc04", "dc03", "dc04", "RPC", direction: VisualLinkDirection.Forward);
            foreach (TopologyEdge edge in chart.Edges) edge.LineStyle = TopologyEdgeLineStyle.Solid;
            chart.AddScenario("replicate", "Replicate a change", scenario => scenario
                .WithSpotlight()
                .AddMessageStep("admin-dc01", TopologyScenarioInteraction.Request, "LDAP modify", configure: step => step.WithPayload("{ \"replace\": { \"title\": \"Site manager\" } }", TopologyScenarioPayloadFormat.Json))
                .AddMessageStep("admin-dc01", TopologyScenarioInteraction.Response, "Success", reverse: true)
                .AddMessageStep("dc01-dc02", TopologyScenarioInteraction.Message, "Change notification")
                .AddMessageStep("dc01-dc02", TopologyScenarioInteraction.Request, "DRSGetNCChanges", reverse: true)
                .AddMessageStep("dc01-dc02", TopologyScenarioInteraction.Response, "1 object")
                .AddMessageStep("dc02-dc03", TopologyScenarioInteraction.Request, "Scheduled pull", reverse: true)
                .AddMessageStep("dc02-dc03", TopologyScenarioInteraction.Response, "Changes")
                .AddMessageStep("dc03-dc04", TopologyScenarioInteraction.Message, "Change notification"));
            return chart;
        }
    }
}
