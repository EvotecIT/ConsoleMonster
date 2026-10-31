using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    public sealed class ImageCapabilityTests {
        [Theory]
        [InlineData("TERM=xterm-kitty", ImageProtocol.Kitty)]
        [InlineData("KITTY_WINDOW_ID=1", ImageProtocol.Kitty)]
        [InlineData("TERM_PROGRAM=ghostty", ImageProtocol.Kitty)]
        [InlineData("TERM=xterm-ghostty", ImageProtocol.Kitty)]
        [InlineData("TERM_PROGRAM=iTerm.app", ImageProtocol.ITerm2)]
        [InlineData("LC_TERMINAL=iTerm2", ImageProtocol.ITerm2)]
        [InlineData("TERM_PROGRAM=WezTerm", ImageProtocol.ITerm2)]
        [InlineData("WT_SESSION=session", ImageProtocol.Sixel)]
        [InlineData("TERM_PROGRAM=Apple_Terminal", ImageProtocol.HalfBlock)]
        [InlineData("VTE_VERSION=7802", ImageProtocol.HalfBlock)]
        // Inherited markers must not win: VS Code started from Windows Terminal, and Kitty or iTerm2 under tmux or screen.
        [InlineData("WT_SESSION=session;TERM_PROGRAM=vscode", ImageProtocol.HalfBlock)]
        [InlineData("TERM=xterm-kitty;TMUX=/tmp/tmux-1/default,1,0", ImageProtocol.HalfBlock)]
        [InlineData("TERM_PROGRAM=iTerm.app;STY=1.pts-0", ImageProtocol.HalfBlock)]
        [InlineData("TERM_PROGRAM=ghostty;ZELLIJ=0", ImageProtocol.HalfBlock)]
        public void Detect_Terminal_ChoosesItsImageProtocol(string variables, ImageProtocol expected) {
            Dictionary<string, string> environment = new Dictionary<string, string> { ["TERM"] = "xterm-256color", ["COLORTERM"] = "truecolor" };
            foreach (string pair in variables.Split(';')) {
                string[] parts = pair.Split(new[] { '=' }, 2);
                environment[parts[0]] = parts[1];
            }
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(environment)).Detect();
            Assert.Equal(expected, profile.ImageProtocol);
            Assert.Contains(profile.Decisions, note => note.StartsWith("images: " + expected));
        }

        [Fact]
        public void Profile_ProtocolsNeedInteractiveVtWith256Colours_EvenWhenForced() {
            TerminalProfile kitty = TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor).WithImageProtocol(ImageProtocol.Kitty);
            Assert.Equal(ImageProtocol.Kitty, kitty.ImageProtocol);
            Assert.Equal(ImageProtocol.Kitty, kitty.WithColorDepth(ColorDepth.Ansi256).ImageProtocol);
            Assert.Equal(ImageProtocol.None, kitty.WithColorDepth(ColorDepth.Ansi16).ImageProtocol);
            Assert.Equal(ImageProtocol.None, kitty.WithColorDepth(ColorDepth.None).ImageProtocol);
            // Line output without cursor control (a format view, for example) still has colour cells for half blocks.
            Assert.Equal(ImageProtocol.HalfBlock, kitty.WithMode(OutputMode.Stream).ImageProtocol);
            Assert.Equal(ImageProtocol.None, kitty.WithMode(OutputMode.Plain).ImageProtocol);
            TerminalProfile ci = new CapabilityDetector(new TerminalEnvironment(new Dictionary<string, string> {
                ["TERM"] = "xterm-kitty", ["CI"] = "true", ["CONSOLEMONSTER_MODE"] = "interactive", ["CONSOLEMONSTER_COLOR"] = "truecolor"
            })).Detect();
            Assert.Equal(ImageProtocol.None, ci.WithImageProtocol(ImageProtocol.Kitty).ImageProtocol);
            TerminalProfile redirected = new TerminalProfile(ColorDepth.TrueColor, GlyphTier.Full, OutputMode.Interactive,
                supportsVirtualTerminal: true, isOutputRedirected: true).WithImageProtocol(ImageProtocol.Sixel);
            Assert.Equal(ImageProtocol.None, redirected.ImageProtocol);
        }
    }
}
