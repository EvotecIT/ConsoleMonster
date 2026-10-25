using System;
using System.Linq;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Testing;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    public sealed class SharedInputDefinitionTests {
        [Fact]
        public void ChoiceSelectorsPreserveStoredMetadataAndExistingChoiceIdentity() {
            var row = new { Name = "web-01", Site = "Warsaw", Detail = "Ready", Reason = "Maintenance" };
            Choice choice = Assert.Single(Choice.From(new[] { row }, display: item => item.Name,
                groupBy: item => item.Site, describe: item => item.Detail, disabledReason: item => item.Reason));
            Assert.Same(row, choice.Value);
            Assert.Equal("web-01", choice.Label);
            Assert.Equal("Warsaw", choice.Group);
            Assert.Equal("Ready", choice.Description);
            Assert.Equal("Maintenance", choice.DisabledReason);
            Assert.Same(choice, Assert.Single(Choice.From(new[] { choice }, display: _ => "Ignored")));
        }

        [Fact]
        public async System.Threading.Tasks.Task ChoiceFieldUsesTheSameLoadChoicesNameAsTheMenuAndPowerShell() {
            var field = new ChoiceField("Site") {
                LoadChoices = (_, __) => System.Threading.Tasks.Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("Warsaw") })
            };
            Assert.Equal("Warsaw", Assert.Single(await field.LoadChoices!(new FormValues(), default)).Value);
        }

        [Fact]
        public void ButtonDefinition_UsesTypedCallbacksAfterInitializationAndOutsideTheControlLock() {
            var calls = new List<string>();
            Button? button = null;
            button = AppInput.Button(new ButtonInputOptions {
                Key = "Deploy", Label = "Run", TabIndex = 2,
                OnChange = (_, change) => calls.Add("change:" + change.Value),
                OnClick = (_, __) => calls.Add("click:" + button!.Value)
            });
            Assert.Empty(calls);
            Assert.Equal("Run", button.Label);
            Assert.Equal(2, button.TabIndex);
            Assert.True(button.HandleInput(InputEvent.Press(ConsoleKey.Enter), Terminal.CreateContext(TestTerminal.Profile())));
            Assert.Equal(new[] { "click:1", "change:1" }, calls);
            button.Disabled = true;
            Assert.False(button.HandleInput(InputEvent.Press(ConsoleKey.Enter), Terminal.CreateContext(TestTerminal.Profile())));
            Assert.Equal(2, calls.Count);
        }

        [Fact]
        public void ToggleDefinition_KeepsInheritedFluentNamesAndInitialValueWithoutCallingBack() {
            int changed = 0;
            var options = new ToggleInputOptions().Key("Trace").Label("Tracing").Value(true).OnText("Enabled").OffText("Disabled");
            options.OnChange = (_, __) => changed++;
            Toggle toggle = AppInput.Toggle(options);
            Assert.True(toggle.Value);
            Assert.Equal("Enabled", toggle.OnText);
            Assert.Equal("Disabled", toggle.OffText);
            Assert.Equal(0, changed);
            toggle.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Terminal.CreateContext(TestTerminal.Profile()));
            Assert.False(toggle.Value);
            Assert.Equal(1, changed);
        }

        [Fact]
        public void SelectionFactoryAppliesTheSamePageSizeToBothKinds() {
            var options = new SelectInputOptions { Key = "Targets", PageSize = 3 };
            Assert.Equal(3, Assert.IsType<Select>(AppInput.Select(options)).PageSize);
            options.Multiple = true;
            Assert.Equal(3, Assert.IsType<MultiSelect>(AppInput.Select(options)).PageSize);
            options.PageSize = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => AppInput.Select(options));
        }

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
