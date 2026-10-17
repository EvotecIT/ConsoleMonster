using System;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A prompt needed an answer in a session where nobody can answer (CI, redirected input, <c>-NonInteractive</c>) and has
    /// no default. The message names the parameter to pass, when the prompt knows it.
    /// </summary>
    public sealed class PromptUnavailableException : InvalidOperationException {
        /// <summary>Creates the exception.</summary>
        public PromptUnavailableException() : base("A prompt needs a value in a non-interactive session.") {
        }

        /// <summary>Creates the exception with a message.</summary>
        /// <param name="message">The message.</param>
        public PromptUnavailableException(string message) : base(message) {
        }

        /// <summary>Creates the exception with an inner exception.</summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The cause.</param>
        public PromptUnavailableException(string message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>The prompt title.</summary>
        public string? Prompt { get; internal set; }

        /// <summary>The parameter that would have supplied the value, if known.</summary>
        public string? Parameter { get; internal set; }
    }
}
