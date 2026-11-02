using System;
using ConsoleMonster.Input;

namespace ConsoleMonster.Live.Tails {
    internal static class LogTailKeys {
        internal static bool Quit(InputEvent input) => input.IsChar('q') || input.IsChar('Q')
            || input.Is(ConsoleKey.Escape) || input.Is(ConsoleKey.C, KeyModifiers.Control);
    }
}
