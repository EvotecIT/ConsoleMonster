using System.Collections.Generic;

namespace ConsoleMonster.Capabilities {
    public sealed partial class CapabilityDetector {
        // The terminal's image protocol from its environment. TerminalProfile owns the output, CI, mode, VT and colour guards.
        // iTerm2 also accepts Kitty graphics but asks the user before showing them, so iTerm2 keeps its own protocol.
        private ImageProtocol DetectImages(List<string> decisions) {
            string term = Get("TERM");
            string program = Get("TERM_PROGRAM");
            ImageProtocol protocol;
            string reason;
            string multiplexer = Get("TMUX").Length > 0 ? "TMUX" : Get("STY").Length > 0 ? "STY" : Get("ZELLIJ").Length > 0 ? "ZELLIJ" : string.Empty;
            if (multiplexer.Length > 0) {
                // Multiplexers don't pass Kitty graphics through and redraw panes over Sixel, so images would vanish or smear.
                protocol = ImageProtocol.HalfBlock;
                reason = multiplexer;
            } else if (Equal(program, "vscode")) {
                // VS Code draws images only with terminal.integrated.enableImages, which the environment does not show.
                protocol = ImageProtocol.HalfBlock;
                reason = "TERM_PROGRAM=vscode";
            } else if (Contains(term, "kitty") || Get("KITTY_WINDOW_ID").Length > 0) {
                protocol = ImageProtocol.Kitty;
                reason = Contains(term, "kitty") ? "TERM=" + term : "KITTY_WINDOW_ID";
            } else if (Equal(program, "ghostty") || Contains(term, "ghostty")) {
                protocol = ImageProtocol.Kitty;
                reason = Equal(program, "ghostty") ? "TERM_PROGRAM=ghostty" : "TERM=" + term;
            } else if (Equal(program, "iTerm.app") || Equal(Get("LC_TERMINAL"), "iTerm2")) {
                // LC_TERMINAL is forwarded by SSH, so iTerm2 is recognised on remote hosts too.
                protocol = ImageProtocol.ITerm2;
                reason = Equal(program, "iTerm.app") ? "TERM_PROGRAM=iTerm.app" : "LC_TERMINAL=iTerm2";
            } else if (Equal(program, "WezTerm")) {
                protocol = ImageProtocol.ITerm2;
                reason = "TERM_PROGRAM=WezTerm";
            } else if (Get("WT_SESSION").Length > 0) {
                protocol = ImageProtocol.Sixel;
                reason = "WT_SESSION";
            } else {
                protocol = ImageProtocol.HalfBlock;
                reason = "no image protocol advertised";
            }
            decisions.Add("images: " + protocol + " (" + reason + "; pixels need interactive VT with 256 colours or more, never redirected or CI)");
            return protocol;
        }
    }
}
