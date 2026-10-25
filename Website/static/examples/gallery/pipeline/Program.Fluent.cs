// Module build pipeline: phases, sixteen steps, compiler lanes and captured output.
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: all tool messages and outcomes are fictional. No build, signing, publishing or file writes occur.
BuildStep[] steps =
{
    new("Prepare", "config", "Read Build/project.build.json", 0.4, StatusKind.Pass, "Loaded project.build.json (3 projects, version 2.0.X)", "Resolved version 2.0.0 from PowerShell Gallery (current 0.0.4)"),
    new("Prepare", "manifest", "Update ConsoleMonster.psd1", 0.5, StatusKind.Pass, "ModuleVersion 0.0.4 -> 2.0.0", "CmdletsToExport: 38 binary cmdlets"),
    new("Restore", "restore", "Restore NuGet packages", 1.6, StatusKind.Pass, "Restored src\\Orion\\Orion.csproj (in 412 ms)", "Restored src\\Orion.Shell\\Orion.Shell.csproj (in 380 ms)"),
    new("Build", "core-ns", "Compile ConsoleMonster (netstandard2.0)", 1.8, StatusKind.Pass, "ConsoleMonster -> bin\\Release\\netstandard2.0\\ConsoleMonster.dll"),
    new("Build", "core-fx", "Compile ConsoleMonster (net472)", 1.5, StatusKind.Pass, "ConsoleMonster -> bin\\Release\\net472\\ConsoleMonster.dll"),
    new("Build", "core-net", "Compile ConsoleMonster (net8.0)", 1.4, StatusKind.Pass, "ConsoleMonster -> bin\\Release\\net8.0\\ConsoleMonster.dll"),
    new("Build", "ps", "Compile ConsoleMonster.PowerShell", 1.9, StatusKind.Pass, "ConsoleMonster.PowerShell -> bin\\Release\\net472\\ConsoleMonster.PowerShell.dll", "ConsoleMonster.PowerShell -> bin\\Release\\net8.0\\ConsoleMonster.PowerShell.dll"),
    new("Test", "xunit", "Run xUnit tests (net472, net8.0)", 3.2, StatusKind.Pass, "Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net472)", "Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net8.0)"),
    new("Test", "compatibility", "Run compatibility tests", 2.4, StatusKind.Pass, "Tests completed in 2.1s", "Tests Passed: 41, Failed: 0, Skipped: 0"),
    new("Test", "pester7", "Run Pester tests (PowerShell 7.5)", 2.1, StatusKind.Warn, "Tests completed in 1.8s", "Tests Passed: 40, Failed: 0, Skipped: 1 (requires conhost)"),
    new("Docs", "md", "Generate markdown help (38 commands)", 1.7, StatusKind.Pass, "Docs\\Readme.md updated", "Docs\\Show-ConsoleTable.md ... Docs\\Invoke-ConsoleDashboard.md"),
    new("Docs", "maml", "Generate en-US MAML help", 0.9, StatusKind.Pass, "en-US\\ConsoleMonster.PowerShell.dll-Help.xml (412 KB)"),
    new("Sign", "sign", "Sign module files", 0.3, StatusKind.Skip, "SignModule is $false for this build; skipping Authenticode signing"),
    new("Package", "nupkg", "Pack ConsoleMonster.2.0.0.nupkg", 1.2, StatusKind.Pass, "Successfully created package 'Artefacts\\ProjectBuild\\ConsoleMonster.2.0.0.nupkg'."),
    new("Package", "zip", "Create module archive", 0.8, StatusKind.Pass, "Artefacts\\Packed\\ConsoleMonster-v2.0.0.zip (1.1 MB)"),
    new("Publish", "publish", "Publish to PowerShell Gallery", 0.3, StatusKind.Skip, "RunMode is Build; publishing is disabled"),
};
string[] phaseNames =
{
    "Prepare",
    "Restore",
    "Build",
    "Test",
    "Docs",
    "Sign",
    "Package",
    "Publish"
};
string[] compilerNames =
{
    "netstandard2.0",
    "net472",
    "net8.0",
    "PowerShell"
};
int completed = 0;
string PathText(string text) => text.Replace('\\', '/');
string Duration(double seconds) => seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
StatusKind Worst(IEnumerable<BuildStep> items) => items.Any(item => item.Result == StatusKind.Warn) ? StatusKind.Warn : items.All(item => item.Result == StatusKind.Skip) ? StatusKind.Skip : StatusKind.Pass;
// Layout: a phase strip and detailed checklist beside module facts, overall progress and compiler lanes.
Phases phases = new Phases();
foreach (string phase in phaseNames)
{
    BuildStep[] group = steps.Where(item => item.Phase == phase).ToArray();
    int first = Array.IndexOf(steps, group[0]);
    int last = first + group.Length;
    StatusKind state = last <= completed ? Worst(group) : first <= completed && completed < steps.Length ? StatusKind.Running : StatusKind.Pending;
    phases.Set(phase, state, detail: last <= completed ? Duration(group.Sum(item => item.Seconds)) : null);
}

StatusList status = new StatusList("steps");
for (int i = 0; i < steps.Length; i++)
    status.Set(steps[i].Key, PathText(steps[i].Label), i < completed ? steps[i].Result : i == completed ? StatusKind.Running : StatusKind.Pending, i < completed ? Duration(steps[i].Seconds) : null);
KeyValueList module = new KeyValueList("module").Add("Module", "[bold]ConsoleMonster[/]").Add("Version", "0.0.4 [muted]→[/] [bold]2.0.0[/]").Add("Frameworks", "netstandard2.0 · net472 · net8.0").Add("PowerShell", "5.1 Desktop · 7.x Core").Add("Run mode", "Build").Add("Elapsed", completed == 0 ? "-" : Duration(steps.Take(completed).Sum(item => item.Seconds)));
ProgressBar progress = new ProgressBar("Overall", "overall").Total(steps.Length).Value(completed).LabelWidth(8);
Lanes compilers = new Lanes("compilers");
for (int i = 0; i < compilerNames.Length; i++)
{
    Lane lane = compilers.Add("c" + i, compilerNames[i], 1);
    lane.Status = completed > i + 3 ? StatusKind.Pass : completed == i + 3 ? StatusKind.Running : StatusKind.Pending;
    lane.Value = completed > i + 3 ? 1 : 0;
    lane.Passed = (int)lane.Value;
    lane.Detail = lane.Value > 0 ? "compiled" : lane.Status == StatusKind.Running ? "compiling" : "queued";
}

LogRegion log = new LogRegion().PreferredHeight(8).ShowElapsed(false);
log.Write("Build engine · release pipeline");
foreach (BuildStep item in steps.Take(completed))
    foreach (string line in item.Output)
        log.Write(PathText(line), item.Result == StatusKind.Warn ? "warn" : item.Result == StatusKind.Skip ? "skip" : "info");
if (completed < steps.Length)
    log.Write(PathText(steps[completed].Label) + "...");
Rows body = new Rows(new Padding().Bottom(1).Right(1).Top(0).Left(1).Content(c => c.Add(phases)), new Columns().Add(new Panel().Title("Steps").Content(c => c.Add(status)), LayoutSize.Fraction(1.5)).Add(new Panel().Title("Module").Content(c => c.Add(new Rows(module, new Rule(), progress, new Rule("Compiler workers"), compilers))))).Add(new Panel().Title("Output").Content(c => c.Add(log)), LayoutSize.Fraction());
Dashboard dashboard = new Dashboard().Content(c => c.Add(body)).Title("[bold]Module build[/]").Subtitle("Build-Module ConsoleMonster").ShowElapsed(false).Footer("[muted]Prepare · Restore · Build · Test · Docs · Sign · Package · Publish    Ctrl+C stop[/]");
// Behaviour: native handles update each region while ordinary tool output is captured by the live session.
using LiveSession session = new LiveSession(dashboard);
try
{
    session.Run(live =>
    {
        var phaseHandle = phases.Handle(live);
        var stepHandle = status.Handle(live);
        var moduleHandle = module.Handle(live);
        var progressHandle = progress.Handle(live);
        var compilerHandle = compilers.Handle(live);
        double elapsed = 0;
        for (int i = 0; i < steps.Length; i++)
        {
            if (live.Cancellation.IsCancellationRequested)
                break;
            BuildStep item = steps[i];
            phaseHandle.Start(item.Phase);
            stepHandle.Set(item.Key, StatusKind.Running);
            progressHandle.Label(item.Phase);
            if (i >= 3 && i <= 6)
                compilerHandle.Start("c" + (i - 3), "compiling");
            Console.WriteLine(PathText(item.Label) + "...");
            live.Delay(TimeSpan.FromMilliseconds(item.Seconds * 400));
            foreach (string line in item.Output)
            {
                if (item.Result == StatusKind.Warn)
                    log.Handle(live).Warn(PathText(line));
                else
                    Console.WriteLine(PathText(line));
            }

            elapsed += item.Seconds;
            stepHandle.Set(item.Key, item.Result);
            stepHandle.Detail(item.Key, Duration(item.Seconds));
            moduleHandle.Set("Elapsed", Duration(elapsed));
            progressHandle.Set(i + 1);
            if (i >= 3 && i <= 6)
            {
                compilerHandle.Advance("c" + (i - 3), StatusKind.Pass, "compiled");
                compilerHandle.Complete("c" + (i - 3), StatusKind.Pass);
            }

            if (i == steps.Length - 1 || steps[i + 1].Phase != item.Phase)
            {
                var group = steps.Where(value => value.Phase == item.Phase);
                live.Post(new LiveEvent("phases", "state", item.Phase, Duration(group.Sum(value => value.Seconds)), Worst(group).ToString()));
            }
        }
    });
}
catch (OperationCanceledException cancelled)when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}

record BuildStep(string Phase, string Key, string Label, double Seconds, StatusKind Result, params string[] Output);
