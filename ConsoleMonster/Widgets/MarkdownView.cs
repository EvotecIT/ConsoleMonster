using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Layout;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A semantic Markdown render target composed from the existing console widgets. It accepts already parsed blocks;
    /// it does not parse Markdown or claim CommonMark conformance. Parser adapters can populate this view without
    /// introducing a parser dependency into the console engine.
    /// </summary>
    public sealed class MarkdownView : IContainer {
        private readonly List<IWidget> _blocks = new List<IWidget>();

        /// <summary>The rendered blocks, in document order.</summary>
        public IReadOnlyList<IWidget> Children => _blocks.AsReadOnly();

        /// <summary>Adds a paragraph. Literal segments remain literal; links get a visible URL on terminals without OSC 8.</summary>
        /// <param name="text">Parsed inline content.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddParagraph(RichText text) => AddBlock(new MarkdownText(text));

        /// <summary>Adds a heading with a double rule for level one, a single rule for level two, and visible level markers below that.</summary>
        /// <param name="text">Parsed heading content.</param>
        /// <param name="level">The heading level, from one through six.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddHeading(RichText text, int level = 1) {
            if (level < 1 || level > 6) {
                throw new ArgumentOutOfRangeException(nameof(level));
            }
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            ThemeToken token = level <= 2 ? ThemeToken.Accent : level == 3 ? ThemeToken.Text : ThemeToken.Muted;
            RichText content = level <= 2 ? text : new RichTextBuilder().Append(new string('#', level) + " ").Append(text).Build();
            MarkdownText heading = new MarkdownText(content, new RichStyle(token, decoration: TextDecoration.Bold));
            return AddBlock(level <= 2
                ? new Rows(heading, new Rule { LineToken = ThemeToken.Accent, LineStyle = level == 1 ? BoxStyle.Double : (BoxStyle?)null })
                : (IWidget)heading);
        }

        /// <summary>Adds a block quote, retaining nested blocks and wrapping beside a muted vertical bar.</summary>
        /// <param name="content">The quote's parsed blocks.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddQuote(MarkdownView content) {
            RejectSelf(content);
            return AddBlock(new MarkdownPrefix(content, null));
        }

        /// <summary>Adds a list with hanging indents and nested block support.</summary>
        /// <param name="items">One parsed document per list item.</param>
        /// <param name="start">The first ordered-list number, or null for bullets.</param>
        /// <param name="tight">Whether to omit blank rows between items.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddList(IEnumerable<MarkdownView> items, int? start = null, bool tight = true) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            Rows rows = new Rows { Gap = tight ? 0 : 1 };
            long number = start ?? 0;
            foreach (MarkdownView item in items) {
                RejectSelf(item);
                string marker = start.HasValue ? (number++).ToString(CultureInfo.InvariantCulture) + ". " : "- ";
                rows.Add(new MarkdownPrefix(item, marker));
            }
            return AddBlock(rows);
        }

        /// <summary>Adds literal code with a language label and the existing registered highlighter, when available.</summary>
        /// <param name="text">The source code, without fence delimiters.</param>
        /// <param name="language">The fence language. Unknown languages display plain code with their original label.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddCode(string text, string? language = null) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            string label = string.IsNullOrWhiteSpace(language) ? "text" : language!.Trim();
            int separator = label.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
            if (separator >= 0) {
                label = label.Substring(0, separator);
            }
            return AddBlock(new CodeView(text, Highlighters.Get(label)) {
                Title = label,
                Wrap = true,
                MaxLines = int.MaxValue
            });
        }

        /// <summary>Adds a table using the console table's width negotiation, wrapping and compact record layouts.</summary>
        /// <param name="headers">Parsed header cells.</param>
        /// <param name="rows">Parsed body rows; each must have the same number of cells as the header.</param>
        /// <param name="alignments">Optional per-column alignment from the Markdown delimiter row.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddTable(IReadOnlyList<RichText> headers, IEnumerable<IReadOnlyList<RichText>> rows,
            IReadOnlyList<HorizontalAlignment?>? alignments = null) => AddBlock(new MarkdownTable(headers, rows, alignments));

        /// <summary>Adds a thematic break.</summary>
        /// <returns>This view.</returns>
        public MarkdownView AddRule() => AddBlock(new Rule());

        /// <summary>Adds a native widget as a parsed document block.</summary>
        /// <param name="block">The block. The caller must not subsequently introduce a layout cycle.</param>
        /// <returns>This view.</returns>
        public MarkdownView AddBlock(IWidget block) {
            if (block == null) {
                throw new ArgumentNullException(nameof(block));
            }
            if (ReferenceEquals(block, this)) {
                throw new ArgumentException(LabelText.Get("Document.Self"), nameof(block));
            }
            _blocks.Add(block);
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Layout().Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Layout().Render(region, context);

        private Rows Layout() => new Rows(_blocks.ToArray()) { Gap = 1 };

        private void RejectSelf(MarkdownView content) {
            if (ReferenceEquals(content, this)) {
                throw new ArgumentException(LabelText.Get("Document.Self"), nameof(content));
            }
        }
    }
}
