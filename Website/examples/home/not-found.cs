using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Tree routes = new Tree("[bold]consolemonster.dev[/]");
routes.Add("/widgets/", StatusKind.Pass, "every widget, every tier");
routes.Add("/tables/", StatusKind.Pass, "width negotiation");
routes.Add("/live/", StatusKind.Pass, "recorded dashboards");
routes.Add("/powershell/", StatusKind.Pass, "cmdlets on 5.1 and 7");
routes.Add("/docs/", StatusKind.Pass, "install and configure");
routes.Add("this address", StatusKind.Fail, "no page here");
Terminal.Write(routes);
