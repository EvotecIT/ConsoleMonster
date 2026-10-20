using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    [Collection("Live sessions")]
    public sealed class FormsPresetRegressionTests {
        private sealed class Target {
            public override string ToString() => "same";
        }

        private sealed class Host : IViewHost {
            public PromptLabels Labels { get; } = new PromptLabels();
            public List<Action> Calls { get; } = new List<Action>();
            public void ShowModal(Modal modal) { }
            public void ShowToast(Toast toast) { }
            public bool TryPost(Action call) { Calls.Add(call); return true; }
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        public void Apply_RawChoices_PreservesIdentityAcrossOverloadsAndLoading(bool formValues, bool loaded, bool shown) {
            Target first = new Target();
            Target second = new Target();
            Choice[] options = { new Choice(first, "First"), new Choice(second, "Second") };
            Form form = new Form("Identity");
            ChoiceField single = form.Select("Single", choices: loaded ? null : options);
            ChoiceField multi = form.MultiSelect("Multi", choices: loaded ? null : options);
            if (loaded) {
                single.Loader = multi.Loader = (v, t) => Task.FromResult<IEnumerable<Choice>>(options);
            }
            FormView? view = shown ? new FormView(form, new Host(), null, false) : null;
            Dictionary<string, object?> values = new Dictionary<string, object?> { ["Single"] = second, ["Multi"] = new[] { second } };
            if (formValues) {
                FormValues earlier = new FormValues();
                foreach (KeyValuePair<string, object?> pair in values) { earlier.Set(pair.Key, pair.Value, true); }
                form.Apply(earlier);
            } else {
                form.Apply(values);
            }
            view = view ?? new FormView(form, new Host(), null, false);
            view.Poll(TimeSpan.Zero);
            Assert.Same(second, single.Value);
            Assert.Same(second, Assert.Single(form.GetValues().GetList<Target>("Multi")));
            view.Detach();
        }

        [Fact]
        public void Apply_SingleChoiceWhoseValueIsACollection_PreservesItsIdentityAndInvalidatesDependents() {
            object[] first = { "same" };
            object[] second = { "same" };
            Form form = new Form("Collection values");
            ChoiceField field = form.Select("Target", choices: new[] { new Choice(first, "First"), new Choice(second, "Second") });
            field.Value = first;
            TextField dependent = form.Text("Result");
            dependent.Value = "cached";
            dependent.DependsOn.Add("Target");
            form.Apply(new Dictionary<string, object?> { ["Target"] = second });
            Assert.Same(second, field.Value);
            Assert.Null(dependent.Value);
        }

        [Fact]
        public void FormPreset_RawPendingChoices_PreservesIdentityInAccessibleMode() {
            Target first = new Target();
            Target second = new Target();
            Form form = new Form("Identity") { Review = false };
            ChoiceField multi = form.MultiSelect("Targets");
            multi.Loader = (v, t) => Task.FromResult<IEnumerable<Choice>>(new[] { new Choice(first, "First"), new Choice(second, "Second") });
            form.Preset("Second", p => p.Value("Targets", new[] { second }));
            PromptModeTests.FakeLines lines = new PromptModeTests.FakeLines(string.Empty, "y");
            FormResult result = form.Show(new FormOptions { Preset = "Second", Interactive = new InteractiveOptions { Mode = InteractionMode.Accessible, LineConsole = lines } });
            Assert.True(result.Submitted);
            Assert.Same(second, Assert.Single(result.Values.GetList<Target>("Targets")));
        }

        [Fact]
        public void BuiltInMenuPreset_SelectsOnlyTheRequestedRawObject() {
            Target first = new Target();
            Target second = new Target();
            Menu menu = new Menu("Identity");
            MenuItem item = new MenuItem("Targets").MultiSelect(new[] { first, second }, t => ReferenceEquals(t, first) ? "First" : "Second");
            menu.Add(item);
            menu.Preset("Second", p => p.Selection(item, second));
            Assert.True(MenuPresets.Apply(menu, null, "Second"));
            Assert.Same(second, Assert.Single(item.Selection));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Apply_DependencyChain_StartsTheNextLoaderAfterRestoringItsParent(bool beforeShowing) {
            Form form = new Form("Chain");
            form.Text("Domain").Value = "east";
            ChoiceField server = form.Select("Server", choices: new[] { new Choice("E1") });
            server.Value = "E1";
            server.DependsOn.Add("Domain");
            server.Loader = (v, t) => Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("W1") });
            ChoiceField check = form.Select("Check", choices: new[] { new Choice("old") });
            check.Value = "old";
            check.DependsOn.Add("Server");
            List<string?> loadedFor = new List<string?>();
            check.Loader = (v, t) => {
                loadedFor.Add(v.Get<string>("Server"));
                return Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("new") });
            };
            Dictionary<string, object?> values = new Dictionary<string, object?> { ["Domain"] = "west", ["Server"] = "W1", ["Check"] = "new" };
            if (beforeShowing) { form.Apply(values); }
            FormView view = new FormView(form, new Host(), null, false);
            if (!beforeShowing) { form.Apply(values); }
            for (int i = 0; i < 3; i++) { view.Poll(TimeSpan.Zero); }
            Assert.Equal(new[] { "W1" }, loadedFor);
            Assert.Equal("new", check.Value);
            view.Detach();
        }

        [Fact]
        public void Apply_EmptyDependentSelection_ClosesTheObsoleteChoiceEditor() {
            Form form = new Form("Scope");
            form.Text("Domain").Value = "east";
            ChoiceField servers = form.MultiSelect("Servers", choices: new[] { new Choice("E1") });
            servers.DependsOn.Add("Domain");
            servers.Loader = (v, t) => new TaskCompletionSource<IEnumerable<Choice>>().Task;
            FormView view = new FormView(form, new Host(), null, false);
            ViewFrame frame = new ViewFrame(Terminal.CreateContext(TerminalProfile.Fixed(80, 24, ColorDepth.None, GlyphTier.Full), Theme.Dark), 80, 24, false, TimeSpan.Zero);
            Press(view, frame, ConsoleKey.DownArrow);
            Press(view, frame, ConsoleKey.Enter);
            Assert.Contains("Servers", view.Crumbs);
            form.Apply(new Dictionary<string, object?> { ["Domain"] = "west" });
            Assert.DoesNotContain("Servers", view.Crumbs);
            Assert.Empty(form.GetValues().GetList<string>("Servers"));
            view.Detach();
        }

        private static void Press(FormView view, ViewFrame frame, ConsoleKey key) {
            List<KeyBinding> bindings = new List<KeyBinding>();
            view.AddBindings(bindings, frame);
            bindings.First(b => b.Matches(InputEvent.Press(key))).Action();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Apply_SecretChange_InvalidatesDependentsWithoutPersistingTheSecret(bool shown) {
            Form form = new Form("Credentials");
            form.Secret("Password").Value = "old";
            TextField dependent = form.Text("Result");
            dependent.Value = "cached";
            dependent.DependsOn.Add("Password");
            FormView? view = shown ? new FormView(form, new Host(), null, false) : null;
            form.Apply(new Dictionary<string, object?> { ["Password"] = "new" });
            Assert.Null(dependent.Value);
            Assert.False(form.ToStored().ContainsKey("Password"));
            view?.Detach();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Apply_AcceptedBeforeDetach_SurvivesWithoutAnotherPoll(bool standalone) {
            Form form = new Form("Closing");
            form.Text("Name").Value = "old";
            Host host = new Host();
            FormHostView? standaloneHost = standalone ? new FormHostView(form, new PromptLabels(), null, false) : null;
            FormView view = standaloneHost?.Pane ?? new FormView(form, host, null, false);
            Exception? error = null;
            Thread worker = new Thread(() => {
                try { form.Apply(new Dictionary<string, object?> { ["Name"] = "accepted" }); }
                catch (Exception ex) { error = ex; }
            });
            worker.Start();
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
            Assert.Null(error);
            view.Detach();
            Assert.Equal("accepted", form.GetValues()["Name"]);
            form.Apply(new Dictionary<string, object?> { ["Name"] = "newer" });
            foreach (Action call in host.Calls) { call(); }
            standaloneHost?.Poll(TimeSpan.Zero);
            Assert.Equal("newer", form.GetValues()["Name"]);
        }

        [Fact]
        public void Apply_QueuedOnTheLastMenuTurn_SurvivesShutdown() {
            Form form = new Form("Closing");
            form.Text("Name").Value = "old";
            Menu menu = new Menu("Closing app");
            menu.Item("Form", i => i.Hotkey('f').Form(form));
            menu.Item("Apply and exit", i => i.Hotkey('a').Action(ctx => {
                Exception? error = null;
                Thread worker = new Thread(() => {
                    try { form.Apply(new Dictionary<string, object?> { ["Name"] = "accepted" }); }
                    catch (Exception ex) { error = ex; }
                });
                worker.Start();
                Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
                Assert.Null(error);
                ctx.Exit();
            }));
            string root = Path.Combine(Path.GetTempPath(), "cm-apply-exit-" + Guid.NewGuid().ToString("N"));
            try {
                RecordingSurface surface = new RecordingSurface(100, 26, false);
                PromptHarness.Guard(surface, () => {
                    menu.Run(new MenuOptions { Layout = MenuLayout.Shell, StorageRoot = root, Interactive = PromptHarness.Options("f Escape a", surface) });
                    return 0;
                });
                Assert.Equal("accepted", form.GetValues()["Name"]);
            } finally {
                if (Directory.Exists(root)) { Directory.Delete(root, true); }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Poll_EmptyOrFailedLoader_DoesNotRetryWithoutInvalidation(bool fail) {
            Form form = new Form("Load once");
            ChoiceField field = form.Select("Choices");
            int calls = 0;
            field.Loader = (v, t) => {
                calls++;
                if (fail) { throw new IOException("Unavailable"); }
                return Task.FromResult<IEnumerable<Choice>>(Array.Empty<Choice>());
            };
            FormView view = new FormView(form, new Host(), null, false);
            for (int i = 0; i < 3; i++) { view.Poll(TimeSpan.Zero); }
            Assert.Equal(1, calls);
            view.Detach();
        }
    }
}
