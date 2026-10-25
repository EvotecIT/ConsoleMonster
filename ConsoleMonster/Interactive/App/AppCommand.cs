using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>One passive action definition shared by buttons, palettes and grid actions.</summary>
    public sealed class AppCommand {
        /// <summary>Creates an asynchronous action. The callback runs outside the app thread and must use its context to report output.</summary>
        public AppCommand(string key, string label, Func<AppActionContext, CancellationToken, Task> action) {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("A command needs a key.", nameof(key)); }
            Key = key;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }
        /// <summary>Stable command identity, independent of the selected data row.</summary>
        public string Key { get; }
        /// <summary>Visible action text.</summary>
        public string Label { get; }
        /// <summary>Optional keyboard shortcut. Text editing retains precedence over plain character shortcuts.</summary>
        public KeyChord? Shortcut { get; set; }
        /// <summary>Optional modal confirmation text. {Selection} expands to the captured row label or row count. Its default answer is No.</summary>
        public string? ConfirmPrompt { get; set; }
        /// <summary>The operation, which receives captured rows and cooperative cancellation.</summary>
        public Func<AppActionContext, CancellationToken, Task> Action { get; }
        internal Func<IReadOnlyList<object?>, IReadOnlyList<object?>>? CaptureRows { get; set; }
        internal IReadOnlyList<object?> Capture(IReadOnlyList<object?> rows) => CaptureRows?.Invoke(rows) ?? Array.AsReadOnly(new List<object?>(rows).ToArray());
    }

    /// <summary>Captured action input and a thread-safe output sink. It deliberately exposes no controls or app state.</summary>
    public sealed class AppActionContext {
        private readonly Action<string, string> _write;
        internal AppActionContext(IReadOnlyList<object?> rows, Action<string, string> write) { Rows = rows; _write = write; }
        /// <summary>The row set captured before confirmation, in display order. Changing selection does not change this set.</summary>
        public IReadOnlyList<object?> Rows { get; }
        /// <summary>Appends literal output to the bounded embedded log. Levels use the existing log vocabulary.</summary>
        public void Write(string text, string level = "out") => _write(text ?? string.Empty, level);
    }
}
