using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>The outcome of a run, shown by the menu afterwards: a status, a summary, key figures, details and next actions.</summary>
    public sealed class MenuRunResult {
        /// <summary>Creates a result.</summary>
        /// <param name="title">The title ("Checks finished").</param>
        /// <param name="status">The status.</param>
        public MenuRunResult(string title, StatusKind status = StatusKind.Pass) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Status = status;
        }

        /// <summary>The title.</summary>
        public string Title { get; set; }

        /// <summary>The status.</summary>
        public StatusKind Status { get; set; }

        /// <summary>A sentence under the title.</summary>
        public string? Summary { get; set; }

        /// <summary>Key figures shown as label/value pairs ("Passed" = "41").</summary>
        public IList<KeyValuePair<string, string>> Figures { get; } = new List<KeyValuePair<string, string>>();

        /// <summary>The status of each figure that has one (its number is coloured), by label.</summary>
        public IDictionary<string, StatusKind> FigureStatus { get; } = new Dictionary<string, StatusKind>(StringComparer.Ordinal);

        /// <summary>Detail content under the figures (a table, a list).</summary>
        public IWidget? Details { get; set; }

        /// <summary>Actions offered next ("Open report", "Run failed again", "Export").</summary>
        public IList<MenuItem> NextActions { get; } = new List<MenuItem>();

        /// <summary>
        /// How long the run took; left at zero, a run started with <see cref="MenuContext.Run"/>
        /// fills it in from the session clock (<see cref="ConsoleMonster.Live.LiveSessionOptions.Clock"/>), so a
        /// <see cref="ConsoleMonster.Live.SimulatedClock"/> gives its time.
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>The exception that ended the run, if any.</summary>
        public Exception? Error { get; set; }

        /// <summary>Adds a key figure.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <returns>The result.</returns>
        public MenuRunResult Figure(string label, string value) {
            Figures.Add(new KeyValuePair<string, string>(label, value));
            return this;
        }

        /// <summary>Adds a key figure whose number carries a status colour (use it only for figures that need attention).</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <param name="status">The status.</param>
        /// <returns>The result.</returns>
        public MenuRunResult Figure(string label, string value, StatusKind status) {
            Figures.Add(new KeyValuePair<string, string>(label, value));
            FigureStatus[label] = status;
            return this;
        }

        /// <summary>Adds a next action.</summary>
        /// <param name="title">The title.</param>
        /// <param name="action">The action.</param>
        /// <returns>The result.</returns>
        public MenuRunResult Next(string title, Action<MenuContext> action) {
            NextActions.Add(new MenuItem(title).Action(action));
            return this;
        }
    }
}
