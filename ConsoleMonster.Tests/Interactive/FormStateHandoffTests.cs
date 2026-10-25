using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    public sealed class FormStateHandoffTests {
        private sealed class Host : IViewHost {
            public PromptLabels Labels { get; } = new PromptLabels();
            public List<Action> Calls { get; } = new List<Action>();
            public void ShowModal(Modal modal) { }
            public void ShowToast(Toast toast) { }
            public bool TryPost(Action call) { Calls.Add(call); return true; }
        }

        [Theory]
        [InlineData("Int32", false)]
        [InlineData("Int64", false)]
        [InlineData("Double", false)]
        [InlineData("Int32", true)]
        [InlineData("Int64", true)]
        [InlineData("Double", true)]
        public void Apply_EquivalentNumericDefault_PreservesDependentsButChangedNumberInvalidates(string originalType, bool shown) {
            object original = originalType == "Int32" ? (object)4 : originalType == "Int64" ? (object)4L : 4d;
            Form form = new Form("Numeric equality");
            NumberField count = form.Number("Count");
            count.Default = original;
            TextField dependent = form.Text("Result");
            dependent.DependsOn.Add("Count");
            FormView? view = shown ? new FormView(form, new Host(), null, false) : null;
            form.Apply(new Dictionary<string, object?> { ["Result"] = "keep" });
            form.Apply(new Dictionary<string, object?> { ["Count"] = 4m });
            Assert.Equal(4m, Assert.IsType<decimal>(count.Value));
            Assert.Equal("keep", dependent.Value);
            form.Apply(new Dictionary<string, object?> { ["Count"] = 5m });
            Assert.Equal(5m, count.Value);
            Assert.Null(dependent.Value);
            view?.Detach();
        }

        [Fact]
        public void Apply_NumericChoiceValues_PreserveBoxedTypeEquality() {
            Form form = new Form("Choice equality");
            ChoiceField choice = form.Select("Count", choices: new[] { new Choice(4, "Integer"), new Choice(4m, "Decimal") });
            choice.Default = 4;
            TextField dependent = form.Text("Result");
            dependent.Value = "cached";
            dependent.DependsOn.Add("Count");
            form.Apply(new Dictionary<string, object?> { ["Count"] = 4m });
            Assert.Equal(4m, Assert.IsType<decimal>(choice.Value));
            Assert.Null(dependent.Value);
        }

        // Caller-supplied lazy multiselection values are enumerated when a queued batch is actually applied.
        private sealed class SelectionValues : IEnumerable<object?> {
            private readonly Action _enumerating;
            public SelectionValues(Action enumerating) { _enumerating = enumerating; }
            public IEnumerator<object?> GetEnumerator() {
                _enumerating();
                yield return "older";
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Detach_FlushesAcceptedValuesBeforePublishingDetachedState(bool staleViewReference) {
            Form form = new Form("Handoff");
            form.MultiSelect("Targets", choices: new[] { new Choice("older"), new Choice("newer") });
            Host host = new Host();
            FormView view = new FormView(form, host, null, false);
            using ManualResetEventSlim flushing = new ManualResetEventSlim();
            using ManualResetEventSlim newerStarted = new ManualResetEventSlim();
            bool attachedDuringFlush = false;
            Exception? newerError = null;
            Thread newer = new Thread(() => {
                try {
                    if (!flushing.Wait(TimeSpan.FromSeconds(5))) { throw new TimeoutException("The accepted batch never started flushing."); }
                    newerStarted.Set();
                    Dictionary<string, object?> values = new Dictionary<string, object?> { ["Targets"] = new[] { "newer" } };
                    if (staleViewReference) { view.ApplyFromApp(values); }
                    else { form.Apply(values); }
                } catch (Exception ex) { newerError = ex; }
            });
            SelectionValues olderValues = new SelectionValues(() => {
                // Inspect publication at the real raw-value enumeration boundary, while the older batch is unfinished.
                attachedDuringFlush = ReferenceEquals(form.View, view);
                flushing.Set();
                if (!newerStarted.Wait(TimeSpan.FromSeconds(5))) { throw new TimeoutException("The newer caller never started."); }
            });
            Exception? olderError = null;
            Thread older = new Thread(() => {
                try { form.Apply(new Dictionary<string, object?> { ["Targets"] = olderValues }); }
                catch (Exception ex) { olderError = ex; }
            });
            older.Start();
            Assert.True(older.Join(TimeSpan.FromSeconds(5)));
            Assert.Null(olderError);
            Assert.Single(host.Calls);
            newer.Start();
            view.Detach();
            Assert.True(newer.Join(TimeSpan.FromSeconds(5)));
            Assert.Null(newerError);
            Assert.True(attachedDuringFlush, "A detached form was published while an older accepted batch was still being applied.");
            Assert.Null(form.View);
            Assert.Equal(new[] { "newer" }, form.GetValues().GetList<string>("Targets"));
            foreach (Action call in host.Calls) { call(); }
            Assert.Equal(new[] { "newer" }, form.GetValues().GetList<string>("Targets"));
        }

        [Theory]
        [InlineData("default")]
        [InlineData("applied")]
        [InlineData("loaded")]
        public void Revert_SingleCollectionChoice_RetainsTheOriginalChoiceObject(string source) {
            object[] first = { "first" };
            object[] second = { "second" };
            Form form = new Form("Collection identity");
            Choice[] choices = { new Choice(first, "First"), new Choice(second, "Second") };
            ChoiceField field = form.Select("Target", choices: source == "loaded" ? null : choices);
            if (source == "default") { field.Default = second; }
            else if (source == "applied") { field.Value = first; }
            else { field.LoadChoices = (v, t) => Task.FromResult<IEnumerable<Choice>>(choices); }
            form.Text("Name").Value = "saved";
            FormView view = new FormView(form, new Host(), null, false);
            if (source != "default") { form.Apply(new Dictionary<string, object?> { ["Target"] = second }); }
            view.Poll(TimeSpan.Zero);
            Assert.Same(second, field.Value);
            Press(view, ConsoleKey.DownArrow);
            Assert.True(view.HandleUnbound(InputEvent.Paste("changed")));
            Assert.True(view.IsDirty);
            view.Revert();
            Assert.Same(second, field.Value);
            Assert.Equal("saved", form.GetValues()["Name"]);
            Assert.False(view.IsDirty);
            view.Detach();
        }

        [Fact]
        public void Revert_MultipleChoice_CopiesTheSelectionContainerButRetainsItsItems() {
            object[] target = { "target" };
            Form form = new Form("Multi identity");
            ChoiceField field = form.MultiSelect("Targets", choices: new[] { new Choice(target, "Target") });
            field.Default = new[] { target };
            FormView view = new FormView(form, new Host(), null, false);
            IList current = Assert.IsAssignableFrom<IList>(field.Value);
            current.Clear();
            view.Revert();
            IList restored = Assert.IsAssignableFrom<IList>(field.Value);
            Assert.NotSame(current, restored);
            Assert.Same(target, Assert.Single(restored.Cast<object>()));
            restored.Clear();
            view.Revert();
            Assert.Same(target, Assert.Single(Assert.IsAssignableFrom<IList>(field.Value).Cast<object>()));
            view.Detach();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void KeyboardDependencyChange_DropsThePreviousPendingChoiceIntent(bool stored) {
            Form form = new Form("Server checks");
            ChoiceField server = form.Select("Server", choices: new[] { new Choice("E1"), new Choice("W1") });
            server.Default = "E1";
            ChoiceField checks = form.MultiSelect("Checks");
            checks.DependsOn.Add("Server");
            TaskCompletionSource<IEnumerable<Choice>> east = new TaskCompletionSource<IEnumerable<Choice>>();
            List<string?> loadedFor = new List<string?>();
            checks.LoadChoices = (v, t) => {
                string? selected = v.Get<string>("Server");
                loadedFor.Add(selected);
                return selected == "E1" ? east.Task : Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("Ping") });
            };
            FormView view = new FormView(form, new Host(), null, false);
            Dictionary<string, object?> requested = new Dictionary<string, object?> { ["Checks"] = new[] { "Ping" } };
            if (stored) { view.ApplyValues(requested, true, true); }
            else { form.Apply(requested); }
            Press(view, ConsoleKey.RightArrow);
            east.SetResult(new[] { new Choice("Ping") });
            view.Poll(TimeSpan.Zero);
            Assert.Equal(new[] { "E1", "W1" }, loadedFor);
            Assert.Equal("W1", server.Value);
            Assert.Equal("Ping", Assert.Single(checks.Choices).Value);
            Assert.Empty(form.GetValues().GetList<string>("Checks"));
            view.Detach();
        }

        private static void Press(FormView view, ConsoleKey key) {
            ViewFrame frame = new ViewFrame(Terminal.CreateContext(TerminalProfile.Fixed(80, 24, ColorDepth.None, GlyphTier.Full), Theme.Dark), 80, 24, false, TimeSpan.Zero);
            List<KeyBinding> bindings = new List<KeyBinding>();
            view.AddBindings(bindings, frame);
            bindings.First(b => b.Matches(InputEvent.Press(key))).Action();
        }
    }
}
