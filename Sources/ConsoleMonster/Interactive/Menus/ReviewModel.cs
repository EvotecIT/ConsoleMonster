using System;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// What a review item shows: the app's current choices as label/value rows, the equivalent command line, problems that
    /// block running, and the run action. Built fresh each time the review is drawn, so it always matches the choices.
    /// </summary>
    public sealed class ReviewModel {
        /// <summary>Creates a review.</summary>
        /// <param name="title">A heading, or null for the item title.</param>
        public ReviewModel(string? title = null) {
            Title = title;
        }

        /// <summary>A heading.</summary>
        public string? Title { get; set; }

        /// <summary>A sentence under the heading.</summary>
        public string? Description { get; set; }

        /// <summary>The choices as label/value rows.</summary>
        public IList<KeyValuePair<string, string>> Rows { get; } = new List<KeyValuePair<string, string>>();

        /// <summary>The equivalent command line, or null.</summary>
        public string? Command { get; set; }

        /// <summary>Problems that block running ("Pick at least one target").</summary>
        public IList<string> Problems { get; } = new List<string>();

        /// <summary>The run action.</summary>
        public Action<MenuContext>? Run { get; set; }

        /// <summary>The caption of the run button; null uses "Run".</summary>
        public string? RunText { get; set; }

        /// <summary>Adds a row.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <returns>The review.</returns>
        public ReviewModel Row(string label, string? value) {
            Rows.Add(new KeyValuePair<string, string>(label, value ?? string.Empty));
            return this;
        }

        /// <summary>Adds a problem that blocks running.</summary>
        /// <param name="problem">The problem.</param>
        /// <returns>The review.</returns>
        public ReviewModel Problem(string problem) {
            Problems.Add(problem);
            return this;
        }
    }
}
