using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A document the viewer can navigate: an optional header card, then one or more parts (shown as tabs when there are
    /// several), each a run of sections. A section has a heading row that folds its body and every deeper section after it.
    /// </summary>
    internal sealed class DocumentModel {
        /// <summary>The link scheme of a command's help; the viewer asks its link resolver for the target.</summary>
        internal const string HelpScheme = "cm-help:";

        internal DocumentModel(string title) { Title = title ?? string.Empty; }

        internal string Title { get; }

        /// <summary>Fixed above the parts: the synopsis and badges of a command's help.</summary>
        internal IWidget? Header { get; set; }

        internal List<DocumentPart> Parts { get; } = new List<DocumentPart>();

        /// <summary>
        /// The structure of a reading view: Markdown with headings and a command's help have one; anything else returns null
        /// and is paged as before.
        /// </summary>
        internal static DocumentModel? From(IWidget content, string title, RenderContext context, bool openCommands) {
            switch (content) {
                case MarkdownView markdown: return FromMarkdown(markdown, title);
                case HelpView help: return help.Document(context, openCommands);
                default: return null;
            }
        }

        private static DocumentModel? FromMarkdown(MarkdownView markdown, string title) {
            IReadOnlyList<MarkdownView.Heading> headings = markdown.Headings;
            if (headings.Count == 0) { return null; }
            IReadOnlyList<IWidget> blocks = markdown.Children;
            DocumentPart part = new DocumentPart(title);
            Slugs slugs = new Slugs();
            if (headings[0].Block > 0) { part.Sections.Add(new DocumentSection(string.Empty, 0, string.Empty, Body(blocks, 0, headings[0].Block))); }
            for (int i = 0; i < headings.Count; i++) {
                MarkdownView.Heading heading = headings[i];
                int end = i + 1 < headings.Count ? headings[i + 1].Block : blocks.Count;
                string text = heading.Text.PlainText.Trim();
                part.Sections.Add(new DocumentSection(text, heading.Level, slugs.Next(text), Body(blocks, heading.Block + 1, end)));
            }
            DocumentModel model = new DocumentModel(title);
            model.Parts.Add(part);
            return model;
        }

        private static IWidget? Body(IReadOnlyList<IWidget> blocks, int start, int end) {
            if (end <= start) { return null; }
            Rows rows = new Rows { Gap = 1 };
            for (int i = start; i < end; i++) { rows.Add(blocks[i]); }
            return rows;
        }

        /// <summary>A model of one widget without structure, such as a list of help topics a link led to.</summary>
        internal static DocumentModel Plain(IWidget content, string title) {
            DocumentModel model = new DocumentModel(title);
            DocumentPart part = new DocumentPart(title);
            part.Sections.Add(new DocumentSection(string.Empty, 0, string.Empty, content));
            model.Parts.Add(part);
            return model;
        }

        /// <summary>
        /// GitHub-style heading anchors: lower case, punctuation dropped, spaces as hyphens, and a numeric suffix for repeats,
        /// so <c>[Install](#install)</c> links work as they do on the web.
        /// </summary>
        internal sealed class Slugs {
            private readonly Dictionary<string, int> _seen = new Dictionary<string, int>(StringComparer.Ordinal);

            internal string Next(string text) {
                string slug = Slug(text);
                if (_seen.TryGetValue(slug, out int count)) {
                    _seen[slug] = count + 1;
                    return slug + "-" + (count + 1).ToString(CultureInfo.InvariantCulture);
                }
                _seen[slug] = 0;
                return slug;
            }

            internal static string Slug(string text) {
                StringBuilder slug = new StringBuilder(text.Length);
                foreach (char c in text.Trim().ToLowerInvariant()) {
                    if (char.IsLetterOrDigit(c) || c == '-' || c == '_') { slug.Append(c); }
                    else if (c == ' ') { slug.Append('-'); }
                }
                return slug.ToString();
            }
        }
    }

    /// <summary>A top-level part of a document; several parts are shown as tabs.</summary>
    internal sealed class DocumentPart {
        internal DocumentPart(string title) { Title = title ?? string.Empty; }

        internal string Title { get; }

        internal List<DocumentSection> Sections { get; } = new List<DocumentSection>();
    }

    /// <summary>A heading and the content up to the next heading. Level 0 is content without a heading, which never folds.</summary>
    internal sealed class DocumentSection {
        internal DocumentSection(string title, int level, string anchor, IWidget? body) {
            Title = title ?? string.Empty;
            Level = level;
            Anchor = anchor ?? string.Empty;
            Body = body;
        }

        internal string Title { get; }

        internal int Level { get; }

        internal string Anchor { get; }

        internal IWidget? Body { get; }

        /// <summary>Whether the section starts folded (examples, parameter details).</summary>
        internal bool StartFolded { get; set; }

        /// <summary>Muted text after the heading while folded, such as a parameter's type and summary.</summary>
        internal string? Summary { get; set; }

        /// <summary>What the palette lists the section as ("Parameter"); null uses the part's title.</summary>
        internal string? Category { get; set; }
    }
}
