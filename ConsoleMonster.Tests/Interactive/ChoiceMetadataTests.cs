using ConsoleMonster.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Typed metadata selectors keep returned objects, badges and disabled choices together.</summary>
    public sealed class ChoiceMetadataTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PresetsAppliedBeforeLoadingRetainTheirRequestedValues(bool saved) {
            var menu = new Menu("Toolbox");
            var pending = new TaskCompletionSource<IEnumerable<Choice>>();
            var item = new MenuItem("Targets").LoadChoices((_, _) => pending.Task);
            menu.Add(item);
            if (saved) MenuPresets.Restore(menu, "", new Dictionary<string, object?> { ["Targets"] = new[] { "api", "db" } });
            else new MenuPreset("Nightly").Selection(item, "api", "db").ApplySelections(menu);
            var pane = new ChoiceItemPane(item, new PromptLabels(), () => { }, () => { }, () => null!);
            Assert.True(pane.Loading);
            pane.Sync();
            Assert.Equal(2, item.Selection.Count);
            pending.SetResult(new[] { new Choice("api"), new Choice("db") { DisabledReason = "maintenance" } });
            pane.Sync(); // A completed task still awaits Poll before the list can represent its choices.
            pane.Poll(TimeSpan.Zero);
            Assert.Equal("api", Assert.Single(item.Selection));
        }

        [Fact]
        public void DeferredObjectSelectionsKeepIdentityWhenDisplayTextIsTheSame() {
            var first = new NamedTarget();
            var second = new NamedTarget();
            var item = new MenuItem("Targets").LoadChoices((_, _) => Task.FromResult<IEnumerable<Choice>>(new[] {
                new Choice(first), new Choice(second)
            }));
            item.SetSelection(new object?[] { second });
            var pane = new ChoiceItemPane(item, new PromptLabels(), () => { }, () => { }, () => null!);
            pane.Poll(TimeSpan.Zero);
            Assert.Same(second, Assert.Single(item.Selection));
        }

        [Fact]
        public void LoadedSingleSelectionFocusesItsRequestedValue() {
            var selected = new Choice("db");
            var item = new MenuItem("Target").LoadChoices((_, _) => Task.FromResult<IEnumerable<Choice>>(new[] {
                new Choice("api"), selected
            }), multiple: false);
            item.SetSelection(new object?[] { "db" });
            var pane = new ChoiceItemPane(item, new PromptLabels(), () => { }, () => { }, () => null!);
            pane.Poll(TimeSpan.Zero);
            Assert.Equal("db", item.SelectedValue);
            Assert.Same(selected, pane.List.FocusedChoice);
        }

        private sealed class NamedTarget {
            public override string ToString() => "same label";
        }

        [Fact]
        public void InlineMenusLoadChoicesBeforeAskingAndDeferredSelectionsRemainStable() {
            var item = new MenuItem("Targets").LoadChoices((_, _) => Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("api"), new Choice("db") }), multiple: false);
            var menu = new Menu("Toolbox");
            menu.Add(item);
            var options = new MenuOptions { Interactive = new InteractiveOptions {
                Mode = InteractionMode.Line, LineConsole = new PromptModeTests.FakeLines("1"), Labels = new PromptLabels()
            } };
            var runner = new InlineMenu(menu, options, new PromptLabels(), new ChoiceStore("Loader contract", Path.GetTempPath()), CancellationToken.None);
            runner.Open(item);
            Assert.Equal("api", item.SelectedValue);
            item.SetSelection(item.Selection.Concat(new[] { "db" }));
            Assert.Equal(new object?[] { "api", "db" }, item.Selection);
        }

        [Fact]
        public void LoadedChoicesDropASelectionThatHasBecomeDisabled() {
            var item = new MenuItem("Targets").LoadChoices((_, _) => Task.FromResult<IEnumerable<Choice>>(new[] {
                new Choice("api"), new Choice("db") { DisabledReason = "maintenance" }
            }));
            item.SetSelection(new object?[] { "api", "db" });
            var pane = new ChoiceItemPane(item, new PromptLabels(), () => { }, () => { }, () => null!);
            Assert.True(pane.Poll(TimeSpan.Zero));
            Assert.Equal("api", Assert.Single(item.Selection));
        }

        [Fact]
        public void TypedChoiceSelectorsKeepValuesAndChoiceMetadataTogether() {
            var values = new[] {
                new { Name = "api", Role = "Web", Offline = "" },
                new { Name = "db", Role = "Data", Offline = "maintenance" }
            };
            var choices = Choice.From(values, item => item.Name, _ => "Primary",
                description: item => item.Role + " service",
                badge: item => ConsoleMonster.Text.RichText.Plain(item.Role), disabled: item => item.Offline);
            Assert.Same(values[0], choices[0].Value);
            Assert.Equal("Primary", choices[0].Group);
            Assert.Equal("Web service", choices[0].Description);
            Assert.Equal("Web", choices[0].Badge!.PlainText);
            Assert.True(choices[0].IsEnabled);
            Assert.False(choices[1].IsEnabled);
            Assert.Equal("maintenance", choices[1].DisabledReason);
        }

    }
}
