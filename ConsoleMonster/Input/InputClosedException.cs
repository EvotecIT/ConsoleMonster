using System;

namespace ConsoleMonster.Input {
    /// <summary>The input source has no more events (a script ran out, or standard input was closed).</summary>
    public sealed class InputClosedException : Exception {
        /// <summary>Creates the exception with the default message.</summary>
        public InputClosedException() : base("The input ended before the prompt was answered.") {
        }

        /// <summary>Creates the exception.</summary>
        /// <param name="message">The message.</param>
        public InputClosedException(string message) : base(message) {
        }

        /// <summary>Creates the exception with an inner exception.</summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The cause.</param>
        public InputClosedException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
