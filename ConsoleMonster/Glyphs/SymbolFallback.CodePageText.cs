using System;
using System.Collections.Concurrent;
using System.Text;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Glyphs {
    public static partial class SymbolFallback {
        // The code page that rendering for the current profile may keep in caller text (see TerminalProfile.TextCodePage).
        // Rendering and encoding are synchronous, so the scope is per thread.
        [ThreadStatic] private static CodePageText? _codePageText;
        private static readonly ConcurrentDictionary<int, CodePageText?> CodePageTexts = new ConcurrentDictionary<int, CodePageText?>();

        /// <summary>
        /// Lets ASCII-tier text keep the characters the profile's code page can encode until the scope is disposed.
        /// Other profiles, including explicitly chosen ASCII, clear the scope so their text keeps transliterating.
        /// </summary>
        internal static CodePageTextScope KeepCodePageText(TerminalProfile? profile) {
            CodePageText? previous = _codePageText;
            _codePageText = profile != null && profile.Glyphs == GlyphTier.Ascii && profile.TextCodePage != 0
                ? CodePageTexts.GetOrAdd(profile.TextCodePage, CodePageText.Create)
                : null;
            return new CodePageTextScope(previous);
        }

        private static bool Kept(char c, GlyphTier tier)
            => Allowed(c, tier) || tier == GlyphTier.Ascii && _codePageText != null && _codePageText.Encodes(c);

        internal readonly struct CodePageTextScope : IDisposable {
            private readonly CodePageText? _previous;

            internal CodePageTextScope(CodePageText? previous) { _previous = previous; }

            public void Dispose() => _codePageText = _previous;
        }

        internal sealed class CodePageText {
            private readonly Encoding _encoding;
            // 0 = not checked yet, 1 = encodable, 2 = not encodable.
            private readonly byte[] _known = new byte[char.MaxValue + 1];

            private CodePageText(Encoding encoding) { _encoding = encoding; }

            internal static CodePageText? Create(int codePage) {
                // No best-fit mapping: a character the code page only approximates is transliterated as before.
                try {
                    return new CodePageText(Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback));
                } catch (Exception) {
                    // .NET only knows legacy code pages once a provider is registered; the console's own encoding is
                    // the one this output is written with.
                }
                try {
                    Encoding console = Console.OutputEncoding;
                    if (console.CodePage != codePage) return null;
                    Encoding encoding = (Encoding)console.Clone();
                    encoding.EncoderFallback = EncoderFallback.ExceptionFallback;
                    return new CodePageText(encoding);
                } catch (Exception) {
                    // Unknown code page: keep transliterating.
                    return null;
                }
            }

            internal bool Encodes(char c) {
                if (c < 0x80 || char.IsSurrogate(c) || char.IsControl(c)) return false;
                byte known = _known[c];
                if (known == 0) {
                    known = TryEncode(c) ? (byte)1 : (byte)2;
                    _known[c] = known;
                }
                return known == 1;
            }

            private bool TryEncode(char c) {
                try {
                    _encoding.GetByteCount(new[] { c });
                    return true;
                } catch (EncoderFallbackException) {
                    return false;
                }
            }
        }
    }
}
