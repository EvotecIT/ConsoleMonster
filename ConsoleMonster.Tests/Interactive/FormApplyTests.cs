using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// Form.Apply sets values whether or not the form is shown: the fields take them, the shown text of every field
    /// follows (text and number boxes too), and choice fields whose options load from a changed field load again, choosing
    /// a value given for them once the options arrive.
    /// </summary>
    [Collection("Live sessions")]
    public sealed class FormApplyTests {
        // A domain whose servers load for it, a name and a count.
        private static Form Scope(List<string> loads) => Scope(loads, new List<int>());

        // The same form; threads records the thread each load of the "west" servers starts on.
        private static Form Scope(List<string> loads, List<int> threads) {
            Form form = new Form("Scope") { Command = "Invoke-Scope", Review = false };
            ChoiceField domain = form.Select("Domain", "Domain", new[] { new Choice("east", "East"), new Choice("west", "West") });
            domain.Default = "east";
            domain.Parameter = "Domain";
            ChoiceField servers = form.MultiSelect("Servers", "Servers");
            servers.DependsOn.Add("Domain");
            servers.Parameter = "ComputerName";
            servers.Loader = (values, token) => {
                string chosen = values.Get<string>("Domain") ?? "none";
                lock (loads) {
                    loads.Add(chosen);
                    if (chosen == "west") {
                        threads.Add(Environment.CurrentManagedThreadId);
                    }
                }
                string prefix = chosen == "west" ? "W" : "E";
                return Task.FromResult<IEnumerable<Choice>>(new[] { new Choice(prefix + "1"), new Choice(prefix + "2") });
            };
            TextField name = form.Text("Name", "Run name");
            name.Parameter = "Name";
            NumberField count = form.Number("Count", "Retries");
            count.WholeNumber = true;
            count.Default = 2m;
            count.Parameter = "Retry";
            return form;
        }

        private static Dictionary<string, object?> West() => new Dictionary<string, object?> {
            ["Domain"] = "west",
            ["Servers"] = new[] { "W2" },
            ["Name"] = "night run",
            ["Count"] = 7
        };

        [Fact]
        public void Apply_BeforeShowing_SetsValuesAndChoosesLoadedOptionsWhenTheyArrive() {
            List<string> loads = new List<string>();
            Form form = Scope(loads);
            form.Apply(West());
            Assert.Equal("west", form.Find("Domain")!.Value);
            Assert.Equal("night run", form.Find("Name")!.Value);
            Assert.Equal(7m, form.Find("Count")!.Value);

            RecordingSurface surface = new RecordingSurface(80, 24, false);
            FormResult result = PromptHarness.Guard(surface, () => form.Show(new FormOptions { Interactive = PromptHarness.Options("Down Down Down Down Enter", surface) }));
            string first = surface.Frames[0].ToPlainText();
            Assert.Contains("night run", first);
            Assert.Contains("7", first.Split('\n').First(l => l.Contains("Retries")));
            Assert.True(result.Submitted);
            Assert.Equal(new[] { "W2" }, result.Values.GetList<string>("Servers").ToArray());
            Assert.Equal(new[] { "west" }, loads.ToArray());
            Assert.Equal("Invoke-Scope -Domain west -ComputerName W2 -Name 'night run' -Retry 7", result.CommandLine);
        }

        [Fact]
        public void Apply_ChangedDependencyBeforeShowing_LoadsTheOptionsAgain() {
            List<string> loads = new List<string>();
            Form form = Scope(loads);
            RecordingSurface surface = new RecordingSurface(80, 24, false);
            PromptHarness.Guard(surface, () => form.Show(new FormOptions { Interactive = PromptHarness.Options("Escape", surface) }));
            Assert.Equal(new[] { "E1", "E2" }, ((ChoiceField)form.Find("Servers")!).Choices.Select(c => (string)c.Value!).ToArray());

            // The form has closed: the old options are dropped and the wanted servers wait for the next showing.
            form.Apply(new Dictionary<string, object?> { ["Domain"] = "west", ["Servers"] = new[] { "W1" } });
            Assert.Empty(((ChoiceField)form.Find("Servers")!).Choices);
            FormResult result = PromptHarness.Guard(surface, () => form.Show(new FormOptions { Interactive = PromptHarness.Options("Down Down Down Down Enter", surface) }));
            Assert.Equal(new[] { "W1" }, result.Values.GetList<string>("Servers").ToArray());
            Assert.Equal(new[] { "east", "west" }, loads.ToArray());
        }

        [Fact]
        public void Apply_WhileOpenInAMenu_RefreshesTheShownTextAndReloadsDependentOptions() {
            List<string> loads = new List<string>();
            Form form = Scope(loads);
            Menu menu = new Menu("Toolbox");
            menu.Item("Scope", i => i.Hotkey('s').Form(form));
            menu.Item("Night", i => i.Hotkey('n').Action(ctx => form.Apply(West())));
            menu.Exit("Quit");
            RecordingSurface surface = new RecordingSurface(100, 26, false);
            // Open the form, go back, run the action that applies the values, then look at the form again and quit:
            // the applied values are the form's saved state, so quitting does not ask about unsaved changes.
            PromptHarness.Guard(surface, () => {
                menu.Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = PromptHarness.Options("s Escape n Up Ctrl+Q", surface), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-apply-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
                return 0;
            });
            string shown = surface.Frames.Select(f => f.ToPlainText()).Last(f => f.Contains("Run name"));
            Assert.Contains("night run", shown);
            Assert.Contains("West", shown);
            Assert.Contains("W2", shown);
            Assert.Contains("7", shown.Split('\n').First(l => l.Contains("Retries")));
            Assert.Equal(new[] { "east", "west" }, loads.ToArray());
            Assert.Equal(new object?[] { "W2" }, ((IEnumerable<object?>)form.Find("Servers")!.Value!).ToArray());
            Assert.DoesNotContain(surface.Frames, f => f.ToPlainText().Contains("Discard unsaved changes"));
        }

        [Fact]
        public void Apply_ValuesOfAnEarlierRun_PutsEveryFieldBack() {
            List<string> loads = new List<string>();
            Form first = Scope(loads);
            first.Apply(West());
            FormValues earlier = first.GetValues();
            Form again = Scope(loads);
            again.Apply(earlier);
            Assert.Equal("west", again.Find("Domain")!.Value);
            Assert.Equal("night run", again.Find("Name")!.Value);
            Assert.Equal(7m, again.Find("Count")!.Value);
        }

        [Fact]
        public void Apply_FromAnAsyncAction_IsCarriedOutOnTheMenuThread() {
            List<string> loads = new List<string>();
            List<int> threads = new List<int>();
            Form form = Scope(loads, threads);
            int menuThread = Environment.CurrentManagedThreadId;
            int actionThread = 0;
            Menu menu = new Menu("Toolbox");
            menu.Item("Scope", i => i.Hotkey('s').Form(form));
            menu.Item("Night", i => i.Hotkey('n').ActionAsync(async (ctx, token) => {
                await Task.Delay(20, token).ConfigureAwait(false);
                actionThread = Environment.CurrentManagedThreadId;
                form.Apply(West());
            }));
            menu.Exit("Quit");
            RecordingSurface surface = new RecordingSurface(100, 26, false);
            InteractiveOptions options = PromptHarness.Options("s Escape n wait:500 Up wait:200 Ctrl+Q", surface);
            ((ScriptedInput)options.Input!).RealTime = true;
            PromptHarness.Guard(surface, () => {
                menu.Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = options, StorageRoot = Path.Combine(Path.GetTempPath(), "cm-apply-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
                return 0;
            });
            Assert.NotEqual(menuThread, actionThread);
            // The reload the values started ran on the menu's thread, not on the action's worker.
            Assert.Equal(new[] { menuThread }, threads.ToArray());
            Assert.Equal(new object?[] { "W2" }, ((IEnumerable<object?>)form.Find("Servers")!.Value!).ToArray());
            Assert.Contains("night run", surface.Frames.Select(f => f.ToPlainText()).Last(f => f.Contains("Run name")));
        }

        // A host that cannot take calls from other threads.
        private sealed class ClosedHost : IViewHost {
            public PromptLabels Labels { get; } = new PromptLabels();

            public void ShowModal(Modal modal) {
            }

            public void ShowToast(Toast toast) {
            }

            public bool TryPost(Action call) => false;
        }

        [Fact]
        public void Apply_FromAnotherThreadWithoutAQueue_FailsClearly() {
            Form form = Scope(new List<string>());
            FormView view = new FormView(form, new ClosedHost(), null, false);
            Exception? error = null;
            System.Threading.Thread stray = new System.Threading.Thread(() => {
                try {
                    form.Apply(West());
                } catch (Exception ex) {
                    error = ex;
                }
            });
            stray.Start();
            stray.Join();
            Assert.Contains("another thread", Assert.IsType<InvalidOperationException>(error).Message);
            view.Detach();
        }

        [Fact]
        public void Apply_BeforeShowing_WinsOverRememberedChoicesAndThePreset() {
            string root = Path.Combine(Path.GetTempPath(), "cm-apply-last-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            try {
                new ChoiceStore("Scope app", root).SaveLast("Scope", new Dictionary<string, object?> { ["Name"] = "remembered", ["Count"] = "3", ["Domain"] = "east" });
                Form form = Scope(new List<string>());
                form.Preset("Quick", p => p.Value("Name", "preset").Value("Count", 5));
                form.Apply(new Dictionary<string, object?> { ["Name"] = "applied", ["Domain"] = "west", ["Servers"] = new[] { "W1" } });
                RecordingSurface surface = new RecordingSurface(80, 24, false);
                FormResult result = PromptHarness.Guard(surface, () => form.Show(new FormOptions { AppName = "Scope app", StorageRoot = root, RememberLastChoices = true, Preset = "Quick", Interactive = PromptHarness.Options("Down Down Down Down Enter", surface) }));
                // Last choices, then the preset, then the applied values: each later one wins where they overlap.
                Assert.Equal("applied", result.Values["Name"]);
                Assert.Equal(5m, result.Values["Count"]);
                Assert.Equal("west", result.Values["Domain"]);
                Assert.Equal(new[] { "W1" }, result.Values.GetList<string>("Servers").ToArray());
                Assert.Equal("Quick", result.Preset);
            } finally {
                if (Directory.Exists(root)) {
                    Directory.Delete(root, true);
                }
            }
        }

        [Fact]
        public void Apply_BeforeShowingInLineMode_WinsOverRememberedChoicesAndThePreset() {
            string root = Path.Combine(Path.GetTempPath(), "cm-apply-line-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            try {
                new ChoiceStore("Line app", root).SaveLast("Run", new Dictionary<string, object?> { ["Name"] = "remembered", ["Count"] = "3" });
                Form form = new Form("Run") { Review = false };
                form.Text("Name", "Run name");
                NumberField count = form.Number("Count", "Retries");
                count.WholeNumber = true;
                form.Preset("Quick", p => p.Value("Name", "preset").Value("Count", 5));
                form.Apply(new Dictionary<string, object?> { ["Name"] = "applied" });
                PromptModeTests.FakeLines lines = new PromptModeTests.FakeLines(string.Empty, string.Empty, "y");
                FormResult result = form.Show(new FormOptions { AppName = "Line app", StorageRoot = root, RememberLastChoices = true, Preset = "Quick", Interactive = new InteractiveOptions { Mode = InteractionMode.Accessible, LineConsole = lines, Labels = new PromptLabels() } });
                Assert.True(result.Submitted);
                Assert.Equal("applied", result.Values["Name"]);
                Assert.Equal(5m, result.Values["Count"]);
                Assert.Equal("Quick", result.Preset);
            } finally {
                if (Directory.Exists(root)) {
                    Directory.Delete(root, true);
                }
            }
        }

        [Fact]
        public void Apply_ThenDiscard_KeepsTheValuesThatArrivedWithTheOptions() {
            Form form = Scope(new List<string>());
            Menu menu = new Menu("Toolbox");
            menu.Item("Scope", i => i.Hotkey('s').Form(form));
            menu.Item("Night", i => i.Hotkey('n').Action(ctx => form.Apply(West())));
            menu.Exit("Quit");
            RecordingSurface surface = new RecordingSurface(100, 26, false);
            // Apply (the servers arrive with the reload), then edit the name and leave with Discard.
            PromptHarness.Guard(surface, () => {
                menu.Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = PromptHarness.Options("s Escape n Up Enter Down Down \"x\" Escape y Ctrl+Q", surface), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-apply-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
                return 0;
            });
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Discard the changes to Scope?"));
            Assert.Equal("night run", form.Find("Name")!.Value);
            Assert.Equal(new object?[] { "W2" }, ((IEnumerable<object?>)form.Find("Servers")!.Value!).ToArray());
        }

        [Fact]
        public void Apply_ChangedField_ResetsItsOtherDependentsUnlessGiven() {
            Form form = new Form("Modes");
            form.Select("Mode", "Mode", new[] { new Choice("a"), new Choice("b") }).Default = "a";
            TextField detail = form.Text("Detail", "Detail");
            detail.DependsOn.Add("Mode");
            detail.Value = "kept";
            form.Apply(new Dictionary<string, object?> { ["Mode"] = "b" });
            Assert.Null(detail.Value);
            form.Apply(new Dictionary<string, object?> { ["Mode"] = "a", ["Detail"] = "given" });
            Assert.Equal("given", detail.Value);
        }

        [Fact]
        public void Apply_UnknownNamesAndOptions_AreIgnored() {
            Form form = Scope(new List<string>());
            form.Apply(new Dictionary<string, object?> { ["Missing"] = "x", ["Domain"] = "north", ["Name"] = "kept" });
            Assert.Equal("east", form.Find("Domain")!.Value);
            Assert.Equal("kept", form.Find("Name")!.Value);
            Assert.Throws<ArgumentNullException>(() => form.Apply((IDictionary<string, object?>)null!));
        }
    }
}
