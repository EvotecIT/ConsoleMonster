using System;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using OfficeIMO.Markdown;

namespace ConsoleMonster.Markdown {
    /// <summary>Maps the parser's typed inline model to literal rich text with nested styles and link targets.</summary>
    internal static class MarkdownInlines {
        internal static RichText Read(InlineSequence sequence) {
            RichTextBuilder result = new RichTextBuilder();
            Append(result, sequence, default);
            return result.Build();
        }

        private static void Append(RichTextBuilder result, InlineSequence sequence, RichStyle style) {
            foreach (IMarkdownInline node in sequence.Nodes) {
                switch (node) {
                    case BoldSequenceInline bold:
                        Append(result, bold.Inlines, style.With(TextDecoration.Bold));
                        break;
                    case ItalicSequenceInline italic:
                        Append(result, italic.Inlines, style.With(TextDecoration.Italic));
                        break;
                    case BoldItalicSequenceInline both:
                        Append(result, both.Inlines, style.With(TextDecoration.Bold | TextDecoration.Italic));
                        break;
                    case LinkInline link:
                        if (link.LabelInlines != null) {
                            Append(result, link.LabelInlines, style.WithLink(link.Url));
                        } else {
                            result.Append(link.Text, style.WithLink(link.Url));
                        }
                        break;
                    case ImageInline image:
                        result.Append(MarkdownMessages.Format("Image", image.PlainAlt), style.WithLink(image.Src));
                        break;
                    case CodeSpanInline code:
                        result.Append(code.Text, style.WithForeground(ThemeToken.Accent));
                        break;
                    case HardBreakInline _:
                        result.Append("\n", style);
                        break;
                    case SoftBreakInline _:
                        result.Append(" ", style);
                        break;
                    case HtmlRawInline html:
                        result.Append(html.Html, style);
                        break;
                    case ILiteralTextMarkdownInline literal:
                        result.Append(literal.Text, style);
                        break;
                    default:
                        throw new NotSupportedException(MarkdownMessages.Format("UnsupportedInline", node.GetType().Name));
                }
            }
        }
    }
}
