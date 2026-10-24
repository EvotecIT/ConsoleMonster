using System.Globalization;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    public sealed class NumberPromptPrecisionTests {
        [Theory]
        [InlineData("en-US", false)]
        [InlineData("en-US", true)]
        [InlineData("pl-PL", false)]
        [InlineData("pl-PL", true)]
        public void NumberPrompt_DefaultAndSteppingRetainFullPrecision(string culture, bool step) {
            const decimal initial = 0.1234567890123456789012345678m;
            const decimal increment = 0.0000000000000000000000000001m;
            NumberPrompt prompt = new NumberPrompt("Rate") {
                Default = initial, Step = increment, Culture = CultureInfo.GetCultureInfo(culture)
            };
            RecordingSurface surface = new RecordingSurface(80, 16, false);
            decimal result = PromptHarness.Guard(surface,
                () => prompt.Show(PromptHarness.Options(step ? "Up Enter" : "Enter", surface)));
            Assert.Equal(step ? initial + increment : initial, result);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("pl-PL")]
        public void NumberPrompt_LocalFractionRequiresCorrectionInWholeNumberMode(string culture) {
            CultureInfo format = CultureInfo.GetCultureInfo(culture);
            PromptLabels labels = new PromptLabels();
            PromptModeTests.FakeLines lines = new PromptModeTests.FakeLines(4.5m.ToString(format), "4");
            decimal result = new NumberPrompt("Count") { Integer = true, Culture = format }.Show(new InteractiveOptions {
                Mode = InteractionMode.Line, LineConsole = lines, Labels = labels
            });
            Assert.Equal(4m, result);
            Assert.Contains(labels.NotAWholeNumber, lines.Text);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("pl-PL")]
        public void NumberPrompt_WholeScaledDefaultAndCultureGroupingRemainValid(string culture) {
            CultureInfo format = CultureInfo.GetCultureInfo(culture);
            NumberPrompt prompt = new NumberPrompt("Count") { Default = 4.000m, Integer = true, Culture = format };
            RecordingSurface surface = new RecordingSurface(80, 16, false);
            Assert.Equal(4m, PromptHarness.Guard(surface, () => prompt.Show(PromptHarness.Options("Enter", surface))));
            PromptModeTests.FakeLines lines = new PromptModeTests.FakeLines("1" + format.NumberFormat.NumberGroupSeparator + "234");
            Assert.Equal(1234m, prompt.Show(new InteractiveOptions {
                Mode = InteractionMode.Line, LineConsole = lines, Labels = new PromptLabels()
            }));
        }
    }
}
