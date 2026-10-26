using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Rows().Add(new Spinner("Resolving DNS records") { Detail = "zone 4 of 9" })
        .Add(new Spinner("Schema snapshot") { Status = StatusKind.Pass, Detail = "cached" })
        .Add(new Spinner("Certificate chain") { Status = StatusKind.Fail, Detail = "expired 3 days ago" }));
