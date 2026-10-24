using System;
using ConsoleMonster.Clipboard;
using ConsoleMonster.Output;

namespace ConsoleMonster.Interactive {
    /// <summary>Binds command copying to the interaction's output, including flushed SSH clipboard requests.</summary>
    internal static class InteractiveClipboard {
        internal static Func<string, bool> ForTarget(ITerminalTarget target) => text =>
            new ClipboardService().TryCopy(text, out _, sequence => {
                if (target.Writer is ITerminalWriter writer) {
                    writer.Write(sequence); writer.Flush();
                } else {
                    // A live session may capture Console.Out. Use the actual terminal descriptor instead.
                    using (ITerminalWriter console = TerminalWriterFactory.Create(Terminal.GetTargetProfile(target))) {
                        console.Write(sequence); console.Flush();
                    }
                }
            });
    }
}
