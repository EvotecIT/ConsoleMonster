using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using OfficeIMO.Markdown;

namespace ConsoleMonster.Markdown {
    /// <summary>Reads CommonMark through OfficeIMO and composes native console widgets without executing document content.</summary>
    public static class MarkdownConsole {
        /// <summary>Parses CommonMark with pipe tables into a reusable, width-responsive console document.</summary>
        /// <param name="content">The complete source, including any reference-link definitions.</param>
        /// <returns>A view that can be written, composed, exported or passed to <see cref="WidgetPager"/>.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <remarks>Raw HTML stays literal. Images become labelled links; destinations are never fetched.</remarks>
        public static MarkdownView Parse(string content) {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            MarkdownReaderOptions options = MarkdownReaderOptions.CreateCommonMarkProfile();
            options.Tables = true;
            // Keep destinations visible. ConsoleMonster's capability and hyperlink policy own OSC 8 emission.
            options.DisallowScriptUrls = false;
            options.DisallowFileUrls = false;
            options.RestrictUrlSchemes = false;
            return ConvertBlocks(MarkdownReader.Parse(content, options).Blocks);
        }

        /// <summary>Writes a document, using the shared pager when it exceeds an interactive screen.</summary>
        /// <param name="content">The complete Markdown source.</param>
        /// <param name="paging">Whether a long interactive document may acquire the input lease.</param>
        /// <param name="cancellationToken">Cancels paging and restores the terminal before control returns.</param>
        /// <remarks>Uses the current Terminal target, profile and theme. Redirected and non-interactive output is complete static text.</remarks>
        public static void Show(string content, bool paging = true, CancellationToken cancellationToken = default) {
            MarkdownView view = Parse(content);
            cancellationToken.ThrowIfCancellationRequested();
            if (!paging || !WidgetPager.TryShow(view, MarkdownMessages.Get("Title"), cancellationToken: cancellationToken)) {
                Terminal.Write(view);
            }
        }

        private static MarkdownView ConvertBlocks(IEnumerable<IMarkdownBlock> blocks) {
            MarkdownView view = new MarkdownView();
            foreach (IMarkdownBlock block in blocks) {
                switch (block) {
                    case HeadingBlock heading:
                        view.AddHeading(MarkdownInlines.Read(heading.Inlines), heading.Level);
                        break;
                    case ParagraphBlock paragraph:
                        view.AddParagraph(MarkdownInlines.Read(paragraph.Inlines));
                        break;
                    case CodeBlock code:
                        view.AddCode(code.Content, code.Language);
                        break;
                    case QuoteBlock quote:
                        view.AddQuote(ConvertBlocks(quote.ChildBlocks));
                        break;
                    case OrderedListBlock ordered:
                        AddList(view, ordered.Items, ordered.Start);
                        break;
                    case UnorderedListBlock unordered:
                        AddList(view, unordered.Items, null);
                        break;
                    case TableBlock table:
                        AddTable(view, table);
                        break;
                    case HorizontalRuleBlock _:
                        view.AddRule();
                        break;
                    case HtmlRawBlock html:
                        view.AddParagraph(RichText.Plain(html.Html));
                        break;
                    case HtmlCommentBlock comment:
                        view.AddParagraph(RichText.Plain("<!--" + comment.Comment + "-->"));
                        break;
                    default:
                        throw new NotSupportedException(MarkdownMessages.Format("UnsupportedBlock", block.GetType().Name));
                }
            }
            return view;
        }

        private static void AddList(MarkdownView view, List<ListItem> items, int? start) =>
            view.AddList(items.Select(item => ConvertBlocks(item.ChildBlocks)), start, !items.Any(item => item.ForceLoose));

        private static void AddTable(MarkdownView view, TableBlock table) {
            RichText[] headers = table.HeaderInlines.Select(MarkdownInlines.Read).ToArray();
            IEnumerable<IReadOnlyList<RichText>> rows = table.RowInlines.Select(row =>
                (IReadOnlyList<RichText>)row.Select(MarkdownInlines.Read).ToArray());
            HorizontalAlignment?[] alignments = table.Alignments.Select(alignment => alignment switch {
                ColumnAlignment.Left => (HorizontalAlignment?)HorizontalAlignment.Left,
                ColumnAlignment.Center => HorizontalAlignment.Center,
                ColumnAlignment.Right => HorizontalAlignment.Right,
                _ => null
            }).ToArray();
            view.AddTable(headers, rows, alignments);
        }
    }
}
