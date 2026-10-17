using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Live;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>The line path (stream and plain output, narration, captured lines) never passes escape sequences or controls through.</summary>
    public sealed class TextSanitizerTests {
        private static readonly string Esc = ((char)27).ToString();
        private static readonly string Bel = ((char)7).ToString();

        // A title change (OSC 0 ended by BEL), a hyperlink (OSC 8 ended by ST), a screen clear (CSI), a C1 CSI and a bell.
        private static string Hostile() {
            return "a" + Esc + "]0;PWNED" + Bel + "b" + Esc + "]8;;https://x" + Esc + "\\c" + Esc + "[2J" + Esc + "[31md" + ((char)0x9B).ToString() + "e" + Bel + "\tf";
        }

        [Fact]
        public void Clean_EscapeSequencesAndControls_AreRemoved() {
            Assert.Equal("abcde    f", TextSanitizer.Clean(Hostile()));
            Assert.Equal("plain text", TextSanitizer.Clean("plain text"));
            Assert.Equal("cut", TextSanitizer.Clean("cut" + Esc + "]0;never terminated"));
        }

        [Theory]
        [InlineData(OutputMode.Plain, ColorDepth.None)]
        [InlineData(OutputMode.Stream, ColorDepth.None)]
        public void LineOutput_InjectedSequences_WritesNoEscapes(OutputMode mode, ColorDepth depth) {
            StringTerminalWriter writer = new StringTerminalWriter();
            TerminalProfile profile = new TerminalProfile(depth, GlyphTier.Ascii, mode, 80, 24);
            using (ITerminalOutput output = Terminal.CreateOutput(profile, writer)) {
                output.WriteLine(StyledLine.From(Hostile()));
            }
            string text = writer.ToString();
            Assert.True(text.IndexOf((char)27) < 0, "plain and stream output must not contain ESC");
            Assert.True(text.IndexOf((char)7) < 0, "no BEL");
            Assert.StartsWith("abcde    f", text);
        }

        [Fact]
        public void VtOutput_InjectedSequences_KeepOnlyItsOwnStyling() {
            StringTerminalWriter writer = new StringTerminalWriter();
            TerminalProfile profile = TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor, GlyphTier.Full);
            using (ITerminalOutput output = Terminal.CreateOutput(profile, writer)) {
                output.WriteLine(StyledLine.From(Hostile()));
            }
            string text = writer.ToString();
            Assert.DoesNotContain("]0;", text);
            Assert.DoesNotContain("]8;", text);
            Assert.DoesNotContain(Esc + "[2J", text);
            Assert.True(text.IndexOf((char)7) < 0, "no BEL");
        }

        [Fact]
        public void StreamSession_LogEventWithEscapes_NarratesCleanText() {
            StringTerminalWriter writer = new StringTerminalWriter();
            TerminalProfile profile = new TerminalProfile(ColorDepth.None, GlyphTier.Ascii, OutputMode.Plain, 80, 24);
            LiveSessionOptions options = new LiveSessionOptions {
                Profile = profile,
                Output = Terminal.CreateOutput(profile, writer),
                CaptureConsole = false,
                HandleCancelKey = false,
                PrintFinalState = false
            };
            Widgets.LogRegion log = new Widgets.LogRegion();
            using (LiveSession session = new LiveSession(log, options)) {
                session.Start();
                session.Post(new LiveEvent("log", "write", text: "injected " + Hostile(), state: "warn"));
            }
            string text = writer.ToString();
            Assert.Contains("injected abcde    f", text);
            Assert.True(text.IndexOf((char)27) < 0, "plain output must not contain ESC");
        }
    }
}
