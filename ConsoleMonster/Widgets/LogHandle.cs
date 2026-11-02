using ConsoleMonster.Live;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="LogRegion"/>.</summary>
    public sealed class LogHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="widgetId">The log name.</param>
        public LogHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) {
        }

        /// <summary>Writes a line.</summary>
        /// <param name="text">The text.</param>
        /// <param name="level">The level: info, warn, err, verbose, debug, pass.</param>
        /// <param name="tag">The source of the line (a job, server or component), or null.</param>
        public void Write(string text, string level = "info", string? tag = null) => Post("write", key: tag, text: text, state: level);

        /// <summary>Writes a warning.</summary>
        /// <param name="text">The text.</param>
        public void Warn(string text) => Write(text, "warn");

        /// <summary>Writes an error.</summary>
        /// <param name="text">The text.</param>
        public void Error(string text) => Write(text, "err");
    }
}
