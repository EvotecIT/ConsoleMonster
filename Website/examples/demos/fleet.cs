using ConsoleMonster.Examples.Showcases;

// The fleet health check, a whole console application: a Shell menu with presets, a form whose servers load for the
// chosen domain, the review with the equivalent command, a live dashboard and the results with next steps. Its two
// other files, FleetHealth.cs and FleetData.cs, go next to this Program.cs.
//   dotnet run              the app
//   dotnet run -- --demo    the app pressing every key itself at a viewer's pace, with a fixed clock and outcomes
if (args.Contains("--demo")) {
    FleetHealth.Demo();
} else {
    FleetHealth.Show();
}