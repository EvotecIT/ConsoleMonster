using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>The <see cref="ILineConsole"/> over <see cref="Console"/>: standard input and output.</summary>
    public sealed class ConsoleLineConsole : ILineConsole {
        /// <summary>The shared instance.</summary>
        public static ConsoleLineConsole Instance { get; } = new ConsoleLineConsole();

        /// <inheritdoc />
        public void Write(string text) => Console.Out.Write(text);

        /// <inheritdoc />
        public void WriteLine(string text) => Console.Out.WriteLine(text);

        /// <inheritdoc />
        public string? ReadLine() => Console.In.ReadLine();

        /// <inheritdoc />
        public string? ReadSecret() {
            bool redirected;
            try {
                redirected = Console.IsInputRedirected;
            } catch (System.IO.IOException) {
                redirected = true;
            }
            if (redirected) {
                return Console.In.ReadLine();
            }
            StringBuilder sb = new StringBuilder();
            while (true) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) {
                    Console.Out.WriteLine();
                    return sb.ToString();
                }
                if (key.Key == ConsoleKey.Backspace) {
                    if (sb.Length > 0) {
                        sb.Length--;
                    }
                    continue;
                }
                if (key.KeyChar >= ' ') {
                    sb.Append(key.KeyChar);
                }
            }
        }

        /// <inheritdoc />
        public int? Choose(string caption, string message, IReadOnlyList<string> choices, int defaultIndex) => null;
    }
}
