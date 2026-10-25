using System;
using System.Linq;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    public sealed class SharedInputDefinitionTests {
        [Fact]
        public void MultipleSelectionKeepsChoiceIdentityAndOmittedInitialSelection() {
            object first = new object();
            object second = new object();
            var options = new SelectInputOptions { Key = "Targets", Multiple = true, Label = "Servers", Disabled = true, TabIndex = 3 };
            options.Choices.Add(new Choice(first, "First") { Selected = true });
            options.Choices.Add(new Choice(second, "Second"));
            var control = Assert.IsType<MultiSelect>(AppInput.Select(options));
            Assert.Same(first, Assert.Single(control.Value));
            Assert.Equal("Servers", control.Label);
            Assert.True(control.Disabled);
            Assert.Equal(3, control.TabIndex);
            options.Value = null;
            Assert.Empty(Assert.IsType<MultiSelect>(AppInput.Select(options)).Value);
            options.Value = new[] { second };
            Assert.Same(second, Assert.Single(Assert.IsType<MultiSelect>(AppInput.Select(options)).Value));
            options.Multiple = false;
            options.Value = first;
            Assert.Same(first, Assert.IsType<Select>(AppInput.Select(options)).Value);
        }

        [Fact]
        public void SelectionQuestionKeepsScalarAndCollectionResultsDistinct() {
            object value = new object();
            var options = new SelectQuestionOptions { Title = "Target", Default = value };
            options.Choices.Add(new Choice(value, "First"));
            var unattended = new InteractiveOptions { Mode = InteractionMode.None };
            Assert.Same(value, Question.Select(options).Show(unattended));
            options.Multiple = true;
            options.Default = new[] { value };
            var result = Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<object?>>(Question.Select(options).Show(unattended));
            Assert.Same(value, Assert.Single(result));
            options.Default = null;
            Assert.Empty(Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<object?>>(Question.Select(options).Show(unattended)));
        }

        [Fact]
        public void SelectionRejectsWrongKindOptionsAndUnavailableDefaultsBeforeOpening() {
            var options = new SelectQuestionOptions { Title = "Target", Minimum = 1 };
            options.Choices.Add(new Choice("First"));
            Assert.Throws<ArgumentException>(() => Question.Select(options));
            options.Multiple = true;
            options.Default = new[] { "missing" };
            Assert.Throws<ArgumentException>(() => Question.Select(options));
            options.Default = new[] { "First" };
            options.Choices[0].DisabledReason = "Offline";
            Assert.Throws<ArgumentException>(() => Question.Select(options));
        }

        [Fact]
        public void TextKindRejectsIrrelevantOptionsRatherThanIgnoringThem() {
            Assert.Throws<ArgumentException>(() => AppInput.Text(new TextInputOptions { Key = "Name", Height = 6 }));
            Assert.Throws<ArgumentException>(() => AppInput.Text(new TextInputOptions { Key = "Name", Multiline = true, Mask = null }));
            Assert.Throws<ArgumentException>(() => AppInput.Text(new TextInputOptions { Key = "Name", Multiline = true, Role = ThemeToken.Text }));
            var area = Assert.IsType<TextArea>(AppInput.Text(new TextInputOptions { Key = "Notes", Multiline = true, Height = 4, MaxLength = 200, Value = "One\nTwo", Placeholder = "Notes" }));
            Assert.Equal(4, area.Height);
            Assert.Equal("One\nTwo", area.Value);
            Assert.Equal(200, area.MaxLength);
        }

        [Fact]
        public void TextQuestionUsesExistingRenderersAndRetainsExplicitEmptyDefault() {
            var options = new TextQuestionOptions { Title = "Name", Default = null };
            Prompt<string> prompt = Question.Text(options);
            Assert.IsType<TextPrompt>(prompt);
            Assert.True(prompt.HasDefault);
            Assert.Equal(string.Empty, prompt.Default);
            options.Multiline = true;
            options.Height = 4;
            Assert.IsType<TextAreaPrompt>(Question.Text(options));
        }

        [Fact]
        public void SecretQuestionCannotReceiveDefaultsOrHistory() {
            Assert.Throws<ArgumentException>(() => Question.Text(new TextQuestionOptions { Title = "Token", AsSecret = true, Default = "secret" }));
            var options = new TextQuestionOptions { Title = "Token", AsSecret = true };
            options.History.Add("secret");
            Assert.Throws<ArgumentException>(() => Question.Text(options));
            options.History.Clear();
            var secret = Assert.IsType<SecretPrompt>(Question.Text(options));
            Assert.True(secret.Required);
            Assert.False(secret.HasDefault);
            options.Hidden = true;
            options.AllowEmpty = true;
            secret = Assert.IsType<SecretPrompt>(Question.Text(options));
            Assert.False(secret.Required);
            Assert.Equal(SecretDisplay.Hidden, secret.Display);
        }

        [Fact]
        public void MultilineQuestionDoesNotSilentlyDiscardSingleLineValidation() {
            Assert.Throws<ArgumentException>(() => Question.Text(new TextQuestionOptions { Title = "Notes", Multiline = true, Pattern = "[a-z]+" }));
            var options = new TextQuestionOptions { Title = "Notes", Multiline = true };
            options.History.Add("previous");
            Assert.Throws<ArgumentException>(() => Question.Text(options));
        }
    }
}
