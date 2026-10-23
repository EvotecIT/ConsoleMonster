using System.Collections.Generic;

namespace ConsoleMonster.Highlighting {
    /// <summary>Highlights the literal CLR signatures returned by PowerShell Get-Member, including overloads and generic types.</summary>
    public sealed class MemberSignatureHighlighter : ITextHighlighter {
        private readonly RegexHighlighter _rules = new RegexHighlighter("member-signature")
            .Rule(@"\b[\w.]+(?:\[[\w.,\[\] ]*\])?(?=\s+[\w.]+)", HighlightRole.Type)
            .Rule(@"\b[\w.]+(?=\s*\()", HighlightRole.Identifier)
            .Rule(@"[(){}\[\],=]", HighlightRole.Punctuation);

        /// <inheritdoc />
        public string Name => "member-signature";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) => _rules.Tokenize(text);
    }
}
