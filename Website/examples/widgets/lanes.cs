using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// One lane per worker: its own bar, counts and current detail.
var lanes = new Lanes("servers");
Lane dc01 = lanes.Add("DC01", "DC01", 40);
dc01.Value = 40;
dc01.Passed = 40;
dc01.Status = StatusKind.Pass;
Lane dc02 = lanes.Add("DC02", "DC02", 40);
dc02.Value = 22;
dc02.Passed = 21;
dc02.Warned = 1;
dc02.Status = StatusKind.Running;
dc02.Detail = "replication";
Lane dc03 = lanes.Add("DC03", "DC03", 40);
dc03.Value = 9;
dc03.Passed = 7;
dc03.Failed = 2;
dc03.Status = StatusKind.Running;
dc03.Detail = "DNS records";
lanes.Add("DC04", "DC04", 40);
Terminal.Write(lanes);
