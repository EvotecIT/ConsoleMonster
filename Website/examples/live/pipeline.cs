using ConsoleMonster.Building;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// A simulated module build as a live dashboard: phases, steps with timings, module facts, a progress bar and the tool's own
// output. The build writes to Console.Out like any tool; the session captures those lines into the Output panel.
Step[] plan =
{
    new("Prepare", "config", "Read Build/project.build.json", 0.4, StatusKind.Pass,
        "Loaded project.build.json (3 projects, version 2.0.X)",
        "Resolved version 2.0.0 from PowerShell Gallery (current 0.0.4)"),
    // <more>
    new("Prepare", "manifest", "Update ConsoleMonster.psd1", 0.5, StatusKind.Pass,
        "ModuleVersion 0.0.4 -> 2.0.0", "CmdletsToExport: 38 binary cmdlets"),
    new("Restore", "restore", "Restore NuGet packages", 1.6, StatusKind.Pass,
        "Restored ConsoleMonster.csproj (in 412 ms)",
        "Restored ConsoleMonster.PowerShell.csproj (in 380 ms)"),
    new("Build", "core-ns", "Compile ConsoleMonster (netstandard2.0)", 1.8, StatusKind.Pass,
        "ConsoleMonster -> bin\\Release\\netstandard2.0\\ConsoleMonster.dll"),
    new("Build", "core-fx", "Compile ConsoleMonster (net472)", 1.5, StatusKind.Pass,
        "ConsoleMonster -> bin\\Release\\net472\\ConsoleMonster.dll"),
    new("Build", "core-net", "Compile ConsoleMonster (net8.0)", 1.4, StatusKind.Pass,
        "ConsoleMonster -> bin\\Release\\net8.0\\ConsoleMonster.dll"),
    new("Build", "ps", "Compile ConsoleMonster.PowerShell", 1.9, StatusKind.Pass,
        "ConsoleMonster.PowerShell -> bin\\Release\\net472\\ConsoleMonster.PowerShell.dll",
        "ConsoleMonster.PowerShell -> bin\\Release\\net8.0\\ConsoleMonster.PowerShell.dll"),
    new("Test", "xunit", "Run xUnit tests (net472, net8.0)", 3.2, StatusKind.Pass,
        "Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net472)",
        "Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net8.0)"),
    new("Test", "pester5", "Run Pester tests (Windows PowerShell 5.1)", 2.4, StatusKind.Pass,
        "Tests completed in 2.1s", "Tests Passed: 41, Failed: 0, Skipped: 0"),
    new("Test", "pester7", "Run Pester tests (PowerShell 7.5)", 2.1, StatusKind.Warn,
        "Tests completed in 1.8s", "Tests Passed: 40, Failed: 0, Skipped: 1 (requires conhost)"),
    new("Docs", "md", "Generate markdown help (38 commands)", 1.7, StatusKind.Pass,
        "Docs\\Readme.md updated",
        "Docs\\Show-ConsoleTable.md ... Docs\\Invoke-ConsoleDashboard.md"),
    new("Docs", "maml", "Generate en-US MAML help", 0.9, StatusKind.Pass,
        "en-US\\ConsoleMonster.PowerShell.dll-Help.xml (412 KB)"),
    new("Sign", "sign", "Sign module files", 0.3, StatusKind.Skip,
        "SignModule is $false for this build; skipping Authenticode signing"),
    new("Package", "nupkg", "Pack ConsoleMonster.2.0.0.nupkg", 1.2, StatusKind.Pass,
        "Successfully created package 'Artefacts\\ProjectBuild\\ConsoleMonster.2.0.0.nupkg'."),
    new("Package", "zip", "Create module archive", 0.8, StatusKind.Pass,
        "Artefacts\\Packed\\ConsoleMonster-v2.0.0.zip (1.1 MB)"),
    new("Publish", "publish", "Publish to PowerShell Gallery", 0.3, StatusKind.Skip,
        "RunMode is Build; publishing is disabled")
// </more>
};
Phases phases = new Phases();
foreach (string phase in new[]
{
    "Prepare", "Restore", "Build", "Test", "Docs", "Sign", "Package", "Publish"
}

)
{
    phases.Set(phase);
}

StatusList steps = new StatusList("steps");
foreach (Step step in plan)
{
    steps.Set(step.Key, step.Label, StatusKind.Pending);
}

KeyValueList module = new KeyValueList("module")
    .Add("Module", "[bold]ConsoleMonster[/]")
    .Add("Version", "0.0.4 [muted]→[/] [bold]2.0.0[/]")
    .Add("Frameworks", "netstandard2.0 · net472 · net8.0")
    .Add("PowerShell", "5.1 Desktop · 7.x Core")
    .Add("Run mode", "Build [muted](publish disabled)[/]")
    .Add("Elapsed", "-");
ProgressBar progress = new ProgressBar("Overall", "overall").Total(plan.Length).LabelWidth(8);
Dashboard dashboard = new DashboardBuilder("[bold]Module build[/]")
    .Subtitle("Build-Module ConsoleMonster")
    .Add(new Padding(phases, 1, 0, 1, 1))
    .Columns(c => c.Panel("Steps", steps, 1.5)
        .Panel("Module", new Rows(module, new Rule(), progress), 1))
    .Panel("Output", new LogRegion { PreferredHeight = 8 }, true)
    .BuildDashboard();
Table summary = new Table()
    .Border(TableBorder.Minimal)
    .Title("[bold]Build summary[/] [muted]ConsoleMonster 2.0.0[/]")
    .AddColumn("Phase")
    .AddColumn("Step", c => c.Flexible())
    .AddColumn("Result")
    .AddColumn("Duration");

// Planned step durations drive the displayed clocks; session delays keep normal playback paced in real time.
SimulatedClock clock = new SimulatedClock();
using (LiveSession session = new LiveSession(
    dashboard,
    new LiveSessionOptions { Clock = clock.Read, Summary = new Padding(summary, 1, 1, 1, 1) }))
{
    session.Run(Build);
}

// <more>
void Build(LiveSession session)
{
    PhasesHandle phasesHandle = phases.Handle(session);
    StatusListHandle stepsHandle = steps.Handle(session);
    KeyValueHandle moduleHandle = module.Handle(session);
    ProgressHandle progressHandle = progress.Handle(session);
    string? currentPhase = null;
    List<StatusKind> phaseResults = new List<StatusKind>();
    session.Post(new LiveEvent("dashboard", "status", default, "building", "running"));
    Console.WriteLine("Build engine 3.0.168 · release pipeline");
    int index = 0;
    foreach (Step step in plan)
    {
        if (session.Cancellation.IsCancellationRequested)
        {
            break;
        }

        if (step.Phase != currentPhase)
        {
            if (currentPhase != null)
            {
                phasesHandle.Complete(currentPhase, Worst(phaseResults));
            }

            currentPhase = step.Phase;
            phaseResults.Clear();
            phasesHandle.Start(currentPhase);
        }

        stepsHandle.Set(step.Key, StatusKind.Running);
        progressHandle.Label(step.Phase);
        TimeSpan stepStart = clock.Elapsed;
        TimeSpan stepDuration = TimeSpan.FromSeconds(step.Seconds);
        int slices = Math.Max(1, (int)(step.Seconds * 10));
        for (int i = 0; i < slices && !session.Cancellation.IsCancellationRequested; i++)
        {
            TimeSpan next = stepStart + TimeSpan.FromTicks(stepDuration.Ticks * (i + 1) / slices);
            TimeSpan slice = next - clock.Elapsed;
            session.Delay(slice);
            clock.Advance(slice);
            if (i < step.Output.Length)
            {
                Console.WriteLine(step.Output[i]);
            }

            if (i % 5 == 0)
            {
                moduleHandle.Set("Elapsed", NumberFormat.Duration(clock.Elapsed));
            }
        }

        for (int i = slices; i < step.Output.Length; i++)
        {
            Console.WriteLine(step.Output[i]);
        }

        if (step.Result == StatusKind.Warn)
        {
            Console.Error.WriteLine(
                "WARNING: 1 Pester test skipped on PowerShell 7 (requires conhost).");
        }

        string duration = NumberFormat.Duration(clock.Elapsed - stepStart);
        stepsHandle.Set(step.Key, step.Result);
        stepsHandle.Detail(step.Key, step.Result == StatusKind.Skip ? "skipped" : duration);
        phaseResults.Add(step.Result);
        summary.AddRow(step.Phase, Markup.Escape(step.Label), TableCell.ForStatus(step.Result),
            step.Result == StatusKind.Skip ? "-" : duration);
        index++;
        progressHandle.Set(index);
    }

    if (currentPhase != null)
    {
        phasesHandle.Complete(currentPhase, Worst(phaseResults));
    }

    moduleHandle.Set("Elapsed", NumberFormat.Duration(clock.Elapsed));
    progressHandle.Complete(StatusKind.Pass);
    Console.WriteLine("Build completed: 2 artefacts in Artefacts\\ (nupkg, zip)");
    session.Post(new LiveEvent("dashboard", "status", default, "build succeeded with 1 warning",
        "warn"));
    session.Delay(TimeSpan.FromMilliseconds(1500));
}

static StatusKind Worst(List<StatusKind> results) => results.Contains(StatusKind.Fail)
    ? StatusKind.Fail
    : results.Contains(StatusKind.Warn)
        ? StatusKind.Warn
        : results.TrueForAll(r => r == StatusKind.Skip) ? StatusKind.Skip : StatusKind.Pass;
// </more>
record Step(string Phase, string Key, string Label, double Seconds, StatusKind Result,
    params string[] Output);
