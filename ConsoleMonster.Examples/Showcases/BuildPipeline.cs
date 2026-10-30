using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Showcase 2: a module build pipeline: phases, steps with timings, module facts, a
    /// progress bar and the tool output. The simulated build writes to <see cref="Console.Out"/> like a real tool; the live
    /// session captures those lines into the log region instead of letting them scribble over the screen.
    /// </summary>
    public static class BuildPipeline {
        private sealed class Step {
            public Step(string phase, string key, string label, double seconds, StatusKind result, params string[] output) {
                Phase = phase;
                Key = key;
                Label = label;
                Seconds = seconds;
                Result = result;
                Output = output;
            }

            public string Phase { get; }

            public string Key { get; }

            public string Label { get; }

            public double Seconds { get; }

            public StatusKind Result { get; }

            public string[] Output { get; }
        }

        private static readonly Step[] Steps = {
            new Step("Prepare", "config", "Read Build/project.build.json", 0.4, StatusKind.Pass, "Loaded project.build.json (3 projects, version 2.0.X)", "Resolved version 2.0.0 from PowerShell Gallery (current 0.0.4)"),
            new Step("Prepare", "manifest", "Update ConsoleMonster.psd1", 0.5, StatusKind.Pass, "ModuleVersion 0.0.4 -> 2.0.0", "CmdletsToExport: 38 binary cmdlets"),
            new Step("Restore", "restore", "Restore NuGet packages", 1.6, StatusKind.Pass, "Restored Sources\\ConsoleMonster\\ConsoleMonster.csproj (in 412 ms)", "Restored Sources\\ConsoleMonster.PowerShell\\ConsoleMonster.PowerShell.csproj (in 380 ms)"),
            new Step("Build", "core-ns", "Compile ConsoleMonster (netstandard2.0)", 1.8, StatusKind.Pass, "ConsoleMonster -> bin\\Release\\netstandard2.0\\ConsoleMonster.dll"),
            new Step("Build", "core-fx", "Compile ConsoleMonster (net472)", 1.5, StatusKind.Pass, "ConsoleMonster -> bin\\Release\\net472\\ConsoleMonster.dll"),
            new Step("Build", "core-net", "Compile ConsoleMonster (net8.0)", 1.4, StatusKind.Pass, "ConsoleMonster -> bin\\Release\\net8.0\\ConsoleMonster.dll"),
            new Step("Build", "ps", "Compile ConsoleMonster.PowerShell", 1.9, StatusKind.Pass, "ConsoleMonster.PowerShell -> bin\\Release\\net472\\ConsoleMonster.PowerShell.dll", "ConsoleMonster.PowerShell -> bin\\Release\\net8.0\\ConsoleMonster.PowerShell.dll"),
            new Step("Test", "xunit", "Run xUnit tests (net472, net8.0)", 3.2, StatusKind.Pass, "Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net472)", "Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net8.0)"),
            new Step("Test", "pester5", "Run Pester tests (Windows PowerShell 5.1)", 2.4, StatusKind.Pass, "Tests completed in 2.1s", "Tests Passed: 41, Failed: 0, Skipped: 0"),
            new Step("Test", "pester7", "Run Pester tests (PowerShell 7.5)", 2.1, StatusKind.Warn, "Tests completed in 1.8s", "Tests Passed: 40, Failed: 0, Skipped: 1 (requires conhost)"),
            new Step("Docs", "md", "Generate markdown help (38 commands)", 1.7, StatusKind.Pass, "Docs\\Readme.md updated", "Docs\\Show-ConsoleTable.md ... Docs\\Invoke-ConsoleDashboard.md"),
            new Step("Docs", "maml", "Generate en-US MAML help", 0.9, StatusKind.Pass, "en-US\\ConsoleMonster.PowerShell.dll-Help.xml (412 KB)"),
            new Step("Sign", "sign", "Sign module files", 0.3, StatusKind.Skip, "SignModule is $false for this build; skipping Authenticode signing"),
            new Step("Package", "nupkg", "Pack ConsoleMonster.2.0.0.nupkg", 1.2, StatusKind.Pass, "Successfully created package 'Artefacts\\ProjectBuild\\ConsoleMonster.2.0.0.nupkg'."),
            new Step("Package", "zip", "Create module archive", 0.8, StatusKind.Pass, "Artefacts\\Packed\\ConsoleMonster-v2.0.0.zip (1.1 MB)"),
            new Step("Publish", "publish", "Publish to PowerShell Gallery", 0.3, StatusKind.Skip, "RunMode is Build; publishing is disabled")
        };

        /// <summary>Runs the pipeline live.</summary>
        public static void Run() {
            Phases phases = new Phases();
            StatusList steps = new StatusList("steps");
            foreach (string phase in new[] { "Prepare", "Restore", "Build", "Test", "Docs", "Sign", "Package", "Publish" }) {
                phases.Set(phase);
            }
            foreach (Step step in Steps) {
                steps.Set(step.Key, step.Label, StatusKind.Pending);
            }
            KeyValueList module = new KeyValueList("module")
                .Add("Module", "[bold]ConsoleMonster[/]")
                .Add("Version", "0.0.4 [muted]→[/] [bold]2.0.0[/]")
                .Add("Frameworks", "netstandard2.0 · net472 · net8.0")
                .Add("PowerShell", "5.1 Desktop · 7.x Core")
                .Add("Run mode", "Build [muted](publish disabled)[/]")
                .Add("Elapsed", "-");
            ProgressBar progress = new ProgressBar("Overall", "overall") { Total = Steps.Length, LabelWidth = 8 };

            Dashboard dashboard = Ui.Dashboard().Title("[bold]Module build[/]")
                .Subtitle("Build-Module ConsoleMonster")
                .Content(body => body
                    .Add(new Padding(phases, 1, 0, 1, 1))
                    .Columns(columns => columns.Content(c => c
                        .Add(new Panel(steps, "Steps").Fill(1.5))
                        .Add(new Panel(new Rows(module, new Rule(), progress), "Module").Fill(1))))
                    .Add(new Panel(new LogRegion { PreferredHeight = 8 }, "Output").Fill()));

            Table summary = new Table { Border = TableBorder.Minimal, Title = "[bold]Build summary[/] [muted]ConsoleMonster 2.0.0[/]" };
            summary.AddColumn("Phase");
            summary.AddColumn(new TableColumn("Step") { Flexible = true });
            summary.AddColumn("Result");
            summary.AddColumn("Duration");
            LiveSessionOptions options = new LiveSessionOptions { Summary = new Padding(summary, 1, 1, 1, 1) };

            using (LiveSession session = new LiveSession(dashboard, options)) {
                session.Run(s => Execute(s, phases, steps, module, progress, summary));
            }
        }

        private static void Execute(LiveSession session, Phases phasesWidget, StatusList stepsWidget, KeyValueList moduleWidget, ProgressBar progressWidget, Table summary) {
            PhasesHandle phases = phasesWidget.Handle(session);
            StatusListHandle steps = stepsWidget.Handle(session);
            KeyValueHandle module = moduleWidget.Handle(session);
            ProgressHandle progress = progressWidget.Handle(session);
            Stopwatch total = Stopwatch.StartNew();
            string? currentPhase = null;
            Stopwatch phaseWatch = new Stopwatch();
            List<StatusKind> phaseResults = new List<StatusKind>();
            session.Post(new LiveEvent("dashboard", "status", text: "building", status: "running"));
            Console.WriteLine("Build engine 3.0.168 · release pipeline");
            int index = 0;
            foreach (Step step in Steps) {
                if (session.Cancellation.IsCancellationRequested) {
                    break;
                }
                if (step.Phase != currentPhase) {
                    if (currentPhase != null) {
                        phases.Complete(currentPhase, Worst(phaseResults));
                    }
                    currentPhase = step.Phase;
                    phaseResults.Clear();
                    phases.Start(currentPhase);
                    phaseWatch.Restart();
                }
                steps.Set(step.Key, StatusKind.Running);
                progress.Label(step.Phase);
                Stopwatch stepWatch = Stopwatch.StartNew();
                int slices = Math.Max(1, (int)(step.Seconds * 10));
                for (int i = 0; i < slices && !session.Cancellation.IsCancellationRequested; i++) {
                    Thread.Sleep(100);
                    if (i < step.Output.Length) {
                        Console.WriteLine(step.Output[i]);
                    }
                    if (i % 5 == 0) {
                        module.Set("Elapsed", NumberFormat.Duration(total.Elapsed));
                    }
                }
                for (int i = slices; i < step.Output.Length; i++) {
                    Console.WriteLine(step.Output[i]);
                }
                if (step.Result == StatusKind.Warn) {
                    Console.Error.WriteLine("WARNING: 1 Pester test skipped on PowerShell 7 (requires conhost).");
                }
                string duration = NumberFormat.Duration(stepWatch.Elapsed);
                steps.Set(step.Key, step.Result);
                steps.Detail(step.Key, step.Result == StatusKind.Skip ? "skipped" : duration);
                phaseResults.Add(step.Result);
                summary.AddRow(step.Phase, Text.Markup.Escape(step.Label), TableCell.ForStatus(step.Result), step.Result == StatusKind.Skip ? "-" : duration);
                index++;
                progress.Set(index);
            }
            if (currentPhase != null) {
                phases.Complete(currentPhase, Worst(phaseResults));
            }
            module.Set("Elapsed", NumberFormat.Duration(total.Elapsed));
            progress.Complete(StatusKind.Pass);
            Console.WriteLine("Build completed: 2 artefacts in Artefacts\\ (nupkg, zip)");
            session.Post(new LiveEvent("dashboard", "status", text: "build succeeded with 1 warning", status: "warn"));
            Thread.Sleep(1500);
        }

        private static StatusKind Worst(List<StatusKind> results) {
            if (results.Contains(StatusKind.Fail)) {
                return StatusKind.Fail;
            }
            if (results.Contains(StatusKind.Warn)) {
                return StatusKind.Warn;
            }
            if (results.TrueForAll(r => r == StatusKind.Skip)) {
                return StatusKind.Skip;
            }
            return StatusKind.Pass;
        }
    }
}
