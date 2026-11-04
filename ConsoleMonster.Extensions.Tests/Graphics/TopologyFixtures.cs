using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    /// <summary>The two consumer stories: AD replication of one change, and a TierBridge file journey.</summary>
    internal static class TopologyFixtures {
        public static TopologyChart AdReplication() {
            TopologyChart chart = TopologyChart.Create()
                .WithId("ad-replication")
                .WithTitle("Active Directory replication")
                .WithSubtitle("How one attribute change reaches every domain controller")
                .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
                .WithViewport(1660, 330, 28)
                .AddGroup("hq", "Site: HQ", 0, 0, 0, 0)
                .AddGroup("warsaw", "Site: Warsaw", 0, 0, 0, 0)
                .AddAutoNode("admin", "Admin", TopologyNodeKind.Person, subtitle: "ad.evotec.xyz")
                .AddAutoNode("adminpc", "ADMIN-PC", TopologyNodeKind.Endpoint, subtitle: "RSAT tools")
                .AddAutoNode("dc01", "DC01", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "hq", "PDC emulator")
                .AddAutoNode("dc02", "DC02", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "hq", "Bridgehead")
                .AddAutoNode("dc03", "DC03", TopologyNodeKind.Server, TopologyHealthStatus.Warning, "warsaw", "Bridgehead")
                .AddAutoNode("dc04", "DC04", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "warsaw", "Branch DC")
                .AddEdge("admin-adminpc", "admin", "adminpc", "signs in", direction: VisualLinkDirection.Forward)
                .AddEdge("adminpc-dc01", "adminpc", "dc01", "LDAP", direction: VisualLinkDirection.Forward)
                .AddEdge("dc01-dc02", "dc01", "dc02", "RPC", direction: VisualLinkDirection.Forward)
                .AddEdge("dc02-dc03", "dc02", "dc03", "Site link", direction: VisualLinkDirection.Forward)
                .AddEdge("dc03-dc04", "dc03", "dc04", "RPC", direction: VisualLinkDirection.Forward);
            foreach (TopologyEdge edge in chart.Edges) edge.LineStyle = TopologyEdgeLineStyle.Solid;
            chart.Nodes[2].Metrics["USN"] = "48211";
            chart.AddScenario("replicate", "Replicate a change", scenario => scenario
                .WithDescription("A title change written on DC01 reaches DC04")
                .WithSpotlight()
                .AddMessageStep("admin-adminpc", TopologyScenarioInteraction.Request, "Set-ADUser -Title")
                .AddMessageStep("adminpc-dc01", TopologyScenarioInteraction.Request, "LDAP modify", configure: step => step.WithPayload(
                    "{\n  \"dn\": \"CN=Ana Nowak,OU=Users,DC=ad,DC=evotec,DC=xyz\",\n  \"replace\": { \"title\": \"Site manager\" }\n}", TopologyScenarioPayloadFormat.Json))
                .AddMessageStep("adminpc-dc01", TopologyScenarioInteraction.Response, "Success", reverse: true)
                .AddNodeStep("dc01", "Commit locally", configure: step => step.WithPayload("USN 48211, originating write"))
                .AddMessageStep("dc01-dc02", TopologyScenarioInteraction.Message, "Change notification after 15 s")
                .AddMessageStep("dc01-dc02", TopologyScenarioInteraction.Request, "DRSGetNCChanges", reverse: true, configure: step => step.WithPayload("{ \"usnFrom\": 48190 }", TopologyScenarioPayloadFormat.Json))
                .AddMessageStep("dc01-dc02", TopologyScenarioInteraction.Response, "1 object, 1 attribute")
                .AddMessageStep("dc02-dc03", TopologyScenarioInteraction.Request, "Scheduled pull", reverse: true)
                .AddMessageStep("dc02-dc03", TopologyScenarioInteraction.Response, "Changes, compressed"))
                .AddScenario("link-down", "Site link down", scenario => scenario
                    .WithSpotlight()
                    .AddMessageStep("dc02-dc03", TopologyScenarioInteraction.Failure, "Scheduled pull", reverse: true, configure: step => step.WithPayload("RPC server is unavailable (1722)"))
                    .AddNodeStep("dc03", "Retries at the next schedule"));
            return chart;
        }

        public static TopologyChart TierBridge() {
            TopologyChart chart = TopologyChart.Create()
                .WithId("tierbridge")
                .WithTitle("TierBridge file journey")
                .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
                .WithViewport(1050, 610, 28)
                .AddGroup("source", "Source zone (Tier 2)", 0, 0, 0, 0)
                .AddGroup("bridge", "TierBridge", 0, 0, 0, 0)
                .AddGroup("target", "Target zone (Tier 0)", 0, 0, 0, 0)
                .AddAutoNode("requester", "Requester", TopologyNodeKind.Person, subtitle: "Tier 2 admin")
                .AddAutoNode("inbox", "Drop share", TopologyNodeKind.Storage, groupId: "source", subtitle: @"\\fs02\drop$")
                .AddAutoNode("watcher", "Watcher", TopologyNodeKind.Service, groupId: "bridge")
                .AddAutoNode("inspection", "Inspection", TopologyNodeKind.Process, groupId: "bridge", subtitle: "AV, type, hash")
                .AddAutoNode("quarantine", "Quarantine", TopologyNodeKind.Storage, TopologyHealthStatus.Warning, "bridge")
                .AddAutoNode("approval", "Approval", TopologyNodeKind.Service, groupId: "bridge")
                .AddAutoNode("approver", "Approver", TopologyNodeKind.Person, subtitle: "Tier 0 owner")
                .AddAutoNode("destination", "Target share", TopologyNodeKind.Storage, groupId: "target")
                .AddEdge("requester-inbox", "requester", "inbox", "copy", direction: VisualLinkDirection.Forward)
                .AddEdge("inbox-watcher", "inbox", "watcher", "event", direction: VisualLinkDirection.Forward)
                .AddEdge("watcher-inspection", "watcher", "inspection", "inspect", direction: VisualLinkDirection.Forward)
                .AddEdge("watcher-quarantine", "watcher", "quarantine", "hold", direction: VisualLinkDirection.Forward)
                .AddEdge("quarantine-approval", "quarantine", "approval", "request", direction: VisualLinkDirection.Forward)
                .AddEdge("approval-approver", "approval", "approver", "ask", direction: VisualLinkDirection.Forward)
                .AddEdge("approval-destination", "approval", "destination", "release", direction: VisualLinkDirection.Forward);
            chart.Groups[1].DashPattern.Add(6);
            chart.Groups[1].DashPattern.Add(4);
            foreach (TopologyEdge edge in chart.Edges) edge.LineStyle = TopologyEdgeLineStyle.Solid;
            chart.AddScenario("approved", "Approved file", scenario => scenario
                .WithSpotlight()
                .AddMessageStep("requester-inbox", TopologyScenarioInteraction.Request, "Copy setup.msi")
                .AddMessageStep("inbox-watcher", TopologyScenarioInteraction.Message, "File created")
                .AddMessageStep("watcher-inspection", TopologyScenarioInteraction.Request, "Inspect", configure: step => step.WithPayload("{ \"file\": \"setup.msi\", \"sha256\": \"9f2c41d7\" }", TopologyScenarioPayloadFormat.Json))
                .AddMessageStep("watcher-inspection", TopologyScenarioInteraction.Response, "Needs approval", reverse: true)
                .AddMessageStep("watcher-quarantine", TopologyScenarioInteraction.Request, "Hold")
                .AddMessageStep("quarantine-approval", TopologyScenarioInteraction.Message, "Request approval")
                .AddMessageStep("approval-approver", TopologyScenarioInteraction.Message, "Ask the owner")
                .AddMessageStep("approval-approver", TopologyScenarioInteraction.Request, "Approve", reverse: true)
                .AddMessageStep("approval-destination", TopologyScenarioInteraction.Request, "Release", configure: step => step.WithPayload("{ \"ticket\": \"TB-1042\", \"released\": true }", TopologyScenarioPayloadFormat.Json)));
            return chart;
        }
    }
}
