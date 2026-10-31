using ConsoleMonster.Capabilities;
using ConsoleMonster.Interop;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    public static partial class Terminal {
        /// <summary>
        /// Writes text that is already encoded for the terminal (an inline image's escape sequence) under the same gate as
        /// print-once widgets, so a concurrent write never lands inside it.
        /// </summary>
        /// <param name="text">The encoded text.</param>
        /// <param name="profile">The profile that chose the encoding; it picks the console writer when none is given.</param>
        /// <param name="writer">The sink, or null for the ambient target's writer or the console.</param>
        internal static void WriteEncoded(string text, TerminalProfile profile, ITerminalWriter? writer) {
            lock (WriteGate) {
                ITerminalWriter? target = writer ?? Target.Writer;
                ITerminalWriter output = target ?? TerminalWriterFactory.Create(profile);
                try {
                    using (WindowsOutputModeScope.Activate(output)) {
                        output.Write(text);
                        output.Flush();
                    }
                } finally {
                    if (target == null) { output.Dispose(); }
                }
            }
        }
    }
}
