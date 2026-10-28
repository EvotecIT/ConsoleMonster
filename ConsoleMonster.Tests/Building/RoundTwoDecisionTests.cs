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

        private sealed record Container(int Id, string Name, string State, int Port);

        [Fact]
        public void TypedTablesInferIdentifierAndStatusPresentersLikePowerShell() {
            Container[] rows = { new Container(12345, "web", "running", 8080), new Container(23456, "db", "restarting", 5432) };
            Table<Container> table = Table.For(rows);
            string text = TestTerminal.RenderText(table, 80);
            Assert.Contains("12345", text);
            Assert.DoesNotContain("12 345", text);
            Assert.DoesNotContain("12,345", text);
            Assert.Same(ConsoleMonster.Presenters.CellPresenters.Identifier, table.Columns[0].Presenter);
            Assert.Same(ConsoleMonster.Presenters.CellPresenters.StatusWord, table.Columns[2].Presenter);
            Table<Container> chosen = Table.For(rows).Column(r => r.State, c => c.Presenter(ConsoleMonster.Presenters.CellPresenters.Auto));
            TestTerminal.RenderText(chosen, 80);
            Assert.Same(ConsoleMonster.Presenters.CellPresenters.Auto, chosen.Columns[0].Presenter);
        }

        [Theory]
        [InlineData("restarting", StatusKind.Warn)]
        [InlineData("exited", StatusKind.Skip)]
        [InlineData("CrashLoopBackOff", StatusKind.Fail)]
        [InlineData("Terminating", StatusKind.Warn)]
        public void ContainerAndServiceStatesReadAsStatusWords(string word, StatusKind expected) {
            Assert.True(ConsoleMonster.Presenters.StatusEnumPresenter.TryRead(word, out StatusKind status));
            Assert.Equal(expected, status);
        }

        [Fact]
        public void DuplicateWidgetIdsNameTheIdAndTheFix() {
            var error = Assert.Throws<ArgumentException>(() => new ConsoleMonster.Live.LiveSession(new Rows(Ui.Chart().Type(ChartType.Bar).Build(), Ui.Chart().Type(ChartType.Bar).Build())));
            Assert.Contains("'bars'", error.Message);
            Assert.Contains("-WidgetId", error.Message);
            Assert.Contains("bars-2", error.Message);
        }

        [Fact]
        public void StatusListGivesDetailsTheRoomShortLabelsLeave() {
            StatusList list = new StatusList().Add("DNS", StatusKind.Pass, "two resolvers answered in 12 ms each");
            string text = TestTerminal.RenderText(list, 50);
            Assert.Contains("two resolvers answered in 12 ms each", text);
            StatusList both = new StatusList().Add(new string('L', 40), StatusKind.Pass, new string('D', 40));
            string line = TestTerminal.RenderText(both, 50);
            Assert.Contains(new string('D', 20), line);
        }

        [Fact]
        public void StackedLegendPercentsAddUpToOneHundred() {
            StackedBar bar = new StackedBar().Add("Passed", 7, ThemeToken.Pass).Add("Warnings", 3, ThemeToken.Warn).Add("Failed", 2, ThemeToken.Fail);
            Assert.Equal(new[] { 58, 25, 17 }, bar.WholePercents(12));
            string text = TestTerminal.RenderText(bar, 80);
            Assert.Contains("17%", text);
            Assert.DoesNotContain("16%", text);
            Assert.Equal(new[] { 100, 0 }, new StackedBar().Add("All", 5).Add("None", 0).WholePercents(5));
            Assert.Equal(new[] { 34, 33, 33 }, new StackedBar().Add("a", 1).Add("b", 1).Add("c", 1).WholePercents(3));
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
