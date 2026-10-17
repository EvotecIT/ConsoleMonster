using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The non-interactive, line-based and accessible paths: defaults, clear errors, numbered questions, no hangs.</summary>
    public sealed class PromptModeTests {
        /// <summary>Line input and output from a list of answers, recording everything written.</summary>
        internal sealed class FakeLines : ILineConsole {
            private readonly Queue<string?> _answers;

            public FakeLines(params string?[] answers) {
                _answers = new Queue<string?>(answers);
            }

            public List<string> Output { get; } = new List<string>();

            public int? ChooseAnswer { get; set; }

            public string Text => string.Join("\n", Output);

            public void Write(string text) => Output.Add(text);

            public void WriteLine(string text) => Output.Add(text);

            public string? ReadLine() => _answers.Count == 0 ? null : _answers.Dequeue();

            public string? ReadSecret() => ReadLine();

            public int? Choose(string caption, string message, IReadOnlyList<string> choices, int defaultIndex) {
                if (ChooseAnswer.HasValue) {
                    Output.Add("choose:" + caption);
                }
                return ChooseAnswer;
            }
        }

        private static InteractiveOptions Lines(FakeLines lines, bool accessible) => new InteractiveOptions { Mode = accessible ? InteractionMode.Accessible : InteractionMode.Line, LineConsole = lines, Labels = new PromptLabels() };

        private static InteractiveOptions None() => new InteractiveOptions { Mode = InteractionMode.None, Labels = new PromptLabels() };

        [Fact]
        public void Show_NonInteractiveWithDefault_ReturnsTheDefault() {
            SelectPrompt<string> prompt = new SelectPrompt<string>("Environment", new[] { "Production", "Staging" }) { Default = "Staging" };
            Assert.Equal("Staging", prompt.Show(None()));
            Assert.True(new ConfirmPrompt("Go?", true).Show(None()));
            Assert.Equal(5m, new NumberPrompt("Count") { Default = 5 }.Show(None()));
        }

        [Fact]
        public void Show_NonInteractiveWithoutDefault_NamesTheParameter() {
            SelectPrompt<string> prompt = new SelectPrompt<string>("Environment", new[] { "Production", "Staging" }) { ParameterName = "-Environment" };
            PromptUnavailableException error = Assert.Throws<PromptUnavailableException>(() => prompt.Show(None()));
            Assert.Equal("Prompt 'Environment' needs a value in a non-interactive session; pass -Environment or set a default.", error.Message);
            Assert.Equal("-Environment", error.Parameter);
            PromptUnavailableException secret = Assert.Throws<PromptUnavailableException>(() => new SecretPrompt("Token").Show(None()));
            Assert.Equal("Prompt 'Token' needs a value in a non-interactive session; set a default.", secret.Message);
        }

        [Fact]
        public void ResolveMode_FlagsAndInputSources() {
            Assert.Equal(InteractionMode.None, new InteractiveOptions { NonInteractive = true }.ResolveMode());
            Assert.Equal(InteractionMode.Rich, new InteractiveOptions { Input = new ScriptedInput(string.Empty) }.ResolveMode());
            Assert.Equal(InteractionMode.Accessible, new InteractiveOptions { Mode = InteractionMode.Accessible }.ResolveMode());
        }

        [Fact]
        public void Select_LineMode_NumbersTheChoicesWithGroupsAndDisabledReasons() {
            FakeLines lines = new FakeLines("9", "3", "2");
            List<Choice> choices = new List<Choice> {
                new Choice("a", "Alpha") { Group = "First" },
                new Choice("b", "Beta") { Group = "Second" },
                new Choice("c", "Gamma") { Group = "Second", DisabledReason = "retired" }
            };
            object? value = new SelectPrompt<object?>("Pick one", choices).Show(Lines(lines, accessible: false));
            Assert.Equal("b", value);
            Assert.Contains("First:", lines.Output);
            Assert.Contains("  1) Alpha", lines.Output);
            Assert.Contains("  3) Gamma (unavailable: retired)", lines.Output);
            Assert.Contains("'9' is not one of the numbers.", lines.Output);
            Assert.Contains("Not available: retired", lines.Output);
        }

        [Fact]
        public void MultiSelect_AccessibleMode_AcceptsRangesAndAnnouncesTheAnswer() {
            FakeLines lines = new FakeLines("1-2,4");
            IReadOnlyList<string> values = new MultiSelectPrompt<string>("Pick", new[] { "a", "b", "c", "d" }) { Minimum = 1 }.Show(Lines(lines, accessible: true));
            Assert.Equal(new[] { "a", "b", "d" }, values.ToArray());
            Assert.Contains("selected: a, b, d", lines.Output);
            Assert.Contains(lines.Output, l => l.StartsWith("Enter numbers separated by commas"));
        }

        [Fact]
        public void MultiSelect_LineMode_EnforcesTheMinimumAndAll() {
            FakeLines lines = new FakeLines("none", "all");
            IReadOnlyList<string> values = new MultiSelectPrompt<string>("Pick", new[] { "a", "b" }) { Minimum = 1 }.Show(Lines(lines, accessible: false));
            Assert.Equal(new[] { "a", "b" }, values.ToArray());
            Assert.Contains("Select at least 1.", lines.Output);
        }

        [Fact]
        public void Confirm_LineMode_UsesTheHostChoiceWhenAvailable() {
            FakeLines host = new FakeLines { ChooseAnswer = 1 };
            Assert.False(new ConfirmPrompt("Delete?", true).Show(Lines(host, accessible: false)));
            Assert.Contains("choose:Delete?", host.Output);
            FakeLines typed = new FakeLines("maybe", "y");
            Assert.True(new ConfirmPrompt("Delete?").Show(Lines(typed, accessible: true)));
            Assert.Contains("Answer y or n.", typed.Output);
        }

        [Fact]
        public void Text_LineMode_RetriesUntilValidAndUsesTheDefault() {
            FakeLines lines = new FakeLines("Bad Name", "good");
            TextPrompt prompt = new TextPrompt("Name") { Pattern = "[a-z]+", PatternMessage = "letters only" };
            Assert.Equal("good", prompt.Show(Lines(lines, accessible: false)));
            Assert.Contains("letters only", lines.Output);
            FakeLines empty = new FakeLines(string.Empty);
            Assert.Equal("srv01", new TextPrompt("Name") { Default = "srv01" }.Show(Lines(empty, accessible: false)));
        }

        [Fact]
        public void Number_LineMode_ParsesAndChecksTheRange() {
            FakeLines lines = new FakeLines("x", "100", "8");
            Assert.Equal(8m, new NumberPrompt("Count") { WholeNumber = true, Minimum = 1, Maximum = 10 }.Show(Lines(lines, accessible: false)));
            Assert.Contains("Enter a whole number.", lines.Output);
            Assert.Contains("Enter 10 or less.", lines.Output);
        }

        [Fact]
        public void Show_LineModeWhenInputEnds_ThrowsInsteadOfHanging() {
            Assert.Throws<InputClosedException>(() => new TextPrompt("Name").Show(Lines(new FakeLines(), accessible: false)));
        }
    }
}
