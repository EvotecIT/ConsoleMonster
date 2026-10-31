namespace ConsoleMonster.Output {
    /// <summary>
    /// A terminal that ConsoleMonster drives: a target (profile, writer, input) that also reports its visible window, so live
    /// sessions, apps, prompts and menus redraw at the size the user sees. The process console (<see cref="ConsoleTarget"/>)
    /// is one driver; <see cref="Headless.HeadlessTerminal"/> runs the same code with no terminal at all, for CI, goldens,
    /// recordings, the browser host and the debug socket.
    /// </summary>
    /// <remarks>
    /// Output reaches the driver through <see cref="ITerminalTarget.Writer"/> exactly as it would reach a terminal (VT
    /// sequences included), and input comes from <see cref="ITerminalTarget.Input"/>. A window size change is reported by
    /// this method and announced to the reader with a resize input event.
    /// </remarks>
    public interface IConsoleDriver : ITerminalTarget {
        /// <summary>The visible window in cells: its position in the buffer and its size.</summary>
        /// <param name="left">The leftmost visible column of the buffer (zero when the driver has no scrollable buffer).</param>
        /// <param name="top">The topmost visible row of the buffer (zero when the driver has no scrollable buffer).</param>
        /// <param name="width">The window width in cells.</param>
        /// <param name="height">The window height in cells.</param>
        /// <returns>False when the size is unknown (for example, output is redirected); callers then use the profile's size.</returns>
        bool TryGetWindow(out int left, out int top, out int width, out int height);
    }
}
