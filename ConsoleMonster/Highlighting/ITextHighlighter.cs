using System.Collections.Generic;

namespace ConsoleMonster.Highlighting {
    /// <summary>
    /// Turns text into tokens with highlight roles. Text between tokens is plain. Implementations read the text as
    /// literal characters (never markup) and must finish in time linear in its length; register one with
    /// <see cref="Highlighters.Register"/> to add a language.
    /// </summary>
    public interface ITextHighlighter {
        /// <summary>The language name, for example "json" (lower case).</summary>
        string Name { get; }

        /// <summary>Tokenizes text.</summary>
        /// <param name="text">The text (any line endings).</param>
        /// <returns>The tokens, in order and not overlapping.</returns>
        IReadOnlyList<HighlightToken> Tokenize(string text);
    }

    /// <summary>A highlighted part of a text.</summary>
    public readonly struct HighlightToken {
        /// <summary>Creates a token.</summary>
        /// <param name="start">The first character.</param>
        /// <param name="length">The length in characters.</param>
        /// <param name="role">The role.</param>
        /// <param name="link">A link target (for URLs), or null.</param>
        public HighlightToken(int start, int length, HighlightRole role, string? link = null) {
            Start = start;
            Length = length;
            Role = role;
            Link = link;
        }

        /// <summary>The first character.</summary>
        public int Start { get; }

        /// <summary>The length in characters.</summary>
        public int Length { get; }

        /// <summary>The role.</summary>
        public HighlightRole Role { get; }

        /// <summary>A link target, or null.</summary>
        public string? Link { get; }
    }

    /// <summary>What a piece of highlighted text is; each role maps to a theme role (see <see cref="HighlightStyles"/>).</summary>
    public enum HighlightRole {
        /// <summary>Plain text.</summary>
        Text = 0,

        /// <summary>A keyword (if, function, SELECT).</summary>
        Keyword,

        /// <summary>A string literal.</summary>
        String,

        /// <summary>A number.</summary>
        Number,

        /// <summary>true, false, null and similar constants.</summary>
        Literal,

        /// <summary>A comment.</summary>
        Comment,

        /// <summary>A key or property name (JSON keys, YAML keys, INI keys, XML attributes).</summary>
        Key,

        /// <summary>Brackets, commas, colons and other punctuation.</summary>
        Punctuation,

        /// <summary>An operator.</summary>
        Operator,

        /// <summary>A type name or a command (C# types, PowerShell cmdlets, XML tags).</summary>
        Type,

        /// <summary>A variable.</summary>
        Variable,

        /// <summary>Something wrong (an error level, an invalid token).</summary>
        Error,

        /// <summary>A warning level.</summary>
        Warning,

        /// <summary>An informational level.</summary>
        Info,

        /// <summary>A line added in a diff.</summary>
        Added,

        /// <summary>A line removed in a diff.</summary>
        Removed,

        /// <summary>A diff hunk header or a section header.</summary>
        Header,

        /// <summary>A time stamp, date or time.</summary>
        Timestamp,

        /// <summary>A URL or e-mail address (rendered as a link).</summary>
        Link,

        /// <summary>An identifier value: a GUID, an IP address, a distinguished name, a path.</summary>
        Identifier,

        /// <summary>Emphasis (Markdown).</summary>
        Emphasis,

        /// <summary>Strong emphasis (Markdown).</summary>
        Strong,

        /// <summary>A heading (Markdown).</summary>
        Heading
    }
}
