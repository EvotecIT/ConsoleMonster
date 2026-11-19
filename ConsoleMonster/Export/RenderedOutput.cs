using System;
using System.Text;

namespace ConsoleMonster.Export {
    /// <summary>One rendered document or image: its format, its bytes and its media type.</summary>
    public sealed class RenderedOutput {
        private readonly byte[] _bytes;

        /// <summary>Wraps rendered bytes.</summary>
        /// <param name="format">The format.</param>
        /// <param name="bytes">The encoded output.</param>
        public RenderedOutput(OutputFormat format, byte[] bytes) {
            Format = format;
            _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        /// <summary>The format.</summary>
        public OutputFormat Format { get; }

        /// <summary>Whether the output is text (everything except <see cref="OutputFormat.Png"/>).</summary>
        public bool IsText => Format != OutputFormat.Png;

        /// <summary>The media type, such as <c>image/svg+xml</c>.</summary>
        public string MediaType => MediaTypeOf(Format);

        /// <summary>A copy of the encoded output (UTF-8 without a byte order mark for text).</summary>
        /// <returns>The bytes.</returns>
        public byte[] ToBytes() => (byte[])_bytes.Clone();

        /// <summary>The text of a text format.</summary>
        /// <returns>The text.</returns>
        /// <exception cref="InvalidOperationException">The output is an image.</exception>
        public string ToText() => IsText ? new UTF8Encoding(false).GetString(_bytes)
            : throw new InvalidOperationException(Format + " output is binary; use ToBytes.");

        /// <summary>The text of a text format.</summary>
        /// <returns>The text, or the format name for an image.</returns>
        public override string ToString() => IsText ? ToText() : Format.ToString();

        internal byte[] Bytes => _bytes;

        /// <summary>The media type of a format.</summary>
        /// <param name="format">The format.</param>
        /// <returns>The media type.</returns>
        public static string MediaTypeOf(OutputFormat format) {
            switch (format) {
                case OutputFormat.Html: return "text/html";
                case OutputFormat.Svg: return "image/svg+xml";
                case OutputFormat.Asciicast: return "application/x-asciicast";
                case OutputFormat.Markdown:
                case OutputFormat.FencedText: return "text/markdown";
                case OutputFormat.Png: return "image/png";
                default: return "text/plain";
            }
        }

        internal static RenderedOutput FromText(OutputFormat format, string text) => new RenderedOutput(format, new UTF8Encoding(false).GetBytes(text ?? string.Empty));
    }
}
