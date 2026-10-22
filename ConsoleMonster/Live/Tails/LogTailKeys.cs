using System;
using ConsoleMonster.Input;

namespace ConsoleMonster.Live.Tails {
    internal static class LogTailKeys {
        internal static string? Operation(InputEvent input) {
            if (input.Is(ConsoleKey.PageUp)) { return "page-up"; }
            if (input.Is(ConsoleKey.UpArrow)) { return "up"; }
            if (input.Is(ConsoleKey.Home)) { return "home"; }
            if (input.Is(ConsoleKey.PageDown)) { return "page-down"; }
            if (input.Is(ConsoleKey.DownArrow)) { return "down"; }
            if (input.Is(ConsoleKey.End)) { return "follow"; }
            return null;
        }

        internal static bool Quit(InputEvent input) => input.IsChar('q') || input.IsChar('Q')
            || input.Is(ConsoleKey.Escape) || input.Is(ConsoleKey.C, KeyModifiers.Control);
    }
}
