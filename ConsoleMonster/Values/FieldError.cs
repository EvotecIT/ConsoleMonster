using System;

namespace ConsoleMonster.Values {
    /// <summary>A field whose getter failed: shown as a warning marker in its cell instead of dropping the record.</summary>
    public sealed class FieldError {
        /// <summary>Creates the marker.</summary>
        /// <param name="message">The getter's error message.</param>
        public FieldError(string message) {
            Message = message ?? string.Empty;
        }

        /// <summary>The getter's error message.</summary>
        public string Message { get; }

        /// <summary>The marker for a getter that failed with an exception (the innermost message).</summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The marker.</returns>
        public static FieldError From(Exception exception) {
            Exception inner = exception;
            while (inner.InnerException != null && (inner is System.Reflection.TargetInvocationException || inner.GetType().Name == "GetValueInvocationException" || inner.GetType().Name == "GetValueException")) {
                inner = inner.InnerException;
            }
            return new FieldError(inner.Message);
        }

        /// <inheritdoc />
        public override string ToString() => "error: " + Message;
    }

    /// <summary>A value that was not read because the shaping pass ran out of time or nodes.</summary>
    public sealed class BudgetExceeded {
        /// <summary>The single marker instance.</summary>
        public static readonly BudgetExceeded Instance = new BudgetExceeded();

        private BudgetExceeded() { }

        /// <inheritdoc />
        public override string ToString() => "…";
    }
}
