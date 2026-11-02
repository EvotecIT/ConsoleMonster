using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Steps joined by labelled connections; a cut connection shows where the path breaks.
var flow = new Flow()
    .Node("client", "Client", StatusKind.Pass)
    .Node("proxy", "Proxy", StatusKind.Pass)
    .Node("api", "API", StatusKind.Warn)
    .Node("db", "Database", StatusKind.Fail)
    .Edge("client", "proxy", ":443")
    .Edge("proxy", "api", "HTTP")
    .Edge("api", "db", "timeout", StatusKind.Fail, cut: true);
flow.Branch = new FlowBranch("db", "listener stopped", default, StatusKind.Fail);
Terminal.Write(flow);
