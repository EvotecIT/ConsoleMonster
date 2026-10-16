using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {

    /// <summary>Chooses WriteConsoleW for Windows consoles and UTF-8 standard output otherwise.</summary>
    public static class TerminalWriterFactory {

        /// <summary>Creates the writer for a profile.</summary>
        /// <param name="profile">The profile.</param>
        /// <returns>A writer the caller owns.</returns>
        public static ITerminalWriter Create(TerminalProfile profile) {
            if (profile == null) {
                throw new ArgumentNullException(nameof(profile));
            }
            return WindowsConsole.IsWindows && profile.IsConsoleHandle
                ? (ITerminalWriter)new ConsoleHandleWriter()
                : StreamTerminalWriter.ForStandardOutput();
        }
    }
}
