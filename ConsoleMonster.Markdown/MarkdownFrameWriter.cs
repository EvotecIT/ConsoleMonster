using OfficeIMO.Markdown;

namespace ConsoleMonster.Markdown {
    /// <summary>Uses OfficeIMO's document model and safe fences for literal terminal content.</summary>
    internal static class MarkdownFrameWriter {
        internal static string Write(string text) {
            // Keep the established empty-frame representation without adding a blank content line.
            if (text.Length == 0) { return "```text\n```\n"; }
            // ToText includes its final newline; Code adds the line ending before its closing fence.
            string content = text.EndsWith("\n", System.StringComparison.Ordinal) ? text.Substring(0, text.Length - 1) : text;
            return MarkdownDoc.Create().Code("text", content).ToMarkdown().Replace("\r\n", "\n").TrimEnd('\n') + "\n";
        }
    }
}
