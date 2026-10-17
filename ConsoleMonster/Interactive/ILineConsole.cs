using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Line-based input and output for <see cref="InteractionMode.Line"/> and <see cref="InteractionMode.Accessible"/>
    /// prompts: the console by default, or a PowerShell host (<c>Host.UI.ReadLine</c>, <c>PromptForChoice</c>) in remoting
    /// and the ISE.
    /// </summary>
    public interface ILineConsole {
        /// <summary>Writes text without ending the line (the question before an answer).</summary>
        /// <param name="text">The text.</param>
        void Write(string text);

        /// <summary>Writes a line.</summary>
        /// <param name="text">The text.</param>
        void WriteLine(string text);

        /// <summary>Reads a line; null when the input ended.</summary>
        /// <returns>The line.</returns>
        string? ReadLine();

        /// <summary>Reads a line without echoing it; null when the input ended.</summary>
        /// <returns>The secret.</returns>
        string? ReadSecret();

        /// <summary>
        /// Lets the host ask a choice question natively (PowerShell's <c>PromptForChoice</c>). Return null to fall back to a
        /// numbered list.
        /// </summary>
        /// <param name="caption">The question.</param>
        /// <param name="message">Extra text, or empty.</param>
        /// <param name="choices">The choice labels.</param>
        /// <param name="defaultIndex">The default choice, or -1.</param>
        /// <returns>The chosen index, or null when not supported.</returns>
        int? Choose(string caption, string message, IReadOnlyList<string> choices, int defaultIndex);
    }
}
