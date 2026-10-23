using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Dependent displays rebuilt through the public application boundary.</summary>
    [Collection("Live sessions")]
    public sealed class AppBindingTests {
        /// <summary>Every typed character reaches the dependency and changes both the visible table and its count.</summary>
        [Fact]
        public void Keystrokes_RebuildFilteredTableAndCount() {
            TextInput query = new TextInput("query");
            string[] names = { "alpha", "alpine", "beta" };
            List<string> queries = new List<string>();
            List<string> displays = new List<string>();
            AppBinding results = new AppBinding(state => {
                string filter = state.Get<string>("query");
                string[] matches = names.Where(name => name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                Table table = new Table().AddColumn("Name");
                foreach (string name in matches) { table.AddRow(name); }
                Rows display = new Rows(table, TextBlock.Plain("Matches: " + matches.Length));
                queries.Add(filter);
                displays.Add(TestTerminal.RenderText(display));
                return display;
            }, "results");
            ConsoleAppOptions options = AppTestHarness.Options("\"al\" Escape", out ScreenCapture capture);
            AppState result = ConsoleApp.Run(new Rows(query, results), options: options);
            Assert.Equal("al", result["query"]);
            // Initial/final refreshes may repeat a value; no keystroke may be coalesced away.
            Assert.Equal(new[] { "", "a", "al" }, queries.Where((value, index) => index == 0 || value != queries[index - 1]));
            Assert.Contains("Matches: 3", displays[queries.IndexOf("a")]);
            string filtered = displays[queries.IndexOf("al")];
            Assert.Contains("alpha", filtered);
            Assert.Contains("alpine", filtered);
            Assert.DoesNotContain("beta", filtered);
            Assert.Contains("Matches: 2", filtered);
            string final = capture.LastFrame!.ToPlainText();
            Assert.Contains("alpha", final);
            Assert.Contains("alpine", final);
            Assert.DoesNotContain("beta", final);
            Assert.Contains("Matches: 2", final);
        }

        /// <summary>State changes dispatched by the host rebuild dependencies without editing an input control.</summary>
        [Fact]
        public void HostStateChange_RebuildsDependencyBeforeNextInput() {
            AppState state = new AppState { ["status"] = "waiting" };
            List<string> built = new List<string>();
            AppBinding status = new AppBinding(values => {
                string value = values.Get<string>("status");
                built.Add(value);
                return TextBlock.Plain("Status: " + value);
            });
            ConsoleAppOptions options = AppTestHarness.Options("Escape", out ScreenCapture capture);
            options.ProcessEvents = values => values["status"] = "ready";
            Assert.Same(state, ConsoleApp.Run(status, state, options));
            Assert.Equal("waiting", built[0]);
            Assert.Contains("ready", built);
            Assert.Contains("Status: ready", capture.LastFrame!.ToPlainText());
        }

        /// <summary>Invalid binding output fails construction and leaves controls detached and reusable.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InvalidBindingOutput_CleansUpConstructionAndAllowsLaterRun(bool returnsInput) {
            TextInput query = new TextInput("query", "seed");
            AppState state = new AppState();
            AppBinding invalid = new AppBinding(_ => returnsInput ? new Rows(new TextInput("dynamic")) : null!);
            Assert.Throws<InvalidOperationException>(() => ConsoleApp.Run(new Rows(query, invalid), state,
                AppTestHarness.Options("Escape", out _)));
            query.Value = "next";
            Assert.Equal("seed", state["query"]);
            AppBinding valid = new AppBinding(values => TextBlock.Plain("Query: " + values.Get<string>("query")));
            ConsoleAppOptions options = AppTestHarness.Options("Escape", out ScreenCapture capture);
            Assert.Same(state, ConsoleApp.Run(new Rows(query, valid), state, options));
            Assert.Equal("next", state["query"]);
            Assert.Contains("Query: next", capture.LastFrame!.ToPlainText());
        }

        /// <summary>A dependency failure after an edit propagates and removes subscriptions before returning control.</summary>
        [Fact]
        public void BindingFailureAfterEdit_PropagatesAndAllowsLaterRun() {
            TextInput query = new TextInput("query");
            AppState state = new AppState();
            InvalidOperationException expected = new InvalidOperationException("Synthetic display failure.");
            AppBinding binding = new AppBinding(values => {
                string value = values.Get<string>("query");
                if (value.Length > 0) { throw expected; }
                return TextBlock.Plain("Empty query");
            });
            Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => ConsoleApp.Run(new Rows(query, binding), state,
                AppTestHarness.Options("\"a\" Escape", out _))));
            Assert.Equal("a", state["query"]);
            query.Value = "detached";
            Assert.Equal("a", state["query"]);
            Assert.Equal("detached", ConsoleApp.Run(query, state, AppTestHarness.Options("Escape", out _))["query"]);
        }
    }
}
