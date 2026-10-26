using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>The typed outcome of a modal form. Value is supplied only when Accepted is true.</summary>
    public sealed class DialogResult<T> {
        internal DialogResult(bool accepted, T? value) { Accepted = accepted; Value = value; }
        /// <summary>Whether the user accepted the form.</summary>
        public bool Accepted { get; }
        /// <summary>The typed accepted value, or default after cancellation.</summary>
        public T? Value { get; }
    }

    /// <summary>A modal form composed from ordinary named inputs, with validation and a typed result.</summary>
    public sealed class FormDialog<T> {
        /// <summary>Creates a form. The result callback runs on the app thread after validation.</summary>
        public FormDialog(string title, IWidget content, Func<AppState, T> result) {
            Title = title ?? string.Empty;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
        /// <summary>The visible dialog title.</summary>
        public string Title { get; }
        /// <summary>The retained form fields.</summary>
        public IWidget Content { get; }
        /// <summary>Maps committed field values to the result.</summary>
        public Func<AppState, T> Result { get; }
        /// <summary>Returns an error to keep the form open, or null to accept.</summary>
        public Func<AppState, string?>? Validate { get; set; }
        /// <summary>The affirmative button label.</summary>
        public string AcceptText { get; set; } = "OK";
        /// <summary>The cancellation button label.</summary>
        public string CancelText { get; set; } = "Cancel";
        /// <summary>Whether Enter initially chooses the affirmative button. False defaults to Cancel.</summary>
        public bool DefaultAccept { get; set; }
        /// <summary>Optional field key to focus initially instead of the default button.</summary>
        public string? InitialFocus { get; set; }
        /// <summary>Maximum dialog width.</summary>
        public int MaxWidth { get; set; } = 64;
    }
}
