using System;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// While the document viewer lays out a document, links to a heading in it (<c>#install</c>) keep their destination as a
    /// cell link, so the viewer can find and follow them. Terminal output never sees them: the viewer removes these links
    /// before drawing, and outside this scope Markdown resolves links through the usual hyperlink safety rules.
    /// </summary>
    internal static class DocumentLinks {
        [ThreadStatic] private static int _depth;

        internal static bool Active => _depth > 0;

        internal static IDisposable Collect() { _depth++; return new Scope(); }

        /// <summary>The destination the viewer follows itself, or null for one the terminal owns.</summary>
        internal static string? Internal(string? link) => Active && link != null && link.Length > 1 && link[0] == '#' ? link : null;

        private sealed class Scope : IDisposable {
            private bool _done;
            public void Dispose() { if (!_done) { _done = true; _depth--; } }
        }
    }
}