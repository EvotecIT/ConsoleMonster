using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    /// <summary>The approved round 2 additions: question entry points, item adds, panel gap and confirm keys.</summary>
    public sealed class RoundTwoDecisionTests {
        private static readonly InteractiveOptions Unattended = new InteractiveOptions { Mode = InteractionMode.None };

        private sealed record Server(string Name, string Site);

        [Fact]
        public void TextQuestionAsksTheSameDefinitionAsReadConsoleText() {
            Assert.Equal("Reports", Ui.TextQuestion().Title("Share name").Default("Reports").Show(Unattended));
            Assert.True(Ui.TextQuestion().AsSecret().AsSecret);
            Assert.True(Ui.TextQuestion().Multiline().Multiline);
            Assert.Throws<ArgumentException>(() => Ui.TextQuestion().AsSecret().Title("Password").Default("x").Show(Unattended));
        }

        [Fact]
        public void SelectQuestionReturnsTypedSingleAndMultipleAnswers() {
            Server[] servers = { new Server("FS01", "Warsaw"), new Server("FS11", "Berlin") };
            Server one = Ui.SelectQuestion().Title("Target").AddChoices(Choice.From(servers, s => s.Name, s => s.Site)).Default(servers[1]).Show<Server>(Unattended);
            Assert.Same(servers[1], one);
            IReadOnlyList<Server> many = Ui.SelectQuestion().Multiple().Title("Targets").AddChoices(Choice.From(servers, s => s.Name, s => s.Site)).Default(servers).ShowMany<Server>(Unattended);
            Assert.Equal(servers, many);
            SelectQuestionOptions multiple = Ui.SelectQuestion().Multiple().Title("Targets").AddChoices(Choice.From(servers, s => s.Name, s => s.Site));
            Assert.Throws<InvalidOperationException>(() => multiple.Show<Server>(Unattended));
            Assert.Throws<InvalidOperationException>(() => Ui.SelectQuestion().Title("Target").AddChoices(Choice.From(servers, s => s.Name, s => s.Site)).Default(servers[0]).ShowMany<Server>(Unattended));
            Assert.Throws<InvalidCastException>(() => Ui.SelectQuestion().Title("Target").AddChoices(Choice.From(servers, s => s.Name, s => s.Site)).Default(servers[0]).Show<string>(Unattended));
        }

        [Fact]
        public void UiOffersQuestionsInsteadOfPerKindPromptFactories() {
            var names = typeof(Ui).GetMethods().Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
            Assert.Contains("TextQuestion", names);
            Assert.Contains("SelectQuestion", names);
            Assert.DoesNotContain("SecretPrompt", names);
            Assert.DoesNotContain("MultiSelectPrompt", names);
            Assert.DoesNotContain("TextPrompt", names);
            Assert.DoesNotContain("SelectPrompt", names);
        }

        [Fact]
        public void ChartAddItemMatchesKeyValueAdd() {
            ChartOptions options = Ui.Chart().Type(ChartType.Bar).AddItem("web01", 42).AddItem("db01", 97, ThemeToken.Fail, "db");
            Assert.Equal(new[] { "web01", "db01" }, options.Items.Select(item => item.Label));
            Assert.Equal("db", options.Items[1].Key);
            string text = TestTerminal.RenderText(options.Build(), 40);
            Assert.Contains("web01", text);
            Assert.Contains("97", text);
            Assert.Equal("CPU", ((Sparkline)Ui.Chart().Type(ChartType.Sparkline).Title("CPU").Build()).Title);
        }

        [Fact]
        public void FlowNodeAndEdgeMatchThePowerShellRows() {
            Flow flow = Ui.Flow().Node("build", "Build", StatusKind.Pass).Node("test", "Test").Edge("build", "test", "artifacts");
            Assert.Equal(new[] { "build", "test" }, flow.Nodes.Select(node => node.Key));
            FlowEdge edge = Assert.Single(flow.Edges);
            Assert.Equal("build->test", edge.Key);
            Assert.Equal("artifacts", edge.Label);
            Assert.Equal("link", Ui.Flow().Node("a", "A").Node("b", "B").Edge("a", "b", key: "link").Edges[0].Key);
        }

        [Fact]
        public void PanelGapSpacesComposedContentInEitherOrder() {
            string before = TestTerminal.RenderText(Ui.Panel().Gap(1).Content(c => { c.Text("one"); c.Text("two"); }), 20);
            string after = TestTerminal.RenderText(Ui.Panel().Content(c => { c.Text("one"); c.Text("two"); }).Gap(1), 20);
            string dense = TestTerminal.RenderText(Ui.Panel().Content(c => { c.Text("one"); c.Text("two"); }), 20);
            Assert.Equal(before, after);
            Assert.Equal(dense.Split('\n').Length + 1, before.Split('\n').Length);
            Rows own = new Rows(new TextBlock("one"), new TextBlock("two")) { Gap = 2 };
            Assert.Equal(2, ((Rows)new Panel(own) { Gap = 1 }.Content).Gap);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Panel { Gap = -1 });
        }

        [Theory]
        [InlineData("pl-PL", "y", true)]
        [InlineData("pl-PL", "n", false)]
        [InlineData("pl-PL", "t", true)]
        [InlineData("de-DE", "y", true)]
        [InlineData("de-DE", "j", true)]
        [InlineData("fr-FR", "y", true)]
        [InlineData("fr-FR", "n", false)]
        public void ConfirmAcceptsEnglishKeysAlongsideTheLocalizedOnes(string culture, string key, bool expected) {
            var target = TestTerminal.Target(60, 8, culture: CultureInfo.GetCultureInfo(culture), keys: "\"" + key + "\"");
            Assert.Equal(expected, new ConfirmPrompt("Deploy?", !expected).Show(new InteractiveOptions { Target = target }));
        }
    }
}
