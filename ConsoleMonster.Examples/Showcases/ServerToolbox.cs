using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConsoleMonster.Building;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases
{
    /// <summary>
    /// The admin-tool showcase in the Shell layout: a "Server toolbox" with targets loaded asynchronously and grouped by
    /// site, grouped tasks with a preview, an options form, presets, a review with the equivalent command, and a run into
    /// a live dashboard that returns to a results summary with next actions. All data is invented.
    /// </summary>
    public static class ServerToolbox
    {
        private sealed class Run
        {
            public Run(DateTime when, int servers, int tasks, int passed, int warnings, int failed, TimeSpan elapsed)
            {
                When = when;
                Servers = servers;
                Tasks = tasks;
                Passed = passed;
                Warnings = warnings;
                Failed = failed;
                Elapsed = elapsed;
            }

            public DateTime When { get; }
            public int Servers { get; }
            public int Tasks { get; }
            public int Passed { get; }
            public int Warnings { get; }
            public int Failed { get; }
            public TimeSpan Elapsed { get; }
        }

        private static readonly List<Run> History = new List<Run>
        {
            new Run(new DateTime(2026, 10, 15, 21, 4, 0), 8, 5, 37, 2, 1, TimeSpan.FromMinutes(6.2)),
            new Run(new DateTime(2026, 10, 16, 7, 30, 0), 3, 2, 6, 0, 0, TimeSpan.FromMinutes(1.1)),
            new Run(new DateTime(2026, 10, 16, 18, 45, 0), 9, 11, 91, 6, 2, TimeSpan.FromMinutes(14.8))
        };
        /// <summary>Builds the toolbox menu.</summary>
        /// <returns>The menu.</returns>
        public static Menu Build()
        {
            Menu menu = new Menu("Server toolbox")
            {
                Subtitle = "lab · 11 servers",
                Layout = MenuLayout.Shell
            };
            MenuItem targets = new MenuItem("Targets")
            {
                Hotkey = 't',
                IconName = GlyphIcon.Targets,
                Description = "Servers the tasks run on. The list is loaded from the inventory; offline servers cannot be picked.",
                LoadChoices = (context, token) => ToolboxData.LoadServersAsync(null, token)
            };
            targets.Minimum = 1;
            MenuItem tasks = new MenuItem("Tasks").Hotkey('k').IconName(GlyphIcon.Tasks).Description("What to run on every target. Tasks marked 'changes' modify the servers; the rest only read.").MultiSelect(ToolboxData.Tasks, t => t.Name, t => t.Category, ToolboxData.TaskPreview);
            foreach (Choice c in tasks.Choices)
            {
                ToolboxTask task = (ToolboxTask)c.Value!;
                RichTextBuilder badge = new RichTextBuilder();
                if (task.Changes)
                {
                    badge.Append("changes ", ThemeToken.Warn);
                }

                badge.Append("~" + task.Minutes.ToString(CultureInfo.InvariantCulture) + " min", ThemeToken.Muted);
                c.Badge = badge.Build();
                c.Description = task.Description;
            }

            tasks.SetSelection(new object? [] { ToolboxData.Tasks[0], ToolboxData.Tasks[3], ToolboxData.Tasks[4] });
            Form options = OptionsForm();
            menu.Add(targets);
            menu.Add(tasks);
            MenuItem optionsItem = new MenuItem("Options").Hotkey('o').IconName(GlyphIcon.Settings).Description("How the run behaves: parallelism, the report and notifications.").Form(options, (context, result) => context.Toast("Options saved"));
            menu.Add(optionsItem);
            menu.Item("Presets", i => i.Hotkey('p').IconName(GlyphIcon.Favorite).Description("Saved combinations of targets, tasks and options. Save one from Review & run.").Presets());
            menu.Separator();
            menu.Item("Review & run", i =>
            {
                i.Hotkey = 'r';
                i.IconName = GlyphIcon.Run;
                i.Review = context => Review(context, targets, tasks, optionsItem, options);
            });
            menu.Item("History", i =>
            {
                i.Hotkey = 'h';
                i.Page = context => HistoryPage();
                i.Detail = () => History.Count.ToString(CultureInfo.CurrentCulture) + " runs";
            });
            menu.Separator();
            menu.Exit("Quit");
            menu.CommandProviders.Add(new CommandProvider("Task", () => ToolboxData.Tasks.Select(task => new PaletteCommand("Toggle " + task.Name, context =>
            {
                List<object?> selection = tasks.Selection.ToList();
                bool on = selection.Remove(task);
                if (!on)
                {
                    selection.Add(task);
                }

                tasks.SetSelection(ToolboxData.Tasks.Where(t => selection.Contains(t)).Cast<object?>());
                context.Toast((on ? "Removed " : "Added ") + task.Name, on ? StatusKind.Info : StatusKind.Pass);
            }) { Description = task.Description })));
            return menu;
        }

        private static Form OptionsForm()
        {
            Form form = new Form("Options")
            {
                Review = false,
                SubmitText = "Save",
                Command = string.Empty
            };
            FormPage page = form.Page("Options", "Saved with presets; used by Review & run.");
            NumberField parallel = page.Number("Parallel", "Servers at a time");
            parallel.Integer = true;
            parallel.Minimum = 1;
            parallel.Maximum = 16;
            parallel.Default = 4m;
            parallel.ParameterName = "ThrottleLimit";
            parallel.Help = "How many servers run their tasks at the same time.";
            PathField report = page.Path("Report", "Report folder");
            report.PathKind = PathKind.Folder;
            report.Default = System.IO.Path.Combine("Reports", "Toolbox");
            report.ParameterName = "ReportPath";
            report.Help = "Created when it does not exist. Tab completes folder names.";
            ChoiceField format = page.Select("Format", "Report format", new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") });
            format.Default = "Html";
            format.ParameterName = "Format";
            ToggleField notify = page.Toggle("Notify", "Email the report");
            notify.ParameterName = "Notify";
            TextField email = page.Text("Email", "Recipient");
            email.VisibleWhen = v => v.Get<bool>("Notify");
            email.Required = true;
            email.Placeholder = "ops@example.org";
            email.ParameterName = "To";
            email.Validator = (value, values) => value is string s && s.Contains("@") ? null : "Enter an email address";
            ToggleField dryRun = page.Toggle("DryRun", "Dry run");
            dryRun.ParameterName = "WhatIf";
            dryRun.Help = "Reports what would change without changing anything.";
            return form;
        }

        private static ReviewModel Review(MenuContext context, MenuItem targets, MenuItem tasks, MenuItem optionsItem, Form options)
        {
            ReviewModel review = new ReviewModel("Review & run")
            {
                Description = "Everything the run will use. The command does the same without the menu."
            };
            List<ToolboxServer> servers = targets.GetSelection<ToolboxServer>().ToList();
            List<ToolboxTask> selected = tasks.GetSelection<ToolboxTask>().ToList();
            FormValues values = options.GetValues();
            review.Row("Targets", servers.Count == 0 ? string.Empty : servers.Count + " · " + string.Join(", ", servers.Select(s => s.Name)));
            review.Row("Tasks", selected.Count == 0 ? string.Empty : selected.Count + " · " + string.Join(", ", selected.Select(t => t.Name)));
            review.Row("Servers at a time", options.Find("Parallel")!.Display(values["Parallel"], PromptLabels.Default));
            review.Row("Report", values.Get<string>("Report") + " (" + options.Find("Format")!.Display(values["Format"], PromptLabels.Default) + ")");
            review.Row("Email", values.Get<bool>("Notify") ? values.Get<string>("Email") : "off");
            review.Row("Dry run", values.Get<bool>("DryRun") ? "yes" : "no");
            if (servers.Count == 0)
            {
                review.Problem("Pick at least one target", targets);
            }

            if (selected.Count == 0)
            {
                review.Problem("Pick at least one task", tasks);
            }

            if (values.Get<bool>("Notify") && string.IsNullOrWhiteSpace(values.Get<string>("Email")))
            {
                review.Problem("Enter the email recipient in Options, or turn off Email the report", optionsItem);
            }

            List<string> arguments = new List<string>
            {
                CommandLine.Argument("ComputerName", servers.Select(s => s.Name).ToList(), CommandStyle.PowerShell),
                CommandLine.Argument("Task", selected.Select(t => t.Id).ToList(), CommandStyle.PowerShell)
            };
            string optionArguments = options.BuildCommand();
            review.Command = CommandLine.Join("Invoke-ServerToolbox", arguments) + (optionArguments.Length > 0 ? " " + optionArguments : string.Empty);
            review.Run = ctx => Execute(ctx, servers, selected, values);
            return review;
        }

        private static void Execute(MenuContext context, List<ToolboxServer> servers, List<ToolboxTask> tasks, FormValues options)
        {
            Lanes lanes = new Lanes("lanes");
            foreach (ToolboxServer server in servers)
            {
                lanes.Add(server.Name, new RichTextBuilder().Append(server.Name, ThemeToken.Accent).Append("  " + server.Site, ThemeToken.Muted).Build(), tasks.Count);
            }

            ProgressBar overall = new ProgressBar("Overall", "overall")
            {
                Total = servers.Count * tasks.Count,
                LabelWidth = 8
            };
            LogRegion log = new LogRegion
            {
                PreferredHeight = 6
            };
            Dashboard dashboard = new DashboardBuilder("[bold]Server toolbox[/]").Subtitle(servers.Count + " servers · " + tasks.Count + " tasks" + (options.Get<bool>("DryRun") ? " · dry run" : string.Empty)).Panel("Servers", lanes).Add(new Padding(overall, 1, 0, 1, 0)).Panel("Log", log, fill: true).BuildDashboard();
            int throttle = (int)options.Get<decimal>("Parallel", 4m);
            context.Run(dashboard, run => Work(run, lanes, overall, log, servers, tasks, throttle));
        }

        private static MenuRunResult Work(RunContext run, Lanes lanesWidget, ProgressBar overallWidget, LogRegion logWidget, List<ToolboxServer> servers, List<ToolboxTask> tasks, int throttle)
        {
            LanesHandle lanes = lanesWidget.Handle(run.Session);
            ProgressHandle overall = overallWidget.Handle(run.Session);
            LogHandle log = logWidget.Handle(run.Session);
            Random random = new Random(42);
            int passed = 0;
            int warnings = 0;
            int failed = 0;
            Table failures = new Table
            {
                Border = TableBorder.Minimal
            };
            failures.AddColumn("Server");
            failures.AddColumn("Task");
            failures.AddColumn(new TableColumn("Finding") { Flexible = true });
            Stopwatch watch = Stopwatch.StartNew();
            log.Write("Started " + tasks.Count + " tasks on " + servers.Count + " servers, " + throttle + " at a time");
            for (int t = 0; t < tasks.Count; t++)
            {
                foreach (ToolboxServer server in servers)
                {
                    run.CancellationToken.ThrowIfCancellationRequested();
                    lanes.Start(server.Name, tasks[t].Name);
                }

                Thread.Sleep(450);
                foreach (ToolboxServer server in servers)
                {
                    double roll = random.NextDouble();
                    StatusKind outcome = roll < 0.08 ? StatusKind.Fail : roll < 0.2 ? StatusKind.Warn : StatusKind.Pass;
                    lanes.Advance(server.Name, outcome, tasks[t].Name);
                    overall.Advance();
                    switch (outcome)
                    {
                        case StatusKind.Fail:
                            failed++;
                            string finding = tasks[t].Category == "Security" ? "3 updates missing for 41 days" : "volume D: below 5% free";
                            failures.AddRow(server.Name, tasks[t].Name, finding);
                            log.Error(server.Name + " · " + tasks[t].Name + ": " + finding);
                            break;
                        case StatusKind.Warn:
                            warnings++;
                            log.Warn(server.Name + " · " + tasks[t].Name + ": needs attention");
                            break;
                        default:
                            passed++;
                            break;
                    }
                }

                Thread.Sleep(250);
            }

            foreach (ToolboxServer server in servers)
            {
                lanes.Complete(server.Name);
            }

            overall.Complete(failed > 0 ? StatusKind.Fail : warnings > 0 ? StatusKind.Warn : StatusKind.Pass);
            Thread.Sleep(600);
            History.Add(new Run(DateTime.Now, servers.Count, tasks.Count, passed, warnings, failed, watch.Elapsed));
            MenuRunResult result = new MenuRunResult("Run finished", failed > 0 ? StatusKind.Fail : warnings > 0 ? StatusKind.Warn : StatusKind.Pass)
            {
                Summary = servers.Count + " servers checked. " + (failed > 0 ? failed + " findings need action." : warnings > 0 ? warnings + " warnings to review; nothing failed." : "Nothing needs action."),
                Details = failed > 0 ? failures : null,
                Elapsed = watch.Elapsed
            };
            result.Figure("passed", passed.ToString(CultureInfo.CurrentCulture));
            if (warnings > 0)
            {
                result.Figure("warnings", warnings.ToString(CultureInfo.CurrentCulture), StatusKind.Warn);
            }
            else
            {
                result.Figure("warnings", "0");
            }

            if (failed > 0)
            {
                result.Figure("failed", failed.ToString(CultureInfo.CurrentCulture), StatusKind.Fail);
            }
            else
            {
                result.Figure("failed", "0");
            }

            result.Next("Open the report", ctx => ctx.Toast("Report opened in the browser", StatusKind.Info));
            if (failed > 0)
            {
                result.Next("Run the failed tasks again", ctx => ctx.Toast("Queued " + failed + " tasks", StatusKind.Info));
            }

            result.Next("Export findings to CSV", ctx => ctx.Toast("Saved findings.csv"));
            return result;
        }

        private static IWidget HistoryPage()
        {
            Table table = new Table
            {
                Border = TableBorder.Minimal
            };
            table.AddColumn("When");
            table.AddColumn(new TableColumn("Servers") { Alignment = HorizontalAlignment.Right });
            table.AddColumn(new TableColumn("Tasks") { Alignment = HorizontalAlignment.Right });
            table.AddColumn("Result");
            table.AddColumn(new TableColumn("Duration") { Alignment = HorizontalAlignment.Right });
            foreach (Run run in Enumerable.Reverse(History))
            {
                StatusKind status = run.Failed > 0 ? StatusKind.Fail : run.Warnings > 0 ? StatusKind.Warn : StatusKind.Pass;
                table.AddRow(run.When.ToString("ddd HH:mm", CultureInfo.CurrentCulture), run.Servers.ToString(CultureInfo.CurrentCulture), run.Tasks.ToString(CultureInfo.CurrentCulture), TableCell.ForStatus(status, run.Passed + " passed · " + run.Warnings + " warn · " + run.Failed + " failed"), NumberFormat.Duration(run.Elapsed));
            }

            return table;
        }

        /// <summary>Runs the toolbox.</summary>
        /// <param name = "layout">The layout override, or Auto.</param>
        public static void Show(MenuLayout layout = MenuLayout.Auto)
        {
            Build().Run(new MenuOptions { Layout = layout, AppName = "ConsoleMonster Server toolbox", RememberLastChoices = true });
        }
    }
}
