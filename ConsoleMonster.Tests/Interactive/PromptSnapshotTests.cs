using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Snapshots render the same initial view as a real prompt without claiming its input device.</summary>
    [Collection("Live sessions")]
    public sealed class PromptSnapshotTests {
        [Fact]
        public void SnapshotKeepsItsInlineHeightLimitWhenWrittenAsAnOrdinaryWidget() {
            var profile = TerminalProfile.Fixed(80, 30);
            var prompt = new SelectPrompt<string>("Target", Enumerable.Range(1, 10).Select(n => "host-" + n));
            var widget = prompt.Snapshot(new InteractiveOptions { Profile = profile, MaxInlineHeight = 3 });
            Assert.Equal(3, Terminal.Render(widget, profile).Height);
        }

        [Theory]
        [InlineData("text")]
        [InlineData("multiline")]
        [InlineData("secret")]
        [InlineData("number")]
        [InlineData("path")]
        [InlineData("confirm")]
        [InlineData("typed")]
        [InlineData("select")]
        [InlineData("multi")]
        public void SnapshotMatchesInitialRichViewWithoutReadingInput(string kind) {
            switch (kind) {
                case "text": Check(new TextPrompt("Project") { Default = "orbit" }); break;
                case "multiline": Check(new TextAreaPrompt("Notes") { Default = "one\ntwo" }); break;
                case "secret": Check(new SecretPrompt("Token")); break;
                case "number": Check(new NumberPrompt("Port") { Default = 443 }); break;
                case "path": Check(new PathPrompt("Folder") { Default = "./orbit" }); break;
                case "confirm": Check(new ConfirmPrompt("Continue?", true)); break;
                case "typed": Check(new TypedConfirmPrompt("Remove?", "orbit")); break;
                case "select": Check(new SelectPrompt<string>("Target", new[] { "api", "worker" })); break;
                case "multi": Check(new MultiSelectPrompt<string>("Checks", new[] { "dns", "tls" })); break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void Check<T>(Prompt<T> prompt) {
            RecordingSurface surface = new RecordingSurface(80, 30, true);
            InteractiveOptions interactive = PromptHarness.Options("Escape", surface);
            using (IInputSource input = interactive.Input!) { prompt.TryShow(out _, interactive); }
            using ScriptedInput untouched = new ScriptedInput("x");
            using (InputLease lease = ConsoleInput.Acquire(untouched)) {
                InteractiveOptions options = new InteractiveOptions {
                    Input = untouched, NonInteractive = true, Mode = InteractionMode.None,
                    Profile = TerminalProfile.Fixed(80, 30, ColorDepth.None, GlyphTier.Full),
                    Theme = Theme.Dark, Labels = new PromptLabels()
                };
                var actual = Terminal.Render(prompt.Snapshot(options), options.Profile!, Theme.Dark);
                Assert.Equal(surface.Frames[0].ToPlainText(), actual.ToPlainText());
                Assert.Equal(1, untouched.Remaining);
            }
        }
    }
}
