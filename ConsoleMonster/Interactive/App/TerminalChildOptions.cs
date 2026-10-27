using System;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>A child process launched on a real PTY/ConPTY while an application suspends its screen.</summary>
    public sealed class TerminalChildOptions {
        /// <summary>Executable to launch, or null for the platform shell.</summary>
        public string? FileName { get; set; }
        /// <summary>Separate arguments, quoted by the existing terminal transport.</summary>
        public IList<string> Arguments { get; } = new List<string>();
        /// <summary>The child's current directory.</summary>
        public string WorkingDirectory { get; set; } = System.Environment.CurrentDirectory;
        /// <summary>Environment overrides passed only to the child.</summary>
        public IDictionary<string, string?> Environment { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);
        /// <summary>Maximum child lifetime. Cancellation terminates this owned child and restores the app.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(24);
    }
}
