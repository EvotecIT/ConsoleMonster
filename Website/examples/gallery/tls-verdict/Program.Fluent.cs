// A deterministic TLS chain story. Data and timings are fictional; no hosts are contacted.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

string scenario = args.Length == 0 ? "Healthy" : args[0];
if (scenario != "Healthy" && scenario != "ExpiredIntermediate")
    throw new ArgumentException("Use Healthy or ExpiredIntermediate.");
bool expired = scenario == "ExpiredIntermediate";
StatusKind chainStatus = expired ? StatusKind.Fail : StatusKind.Pass;
string verdict = expired ? "UNTRUSTED" : "TRUSTED";
string hostName = expired ? "legacy.example.test" : "portal.example.test";
// Real data plugs in here: map your TLS checker's connection, date and revocation evidence
// to these records. A successful handshake alone does not establish trust.
Step[] steps =
{
    new("dns", "Resolve DNS", StatusKind.Pass, "203.0.113.42"),
    new("connect", "Connect :443", StatusKind.Pass, "connected"),
    new("handshake", "TLS handshake", StatusKind.Pass, "TLS 1.3"),
    new("chain", "Validate chain", chainStatus, expired ? "expired intermediate" : "verified"),
    new("expiry", "Expiry and revocation", expired ? StatusKind.Skip : StatusKind.Pass, expired ? "skipped" : "valid / good")
};
Pair[] context =
{
    new("Host", hostName),
    new("IP", "203.0.113.42"),
    new("Protocol", "TLS 1.3"),
    new("Cipher", "AES_256_GCM_SHA384"),
    new("Issuer", "Example Issuing CA"),
    new("Valid until", "2027-04-15 (leaf)")
};
FlowNode[] nodes =
{
    new FlowNode("client", "client", StatusKind.Pass),
    new FlowNode("host", "host:443", StatusKind.Pass),
    new FlowNode("leaf", "leaf", StatusKind.Pass),
    new FlowNode("intermediate", "intermediate", chainStatus),
    new FlowNode("root", "root", expired ? StatusKind.Skip : StatusKind.Pass)
};
FlowEdge[] edges =
{
    new FlowEdge("connect", "client", "host", ":443"),
    new FlowEdge("tls", "host", "leaf", "TLS"),
    new FlowEdge("issuer", "leaf", "intermediate", "issuer", chainStatus, expired),
    new FlowEdge("anchor", "intermediate", "root", "anchor")
};
var flow = Ui.Flow("chain");
foreach (FlowNode node in nodes)
    flow.Add(node);
foreach (FlowEdge edge in edges)
    flow.Add(edge);
if (expired)
    flow.Branch(new FlowBranch("intermediate", "expired 2026-09-30", Status: StatusKind.Fail));
var phases = Ui.Phases().StepCounter();
foreach (Step step in steps)
    phases.Set(step.Key, step.Status, step.Label, step.Detail);
var facts = Ui.KeyValue().Columns(2).KeyRole(ThemeToken.Accent);
foreach (Pair pair in context)
    facts.Add(pair.Key, pair.Value);
string proof = expired ? "Intermediate expired before 2026-10-25." : "All three certificates valid on 2026-10-25.";
string action = expired ? "Renew the intermediate; serve the full chain." : "No repair needed; keep expiry monitoring on.";
int exitCode = expired ? 1 : 0;
Ui.Rows()
    .Gap(1)
    .Content(c2 =>
    {
        c2.Markup("[bold accent]TLS verdict[/]  [muted]Follow the chain. Verify the trust.[/]");
        c2.Panel(p => p.Title("Connection ledger").Content(c =>
        {
            c.Add(phases);
        }));
        c2.Add(facts);
        c2.Add(flow);
        c2.Rule("Trust decision");
        c2.Panel(p => p.Content(content =>
        {
            content.Markup(expired ? "Chain validation failed." : "Chain validation passed.");
        }).Title(verdict).Verdict().Status(chainStatus));
        c2.KeyValue(kv => kv.KeyRole(chainStatus.ToToken())
                .Add("Cause", expired ? "Expired intermediate certificate." : "Trusted root and complete chain.")
                .Add("Proof", proof)
                .Add("Action", action)
                .Add("Exit", exitCode + " · 184 ms"));
    })
    .Show();
record Step(string Key, string Label, StatusKind Status, string Detail);
record Pair(string Key, string Value);
