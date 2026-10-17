using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Output {
    /// <summary>
    /// Where console output goes and where input comes from: the capability profile (size, colour depth, glyph tier,
    /// culture and language), the theme, the writer and the input source. Every write, live session, prompt and status
    /// helper resolves one: an explicit target (<see cref="Live.LiveSessionOptions"/>, <see cref="Interactive.InteractiveOptions"/>,
    /// the <see cref="TerminalTargetExtensions"/> methods) or the ambient <see cref="Terminal.Target"/>.
    /// </summary>
    /// <remarks>
    /// Built-ins: <see cref="ConsoleTarget"/> (the process console, the default), <see cref="RecordingTarget"/> (in memory,
    /// for tests; see <see cref="Testing.TestTerminal.Target"/>), <see cref="WriterTarget"/> (a <see cref="System.IO.TextWriter"/>
    /// with a chosen tier and width) and <see cref="NullTarget"/> (discards everything). Implement it to host ConsoleMonster
    /// output elsewhere, for example in a log window.
    /// </remarks>
    public interface ITerminalTarget {
        /// <summary>A short description for diagnostics, for example <c>console</c> or <c>recording 80x24</c>.</summary>
        string Name { get; }

        /// <summary>The capability profile for a write happening now (the console's refreshes its size on every call).</summary>
        TerminalProfile Profile { get; }

        /// <summary>The theme, or null to choose one from the profile (see <see cref="Terminal.ResolveTheme"/>).</summary>
        ITheme? Theme { get; }

        /// <summary>
        /// The writer that receives the output, or null for the process console. The target owns it: callers write and
        /// flush but never dispose it.
        /// </summary>
        ITerminalWriter? Writer { get; }

        /// <summary>
        /// The input for prompts, menus and forms, or null. On the console null means the keyboard; on any other target it
        /// means nobody can answer, so prompts use their defaults or explain what is missing.
        /// </summary>
        IInputSource? Input { get; }
    }
}
