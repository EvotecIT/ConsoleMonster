using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>How a form ended.</summary>
    public sealed class FormResult {
        internal FormResult(bool submitted, FormValues values, string commandLine, string? preset) {
            Submitted = submitted;
            Values = values;
            CommandLine = commandLine;
            Preset = preset;
        }

        /// <summary>Whether the form was submitted (Run); false when cancelled.</summary>
        public bool Submitted { get; }

        /// <summary>The values.</summary>
        public FormValues Values { get; }

        /// <summary>The equivalent command line.</summary>
        public string CommandLine { get; }

        /// <summary>The preset that was last applied or saved, if any.</summary>
        public string? Preset { get; }

        /// <summary>The result of the run started from FormOptions.AfterSubmit, or null.</summary>
        public RunResult? RunResult { get; internal set; }
    }
}
