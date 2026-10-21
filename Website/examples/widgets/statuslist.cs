using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new StatusList()
    .Add("Restore NuGet packages", StatusKind.Pass, "1.6 s")
    .Add("Compile ConsoleMonster", StatusKind.Pass, "4.7 s")
    .Add("Run Pester on PowerShell 7", StatusKind.Warn, "1 skipped")
    .Add("Sign module files", StatusKind.Skip, "skipped")
    .Add("Pack ConsoleMonster.2.0.0.nupkg", StatusKind.Running)
    .Add("Publish to PowerShell Gallery", StatusKind.Pending));