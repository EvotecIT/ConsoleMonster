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

        // Whether every character of the text survives the tier: its own repertoire, or on the ASCII tier the code page in
        // scope. A surrogate pair is checked as one code point; an isolated surrogate never is.
        private static bool Kept(string text, GlyphTier tier) {
            CodePageText? codePage = tier == GlyphTier.Ascii ? _codePageText : null;
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (Allowed(c, tier)) continue;
                if (codePage == null) return false;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) {
                    if (!codePage.Encodes(char.ConvertToUtf32(c, text[++i]))) return false;
                } else if (!codePage.Encodes(c)) {
                    return false;
                }
            }
            return true;
        }

        internal readonly struct CodePageTextScope : IDisposable {
            private readonly CodePageText? _previous;

            internal CodePageTextScope(CodePageText? previous) { _previous = previous; }

            public void Dispose() => _codePageText = _previous;
        }

        internal sealed class CodePageText {
            private readonly Encoding _encoding;
            // Basic plane by array (0 = not checked yet, 1 = encodable, 2 = not encodable); the other planes by dictionary.
            private readonly byte[] _known = new byte[char.MaxValue + 1];
            private readonly ConcurrentDictionary<int, bool> _supplementary = new ConcurrentDictionary<int, bool>();

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

            /// <summary>Whether the code page can encode this code point; ASCII, controls and surrogate code units are excluded.</summary>
            internal bool Encodes(int codePoint) {
                if (codePoint < 0x80 || codePoint > 0x10FFFF || codePoint >= 0xD800 && codePoint <= 0xDFFF) return false;
                if (codePoint > char.MaxValue) return _supplementary.GetOrAdd(codePoint, TryEncode);
                if (char.IsControl((char)codePoint)) return false;
                byte known = _known[codePoint];
                if (known == 0) {
                    known = TryEncode(codePoint) ? (byte)1 : (byte)2;
                    _known[codePoint] = known;
                }
                return known == 1;
            }

            private bool TryEncode(int codePoint) {
                try {
                    _encoding.GetByteCount(char.ConvertFromUtf32(codePoint));
                    return true;
                } catch (EncoderFallbackException) {
                    return false;
                }
            }
        }
    }
}
