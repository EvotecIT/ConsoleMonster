using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// A live job monitor with three screens: an overview of twelve jobs (type a number to open one), a job's phases as a
    /// diagram or cards, and the job's settings with a dropdown. A background feed advances the jobs and writes the log.
    /// The screens, footer hints, breadcrumbs, dialog, dropdown and toast all come from the app; nothing handles raw keys.
    /// </summary>
    public static class JobMonitor {
        private static readonly string[] Phases = { "Fetch", "Build", "Test", "Publish" };
        private static readonly string[] Names = {
            "nightly-backup", "web-deploy", "sql-index", "cert-renew", "dns-sync", "report-mail",
            "log-rotate", "user-sync", "patch-scan", "cache-warm", "mail-archive", "ad-health"
        };

        /// <summary>One synthetic job.</summary>
        public sealed class Job {
            internal Job(int number, string name) { Number = number; Name = name; }
            /// <summary>The number that opens it.</summary>
            public int Number { get; }
            /// <summary>The job name.</summary>
            public string Name { get; }
            /// <summary>The current phase index.</summary>
            public int Phase { get; internal set; }
            /// <summary>Progress through the current phase.</summary>
            public int Percent { get; internal set; }
            /// <summary>The job state.</summary>
            public StatusKind State { get; internal set; } = StatusKind.Pending;
            /// <summary>How often it runs.</summary>
            public string Schedule { get; internal set; } = "Nightly";
            /// <summary>Whether a failure sends mail.</summary>
            public bool Notify { get; internal set; }
        }

        /// <summary>Runs the monitor until Escape on the overview, and returns the final state.</summary>
        /// <param name="interactive">Optional input, capture and theme settings (scripted keys and memory capture in tests).</param>
        /// <param name="once">Prints one frame of the overview instead of running.</param>
        /// <returns>The final state.</returns>
        public static AppState Run(InteractiveOptions? interactive = null, bool once = false) {
            // <example>
            List<Job> jobs = Names.Select((name, i) => new Job(i + 1, name)).ToList();
            object gate = new object();
            AppState state = new AppState { ["job"] = 1, ["view"] = "diagram" };
            AppDispatcher dispatcher = new AppDispatcher();
            AppDialogs dialogs = new AppDialogs();
            Job Selected() => jobs[state.Get("job", 1) - 1];

            LogViewport log = new LogViewport("log") { Label = "Log", Height = 6, ShowElapsed = false, Capacity = 500 };
            Select schedule = new Select("schedule", new[] { new Choice("Hourly"), new Choice("Nightly"), new Choice("Weekly") }) { Label = "Schedule", Dropdown = true };
            Toggle notify = new Toggle("notify") { Label = "Mail me when it fails" };
            Button save = new Button("save", "Save");

            AppScreens screens = new AppScreens();
            screens.Add(new AppScreen("overview", "Overview", new AppBinding(_ => Overview(jobs, gate))) {
                Bindings = { KeyBinding.Numbered("open", () => jobs.Count, number => { state["job"] = number; screens.Push("job"); }) }
            });
            screens.Add(new AppScreen("job", "Job", new AppBinding(s => Detail(Selected(), s.Get("view", "diagram"), gate))) {
                TitleSource = _ => Selected().Name,
                Bindings = {
                    new KeyBinding("view", () => state["view"] = state.Get("view", "diagram") == "diagram" ? "cards" : "diagram", new KeyChord('v')),
                    new KeyBinding("restart", () => ConfirmRestart(Selected(), dialogs, log, gate), new KeyChord('r')),
                    new KeyBinding("settings", () => {
                        schedule.Value = Selected().Schedule;
                        notify.Value = Selected().Notify;
                        screens.Push("settings");
                    }, new KeyChord('s'))
                }
            });
            screens.Add(new AppScreen("settings", "Settings", new Rows(
                new AppBinding(_ => new TextBlock("[muted]Settings for[/] [bold]" + Selected().Name + "[/]")),
                schedule, notify, save)));
            save.Clicked += (_, __) => {
                Selected().Schedule = (string?)schedule.Value ?? "Nightly";
                Selected().Notify = notify.Value;
                dialogs.Show(new Toast("Saved " + Selected().Name + ": " + Selected().Schedule + (Selected().Notify ? ", mail on failure" : "")));
            };

            Rows layout = new Rows()
                .Add(new AppBinding(_ => Header(jobs, gate)), LayoutSize.Fixed(1))
                .Add(screens, LayoutSize.Fraction())
                .Add(log, LayoutSize.Fixed(8));
            ConsoleAppOptions options = new ConsoleAppOptions {
                Dispatcher = dispatcher, Dialogs = dialogs, MinimumWidth = 50, MinimumHeight = 20, Interactive = interactive
            };
            if (once) { options.Interactive = (interactive ?? new InteractiveOptions()).Clone(); options.Interactive.Mode = InteractionMode.Snapshot; }

            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                Task feed = once ? Task.CompletedTask : Task.Run(() => Feed(jobs, gate, dispatcher, log, state, stop.Token));
                try { return ConsoleApp.Run(layout, state, options); }
                finally { stop.Cancel(); try { feed.Wait(); } catch (AggregateException) { } }
            }
            // </example>
        }

        private static IWidget Header(List<Job> jobs, object gate) {
            lock (gate) {
                int running = jobs.Count(job => job.State == StatusKind.Running);
                int failed = jobs.Count(job => job.State == StatusKind.Fail);
                StatusKind overall = failed > 0 ? StatusKind.Fail : running > 0 ? StatusKind.Running : StatusKind.Pass;
                return new HeaderBar("Job monitor") { Status = overall, Detail = running + " running · " + failed + " failed" };
            }
        }

        private static IWidget Overview(List<Job> jobs, object gate) {
            StatusList list = new StatusList("overview");
            lock (gate) {
                foreach (Job job in jobs) { list.Add($"[bold]{job.Number,2}[/]  {job.Name}", job.State, Chain(job)); }
            }
            return new Rows(new TextBlock("[muted]Type a job's number to open it; two digits open 10 to 12.[/]"), list) { Gap = 1 };
        }

        private static IWidget Detail(Job job, string view, object gate) {
            lock (gate) {
                string facts = "[muted]" + job.Schedule + (job.Notify ? " · mail on failure" : "") + "[/]";
                if (view == "diagram") {
                    Flow flow = new Flow("job-flow");
                    for (int i = 0; i < Phases.Length; i++) {
                        flow.Node(Phases[i], Phases[i] + (i == job.Phase && job.State == StatusKind.Running ? " " + job.Percent + "%" : ""), PhaseState(job, i));
                        if (i > 0) { flow.Edge(Phases[i - 1], Phases[i]); }
                    }
                    return new Rows(new TextBlock("[bold]" + job.Name + "[/] · diagram view  " + facts), flow) { Gap = 1 };
                }
                Columns cards = new Columns { Gap = 1 };
                for (int i = 0; i < Phases.Length; i++) {
                    StatusKind state = PhaseState(job, i);
                    cards.Add(new Panel(new TextBlock(state == StatusKind.Running ? job.Percent + "% done" : state.ToString()), Phases[i]) { Status = state });
                }
                return new Rows(new TextBlock("[bold]" + job.Name + "[/] · card view  " + facts), cards) { Gap = 1 };
            }
        }

        private static void ConfirmRestart(Job job, AppDialogs dialogs, LogViewport log, object gate) {
            FormDialog<bool> confirm = new FormDialog<bool>("Restart " + job.Name + "?", new TextBlock("Runs every phase again, starting with Fetch."), _ => true) {
                AcceptText = "Restart", DefaultAccept = true, MaxWidth = 48
            };
            dialogs.Show(confirm, result => {
                if (!result.Accepted) { return; }
                lock (gate) { job.Phase = 0; job.Percent = 0; job.State = StatusKind.Running; }
                log.Write("[" + job.Number + "] " + job.Name + ": restarted");
                dialogs.Show(new Toast("Restarted " + job.Name));
            });
        }

        // Advances a random job every 40 ms; all changes are posted to the app thread.
        private static async Task Feed(List<Job> jobs, object gate, AppDispatcher dispatcher, LogViewport log, AppState state, CancellationToken token) {
            Random random = new Random(7);
            int tick = 0;
            while (!token.IsCancellationRequested) {
                try { await Task.Delay(40, token).ConfigureAwait(false); } catch (OperationCanceledException) { return; }
                Job job = jobs[random.Next(jobs.Count)];
                int now = ++tick;
                dispatcher.TryPost(() => {
                    string line;
                    lock (gate) {
                        if (job.State == StatusKind.Pass || job.State == StatusKind.Fail) { return; }
                        job.State = StatusKind.Running;
                        job.Percent += 20;
                        if (job.Number == 4 && job.Phase == 2 && job.Percent >= 60) { job.State = StatusKind.Fail; line = "test suite failed: 3 errors"; }
                        else if (job.Percent < 100) { line = Phases[job.Phase] + " " + job.Percent + "%"; }
                        else if (job.Phase == Phases.Length - 1) { job.State = StatusKind.Pass; line = "finished"; }
                        else { job.Percent = 0; job.Phase++; line = "entered " + Phases[job.Phase]; }
                    }
                    log.Write("[" + job.Number + "] " + job.Name + ": " + line, job.State == StatusKind.Fail ? "error" : "info");
                    state["tick"] = now;
                });
            }
        }

        private static StatusKind PhaseState(Job job, int phase) => phase < job.Phase ? StatusKind.Pass : phase == job.Phase ? job.State : StatusKind.Pending;

        private static string Chain(Job job) => string.Join(" › ", Phases.Select((phase, i) => {
            StatusKind state = PhaseState(job, i);
            string mark = state == StatusKind.Pass ? "✓" : state == StatusKind.Running ? "●" : state == StatusKind.Fail ? "✗" : "○";
            return mark + " " + phase + (state == StatusKind.Running ? " " + job.Percent + "%" : "");
        }));
    }
}
