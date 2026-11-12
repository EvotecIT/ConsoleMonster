using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using OfficeIMO.Markdown;

namespace ConsoleMonster.Markdown {
    /// <summary>Reads CommonMark through OfficeIMO and composes native console widgets without executing document content.</summary>
    public static class MarkdownConsole {
        /// <summary>Parses CommonMark with GitHub pipe tables, task lists and alerts into a reusable, width-responsive console document.</summary>
        /// <param name="content">The complete source, including any reference-link definitions.</param>
        /// <returns>A view that can be written, composed, exported or passed to <see cref="WidgetPager"/>.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <remarks>Raw HTML stays literal. Images become labelled links; destinations are never fetched. Leading YAML front matter is metadata and is not displayed.</remarks>
        public static MarkdownView Parse(string content) {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            MarkdownReaderOptions options = MarkdownReaderOptions.CreateCommonMarkProfile();
            options.Tables = true;
            options.TaskLists = true;
            options.FrontMatter = true; // Document blocks exclude the front matter, so metadata never prints as body text.
            options.Callouts = true;
            MarkdownReaderBuiltInExtensions.AddCallouts(options); // The CommonMark profile carries no block extensions.
            // Keep destinations visible. ConsoleMonster's capability and hyperlink policy own OSC 8 emission.
            options.DisallowScriptUrls = false;
            options.DisallowFileUrls = false;
            options.RestrictUrlSchemes = false;
            return ConvertBlocks(MarkdownReader.Parse(content, options).Blocks);
        }

        /// <summary>Parses Markdown source and adds its semantic blocks to a content builder.</summary>
        /// <param name="content">The layout being composed.</param>
        /// <param name="source">The complete Markdown document, including link definitions.</param>
        /// <returns>The same content builder.</returns>
        public static ContentBuilder Markdown(this ContentBuilder content, string source) => content.Add(Parse(source));

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
                        // With Graphics registered a Mermaid fence draws as a diagram; otherwise it stays readable source.
                        Diagram? diagram = Diagram.FenceParser?.Invoke(code.Language, code.Content);
                        if (diagram != null) view.AddBlock(diagram); else view.AddCode(code.Content, code.Language);
                        break;
                    case QuoteBlock quote:
                        view.AddQuote(ConvertBlocks(quote.ChildBlocks));
                        break;
                    case CalloutBlock callout:
                        AddCallout(view, callout);
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
            view.AddList(items.Select(item => ConvertBlocks(item.ChildBlocks)).ToList(),
                items.Select(item => item.IsTask ? item.Checked : (bool?)null).ToList(), start, !items.Any(item => item.ForceLoose));

        private static void AddCallout(MarkdownView view, CalloutBlock callout) {
            string kind = (callout.Kind ?? string.Empty).Trim().ToLowerInvariant();
            ThemeToken role = kind switch {
                "important" => ThemeToken.Accent,
                "warning" => ThemeToken.Warn,
                "caution" => ThemeToken.Fail,
                _ => ThemeToken.Info
            };
            // GitHub alerts carry no title of their own; a Docs-style "[!KIND] Title" keeps the author's title.
            RichText title = callout.TitleInlines != null && !string.IsNullOrWhiteSpace(callout.Title)
                ? MarkdownInlines.Read(callout.TitleInlines)
                : RichText.Plain(Label(kind));
            view.AddCallout(ConvertBlocks(callout.ChildBlocks), title, role);
        }

        // GitHub's five alert kinds have localised titles; any other kind keeps its own word.
        private static string Label(string kind) => kind switch {
            "note" => MarkdownMessages.Get("CalloutNote"),
            "tip" => MarkdownMessages.Get("CalloutTip"),
            "important" => MarkdownMessages.Get("CalloutImportant"),
            "warning" => MarkdownMessages.Get("CalloutWarning"),
            "caution" => MarkdownMessages.Get("CalloutCaution"),
            _ => kind.Length == 0 ? MarkdownMessages.Get("CalloutNote") : char.ToUpperInvariant(kind[0]) + kind.Substring(1)
        };

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
