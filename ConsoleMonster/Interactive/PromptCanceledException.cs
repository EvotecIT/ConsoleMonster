using System;

namespace ConsoleMonster.Interactive {
    /// <summary>The user cancelled a prompt (Esc where cancelling is allowed, or Ctrl+C).</summary>
    public sealed class PromptCanceledException : OperationCanceledException {
        /// <summary>Creates the exception.</summary>
        public PromptCanceledException() : base("The prompt was cancelled.") {
        }

        /// <summary>Creates the exception for a prompt.</summary>
        /// <param name="prompt">The prompt title.</param>
        public PromptCanceledException(string prompt) : base("The prompt '" + prompt + "' was cancelled.") {
            Prompt = prompt;
        }

        /// <summary>Creates the exception with an inner exception.</summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The cause.</param>
        public PromptCanceledException(string message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>The prompt title, when known.</summary>
        public string? Prompt { get; }
    }
}
