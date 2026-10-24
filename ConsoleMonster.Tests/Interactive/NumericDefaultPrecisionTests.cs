using System;
using System.Globalization;
using ConsoleMonster.Interactive;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    public sealed class NumericDefaultPrecisionTests {
        [Theory]
        [InlineData("en-US")]
        [InlineData("pl-PL")]
        public void WholeNumberInput_StillAcceptsCurrentCultureGrouping(string culture) {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                Form form = new Form("Options") { Review = false };
                form.Number("Count").Integer = true;
                string grouped = "1" + CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator + "234";
                FormResult result = form.Show(new FormOptions { Interactive = new InteractiveOptions {
                    Mode = InteractionMode.Line, LineConsole = new PromptModeTests.FakeLines(grouped, "y"), Labels = new PromptLabels()
                } });
                Assert.True(result.Submitted);
                Assert.Equal(1234m, result.Values["Count"]);
            } finally { CultureInfo.CurrentCulture = previous; }
        }
        [Theory]
        [InlineData(false, "en-US")]
        [InlineData(true, "en-US")]
        [InlineData(false, "pl-PL")]
        [InlineData(true, "pl-PL")]
        public void WholeNumberDefault_WithRetainedDecimalScale_SubmitsUnchanged(bool lineMode, string culture) {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                Form form = new Form("Options") { Review = false, Command = "tool" };
                NumberField field = form.Number("Count");
                field.Default = 4.000m;
                field.Integer = true;
                field.Parameter = "Count";
                FormResult result;
                if (lineMode) {
                    result = form.Show(new FormOptions { Interactive = new InteractiveOptions {
                        Mode = InteractionMode.Line, LineConsole = new PromptModeTests.FakeLines(string.Empty, "y"), Labels = new PromptLabels()
                    } });
                } else {
                    RecordingSurface surface = new RecordingSurface(80, 24, false);
                    result = PromptHarness.Guard(surface, () => form.Show(new FormOptions {
                        Interactive = PromptHarness.Options("Down Enter", surface)
                    }));
                }
                Assert.True(result.Submitted);
                Assert.Equal(4m, result.Values["Count"]);
                Assert.Equal("tool", result.CommandLine);
            } finally { CultureInfo.CurrentCulture = previous; }
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("pl-PL")]
        public void WholeNumberInput_RejectsLocalizedFractionUntilCorrected(string culture) {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                Form form = new Form("Options") { Review = false };
                NumberField field = form.Number("Count");
                field.Default = 4m;
                field.Integer = true;
                PromptLabels labels = new PromptLabels();
                PromptModeTests.FakeLines lines = new PromptModeTests.FakeLines(4.5m.ToString(CultureInfo.CurrentCulture), "4", "y");
                FormResult result = form.Show(new FormOptions { Interactive = new InteractiveOptions {
                    Mode = InteractionMode.Line, LineConsole = lines, Labels = labels
                } });
                Assert.True(result.Submitted);
                Assert.Equal(4m, result.Values["Count"]);
                Assert.Contains(labels.NotAWholeNumber, lines.Text);
            } finally { CultureInfo.CurrentCulture = previous; }
        }
        [Theory]
        [InlineData(false, "en-US", "1.2345678901234")]
        [InlineData(true, "en-US", "1.2345678901234")]
        [InlineData(false, "pl-PL", "0.1234567890123456789012345678")]
        [InlineData(true, "pl-PL", "0.1234567890123456789012345678")]
        public void NumericDefaults_SubmitWithoutPrecisionLoss(bool lineMode, string culture, string value) {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                object initial = value.StartsWith("1.", StringComparison.Ordinal)
                    ? (object)double.Parse(value, CultureInfo.InvariantCulture)
                    : decimal.Parse(value, CultureInfo.InvariantCulture);
                decimal expected = Convert.ToDecimal(initial, CultureInfo.InvariantCulture);
                Form form = new Form("Options") { Review = false, Command = "tool" };
                NumberField field = form.Number("Rate");
                field.Default = initial;
                field.Parameter = "Rate";
                FormResult result;
                if (lineMode) {
                    result = form.Show(new FormOptions { Interactive = new InteractiveOptions {
                        Mode = InteractionMode.Line,
                        LineConsole = new PromptModeTests.FakeLines(string.Empty, "y"), Labels = new PromptLabels()
                    } });
                } else {
                    RecordingSurface surface = new RecordingSurface(80, 24, false);
                    result = PromptHarness.Guard(surface, () => form.Show(new FormOptions {
                        Interactive = PromptHarness.Options("Down Enter", surface)
                    }));
                }
                Assert.True(result.Submitted);
                Assert.Equal(expected, result.Values["Rate"]);
                Assert.Equal("tool", result.CommandLine);
            } finally {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
