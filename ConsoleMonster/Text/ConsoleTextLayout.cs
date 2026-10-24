using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>
    /// Lays out console text the way Write-Color does, for Write-ConsoleText and C# hosts alike: empty lines before and
    /// after, a prefix of tabs, spaces and an optional time stamp on the first line, text split at a separator with the
    /// following parts aligned under the text, wrapping that continues under the text, padding (left, right or centred,
    /// measured in terminal cells) and centring in the window.
    /// </summary>
    public sealed class ConsoleTextLayout {
        /// <summary>Tabs before the text (eight cells each).</summary>
        public int StartTab { get; set; }

        /// <summary>Spaces before the text.</summary>
        public int StartSpaces { get; set; }

        /// <summary>Empty lines before the text.</summary>
        public int LinesBefore { get; set; }

        /// <summary>Empty lines after the text.</summary>
        public int LinesAfter { get; set; }

        /// <summary>The time shown in brackets at the start, or null for none.</summary>
        public DateTime? Time { get; set; }

        /// <summary>The time format (default yyyy-MM-dd HH:mm:ss).</summary>
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>A separator the text is split at; the following parts are aligned under the first, after the prefix.</summary>
        public string? SplitLines { get; set; }

        /// <summary>Wraps long text at the width, continuing under the text after the prefix.</summary>
        public bool Wrap { get; set; }

        /// <summary>Centres each line in the width.</summary>
        public bool HorizontalCenter { get; set; }

        /// <summary>Pads each line on the left to this many cells (right-aligned).</summary>
        public int PadLeft { get; set; }

        /// <summary>Pads each line on the right to this many cells.</summary>
        public int PadRight { get; set; }

        /// <summary>Centres each line within this many cells.</summary>
        public int PadCenter { get; set; }

        /// <summary>The padding character (default a space).</summary>
        public char PadCharacter { get; set; } = ' ';

        /// <summary>The parts of a text after <see cref="SplitLines"/>.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The parts (one when there is no separator).</returns>
        public IReadOnlyList<RichText> Parts(RichText text) {
            return string.IsNullOrEmpty(SplitLines) ? new[] { text } : (IReadOnlyList<RichText>)text.Split(SplitLines!);
        }

        /// <summary>The lines of a text at a width.</summary>
        /// <param name="text">The text.</param>
        /// <param name="context">The render context.</param>
        /// <param name="width">The width for wrapping and centring.</param>
        /// <returns>The lines.</returns>
        public IReadOnlyList<StyledLine> Lines(RichText text, RenderContext context, int width) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            List<StyledLine> result = new List<StyledLine>();
            for (int i = 0; i < LinesBefore; i++) {
                result.Add(StyledLine.Empty);
            }
            string prefix = new string(' ', (Math.Max(0, StartTab) * 8) + Math.Max(0, StartSpaces));
            if (Time.HasValue) {
                prefix += "[" + Time.Value.ToString(TimestampFormat, CultureInfo.CurrentCulture) + "] ";
            }
            int prefixWidth = DisplayWidth.Measure(prefix);
            List<KeyValuePair<StyledLine, bool>> lines = new List<KeyValuePair<StyledLine, bool>>();
            IReadOnlyList<RichText> parts = Parts(text);
            for (int p = 0; p < parts.Count; p++) {
                IReadOnlyList<StyledLine> styled = parts[p].ToStyledText(context).Lines;
                for (int l = 0; l < styled.Count; l++) {
                    // Split parts continue under the text (hanging); line breaks inside a part start at the left, as Write-Host does.
                    lines.Add(new KeyValuePair<StyledLine, bool>(Pad(styled[l]), p > 0 && l == 0));
                }
            }
            bool first = true;
            foreach (KeyValuePair<StyledLine, bool> entry in lines) {
                IReadOnlyList<StyledLine> pieces = Wrap ? TextWrapper.Wrap(entry.Key, Math.Max(1, width - prefixWidth)) : new[] { entry.Key };
                for (int w = 0; w < pieces.Count; w++) {
                    List<TextSpan> spans = new List<TextSpan>();
                    if (first) {
                        spans.Add(new TextSpan(prefix, Style.Plain));
                    } else if (entry.Value || w > 0) {
                        spans.Add(new TextSpan(new string(' ', prefixWidth), Style.Plain));
                    }
                    spans.AddRange(pieces[w]);
                    StyledLine line = new StyledLine(spans);
                    if (HorizontalCenter && line.Width < width) {
                        List<TextSpan> centred = new List<TextSpan> { new TextSpan(new string(' ', (width - line.Width) / 2), Style.Plain) };
                        centred.AddRange(line);
                        line = new StyledLine(centred);
                    }
                    result.Add(line);
                    first = false;
                }
            }
            if (lines.Count == 0) {
                result.Add(StyledLine.From(prefix));
            }
            for (int i = 0; i < LinesAfter; i++) {
                result.Add(StyledLine.Empty);
            }
            return result;
        }

        // PadCenter, then PadLeft, then PadRight: the first one given applies; widths are terminal cells.
        private StyledLine Pad(StyledLine line) {
            int target = PadCenter > 0 ? PadCenter : PadLeft > 0 ? PadLeft : PadRight;
            if (target <= line.Width) {
                return line;
            }
            int missing = target - line.Width;
            int before = PadCenter > 0 ? missing / 2 : PadLeft > 0 ? missing : 0;
            int after = missing - before;
            int fill = Math.Max(1, DisplayWidth.Measure(PadCharacter.ToString()));
            List<TextSpan> spans = new List<TextSpan>();
            if (before > 0) {
                spans.Add(new TextSpan(new string(PadCharacter, before / fill) + new string(' ', before % fill), Style.Plain));
            }
            spans.AddRange(line);
            if (after > 0) {
                spans.Add(new TextSpan(new string(PadCharacter, after / fill) + new string(' ', after % fill), Style.Plain));
            }
            return new StyledLine(spans);
        }
    }
}
