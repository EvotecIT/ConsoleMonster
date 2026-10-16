using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>One line of styled spans (no line breaks). Immutable once created.</summary>
    public sealed class StyledLine : IReadOnlyList<TextSpan> {
        private static readonly TextSpan[] NoSpans = new TextSpan[0];
        private readonly TextSpan[] _spans;
        private int _width = -1;

        /// <summary>Creates a line from spans. Line breaks inside spans are replaced with spaces.</summary>
        /// <param name="spans">The spans.</param>
        public StyledLine(IEnumerable<TextSpan> spans) {
            if (spans == null) {
                throw new ArgumentNullException(nameof(spans));
            }
            List<TextSpan> list = new List<TextSpan>();
            foreach (TextSpan span in spans) {
                if (span.Text.Length == 0) {
                    continue;
                }
                if (span.Text.IndexOf('\n') >= 0 || span.Text.IndexOf('\r') >= 0) {
                    list.Add(new TextSpan(span.Text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' '), span.Style));
                } else {
                    list.Add(span);
                }
            }
            _spans = list.Count == 0 ? NoSpans : list.ToArray();
        }

        /// <summary>An empty line.</summary>
        public static StyledLine Empty { get; } = new StyledLine(NoSpans);

        /// <summary>Creates a single-span line.</summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        /// <returns>The line.</returns>
        public static StyledLine From(string text, Style style = default) => new StyledLine(new[] { new TextSpan(text, style) });

        /// <inheritdoc />
        public int Count => _spans.Length;

        /// <inheritdoc />
        public TextSpan this[int index] => _spans[index];

        /// <summary>Display width in cells.</summary>
        public int Width {
            get {
                if (_width < 0) {
                    // A style boundary can occur inside a combining or ZWJ sequence.
                    _width = DisplayWidth.Measure(PlainText);
                }
                return _width;
            }
        }

        /// <summary>The text without styles.</summary>
        public string PlainText {
            get {
                if (_spans.Length == 1) {
                    return _spans[0].Text;
                }
                StringBuilder sb = new StringBuilder();
                foreach (TextSpan span in _spans) {
                    sb.Append(span.Text);
                }
                return sb.ToString();
            }
        }

        /// <summary>Returns a copy with <paramref name="baseStyle"/> layered under every span.</summary>
        /// <param name="baseStyle">The style underneath.</param>
        /// <returns>A new line.</returns>
        public StyledLine WithBaseStyle(Style baseStyle) {
            if (baseStyle.IsPlain) {
                return this;
            }
            TextSpan[] spans = new TextSpan[_spans.Length];
            for (int i = 0; i < spans.Length; i++) {
                spans[i] = new TextSpan(_spans[i].Text, baseStyle.Combine(_spans[i].Style));
            }
            return new StyledLine(spans);
        }

        /// <inheritdoc />
        public IEnumerator<TextSpan> GetEnumerator() => ((IEnumerable<TextSpan>)_spans).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _spans.GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => PlainText;
    }
}
