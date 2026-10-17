using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Runs prompts against scripted keys and a recording surface.</summary>
    internal static class PromptHarness {
        public static InteractiveOptions Options(string keys, RecordingSurface surface, GlyphTier tier = GlyphTier.Full, ColorDepth depth = ColorDepth.None) {
            return new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(keys) { Paced = true },
                Surface = surface,
                Profile = TerminalProfile.Fixed(surface.Width, surface.Height, depth, tier),
                Theme = Theme.Dark,
                Labels = new PromptLabels()
            };
        }

        public static T Run<T>(Prompt<T> prompt, string keys, out RecordingSurface surface, int width = 64, int height = 20, GlyphTier tier = GlyphTier.Full, bool inline = true) {
            surface = new RecordingSurface(width, height, inline);
            return prompt.Show(Options(keys, surface, tier));
        }

        // Runs an interaction; when the script ends early, fails with the last frame so the test shows where it stopped.
        public static T Guard<T>(RecordingSurface surface, System.Func<T> run) {
            try {
                return run();
            } catch (ConsoleMonster.Input.InputClosedException) {
                throw new Xunit.Sdk.XunitException("The script ended at this frame:\n" + surface.LastText());
            }
        }

        /// <summary>Every frame as text, separated by a rule (for golden files).</summary>
        public static string Frames(RecordingSurface surface, int last = int.MaxValue) {
            StringBuilder sb = new StringBuilder();
            int start = System.Math.Max(0, surface.Frames.Count - last);
            for (int i = start; i < surface.Frames.Count; i++) {
                sb.Append("--- frame ").Append(i).Append('\n');
                sb.Append(surface.Frames[i].ToPlainText().TrimEnd('\n')).Append('\n');
            }
            if (surface.Summary != null) {
                sb.Append("--- summary\n").Append(surface.SummaryText()).Append('\n');
            }
            return sb.ToString();
        }
    }
}
