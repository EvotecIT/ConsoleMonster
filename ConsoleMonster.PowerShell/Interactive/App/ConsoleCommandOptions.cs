using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Explicit PowerShell operation inputs shared with New-ConsoleCommand.</summary>
    public sealed class ConsoleCommandOptions {
        /// <summary>Stable action identity.</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>Visible label; null uses Key.</summary>
        public string? Label { get; set; }
        /// <summary>Optional key chord, such as Ctrl+R or F5.</summary>
        public string? Shortcut { get; set; }
        /// <summary>Optional modal confirmation text; No is the initial answer.</summary>
        public string? ConfirmPrompt { get; set; }
        /// <summary>Operation source, receiving the row followed by ArgumentList values in an isolated runspace.</summary>
        public ScriptBlock ScriptBlock { get; set; } = null!;
        /// <summary>Values serialized at definition time; closures are not captured.</summary>
        public object[] ArgumentList { get; set; } = Array.Empty<object>();
        /// <summary>Modules loaded in the operation's runspace.</summary>
        public string[] ImportModule { get; set; } = Array.Empty<string>();
    }
}
