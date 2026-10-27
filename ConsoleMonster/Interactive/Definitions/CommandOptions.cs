using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>Configuration for an action whose identity and callback become immutable at Build.</summary>
    public sealed class CommandOptions {
        /// <summary>The stable command key.</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>The visible label; null uses Key.</summary>
        public string? Label { get; set; }
        /// <summary>The asynchronous operation over captured rows and cancellation.</summary>
        public Func<AppActionContext, CancellationToken, Task>? Action { get; set; }
        /// <summary>The optional keyboard shortcut.</summary>
        public KeyChord? Shortcut { get; set; }
        /// <summary>The optional confirmation prompt.</summary>
        public string? ConfirmPrompt { get; set; }
        /// <summary>Shows the shared operation progress and summary dialogs.</summary>
        public bool ProgressDialog { get; set; }
    }
}
