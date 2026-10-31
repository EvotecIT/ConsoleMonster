using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    public sealed class ImageCapabilityTests {
        [Theory]
        [InlineData("TERM=xterm-kitty", ImageProtocol.Kitty, "images: Kitty (TERM=xterm-kitty)")]
        [InlineData("KITTY_WINDOW_ID=1", ImageProtocol.Kitty, "images: Kitty (KITTY_WINDOW_ID)")]
        [InlineData("TERM_PROGRAM=ghostty", ImageProtocol.Kitty, "images: Kitty (TERM_PROGRAM=ghostty)")]
        [InlineData("TERM=xterm-ghostty", ImageProtocol.Kitty, "images: Kitty (TERM=xterm-ghostty)")]
        [InlineData("TERM_PROGRAM=iTerm.app", ImageProtocol.ITerm2, "images: ITerm2 (TERM_PROGRAM=iTerm.app)")]
        [InlineData("LC_TERMINAL=iTerm2", ImageProtocol.ITerm2, "images: ITerm2 (LC_TERMINAL=iTerm2)")]
        [InlineData("TERM_PROGRAM=WezTerm", ImageProtocol.ITerm2, "images: ITerm2 (TERM_PROGRAM=WezTerm)")]
        [InlineData("WT_SESSION=session", ImageProtocol.Sixel, "images: Sixel (WT_SESSION)")]
        [InlineData("TERM_PROGRAM=Apple_Terminal", ImageProtocol.HalfBlock, "images: HalfBlock (no image protocol advertised)")]
        [InlineData("VTE_VERSION=7802", ImageProtocol.HalfBlock, "images: HalfBlock (no image protocol advertised)")]
        // Inherited markers must not win: VS Code started from Windows Terminal, and Kitty or iTerm2 under tmux or screen.
        [InlineData("WT_SESSION=session;TERM_PROGRAM=vscode", ImageProtocol.HalfBlock, "images: HalfBlock (TERM_PROGRAM=vscode)")]
        [InlineData("TERM=xterm-kitty;TMUX=/tmp/tmux-1/default,1,0", ImageProtocol.HalfBlock, "images: HalfBlock (TMUX)")]
        [InlineData("TERM_PROGRAM=iTerm.app;STY=1.pts-0", ImageProtocol.HalfBlock, "images: HalfBlock (STY)")]
        [InlineData("TERM_PROGRAM=ghostty;ZELLIJ=0", ImageProtocol.HalfBlock, "images: HalfBlock (ZELLIJ)")]
        // The note explains the final protocol, not only what the terminal advertised.
        [InlineData("TERM=xterm-kitty;CI=true", ImageProtocol.None, "images: None (CI output gets the placeholder; terminal: Kitty, TERM=xterm-kitty)")]
        [InlineData("TERM=xterm-kitty;REDIRECTED", ImageProtocol.None, "images: None (redirected output gets the placeholder; terminal: Kitty, TERM=xterm-kitty)")]
        [InlineData("TERM=xterm-kitty;NO_COLOR=1", ImageProtocol.None, "images: None (no-colour output gets the placeholder; terminal: Kitty, TERM=xterm-kitty)")]
        [InlineData("TERM=xterm-kitty;CONSOLEMONSTER_COLOR=16", ImageProtocol.None, "images: None (16-colour output gets the placeholder; terminal: Kitty, TERM=xterm-kitty)")]
        [InlineData("TERM=xterm-kitty;CONSOLEMONSTER_MODE=plain", ImageProtocol.None, "images: None (plain mode gets the placeholder; terminal: Kitty, TERM=xterm-kitty)")]
        public void Detect_Terminal_ChoosesItsImageProtocolAndExplainsIt(string variables, ImageProtocol expected, string decision) {
            Dictionary<string, string> environment = new Dictionary<string, string> { ["TERM"] = "xterm-256color", ["COLORTERM"] = "truecolor" };
            bool redirected = false;
            foreach (string pair in variables.Split(';')) {
                string[] parts = pair.Split(new[] { '=' }, 2);
                if (parts[0] == "REDIRECTED") { redirected = true; continue; }
                environment[parts[0]] = parts[1];
            }
            TerminalProfile profile = new CapabilityDetector(new TerminalEnvironment(environment, isOutputRedirected: redirected)).Detect();
            Assert.Equal(expected, profile.ImageProtocol);
            Assert.Equal(decision, Assert.Single(profile.Decisions, note => note.StartsWith("images:")));
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
            // A chosen protocol that the output overrides says so, and the note follows every copy.
            Assert.Equal("images: None (redirected output gets the placeholder; terminal: Sixel, chosen with WithImageProtocol)", Assert.Single(redirected.Decisions, note => note.StartsWith("images:")));
            Assert.Equal("images: HalfBlock (pixels need interactive VT output; terminal: Kitty, chosen with WithImageProtocol)", Assert.Single(kitty.WithMode(OutputMode.Stream).Decisions, note => note.StartsWith("images:")));
            Assert.Equal("images: Kitty (chosen with WithImageProtocol)", Assert.Single(kitty.WithDecision("width: 80").Decisions, note => note.StartsWith("images:")));
        }
    }
}
