using System;
using System.IO;
using System.Text;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Rendering {
    /// <summary>OSC 8 targets for terminal output, including file links. Exporters retain their own scheme policy.</summary>
    internal static class HyperlinkTarget {
        /// <summary>Normalizes a URL or explicit filesystem path without checking or opening the destination.</summary>
        internal static string? From(string? address) {
            if (string.IsNullOrWhiteSpace(address) || address!.Length > 2048) {
                return null;
            }
            foreach (char c in address) {
                if (c < 0x20 || (c >= 0x7f && c <= 0x9f)) {
                    return null;
                }
            }
            try {
                if (address.Length >= 3 && char.IsLetter(address[0]) && address[1] == ':' && (address[2] == '\\' || address[2] == '/')) {
                    return "file:///" + address.Substring(0, 2) + EscapePath(address.Substring(2).Replace('\\', '/'));
                }
                if (address.StartsWith("\\\\", StringComparison.Ordinal)) {
                    string unc = address.Substring(2).Replace('\\', '/');
                    int slash = unc.IndexOf('/');
                    if (slash <= 0) {
                        return null;
                    }
                    string server = unc.Substring(0, slash);
                    if (Uri.CheckHostName(server) == UriHostNameType.Unknown) {
                        return null;
                    }
                    return "file://" + server + EscapePath(unc.Substring(slash));
                }
                if (address.StartsWith("/", StringComparison.Ordinal) && !address.StartsWith("//", StringComparison.Ordinal)) {
                    return "file://" + EscapePath(address);
                }
                if (address.StartsWith("./", StringComparison.Ordinal) || address.StartsWith("../", StringComparison.Ordinal)
                    || address.StartsWith(".\\", StringComparison.Ordinal) || address.StartsWith("..\\", StringComparison.Ordinal)) {
                    return new Uri(Path.GetFullPath(address)).AbsoluteUri;
                }
                if (!IsSafe(address)) {
                    return null;
                }
                return address;
            } catch (ArgumentException) {
                return null;
            } catch (NotSupportedException) {
                return null;
            } catch (PathTooLongException) {
                return null;
            } catch (UriFormatException) {
                return null;
            }
        }

        /// <summary>Validates the exact string to be emitted; control characters can never enter an OSC sequence.</summary>
        internal static bool IsSafe(string? target) {
            if (string.IsNullOrWhiteSpace(target) || target!.Length > 8192) {
                return false;
            }
            foreach (char c in target) {
                if (c <= 0x20 || (c >= 0x7f && c <= 0x9f) || c == '\\' || c == '"' || c == '<' || c == '>' || c == '`') {
                    return false;
                }
            }
            if (!Uri.TryCreate(target, UriKind.Absolute, out Uri? uri)) {
                return false;
            }
            // On Unix, Uri accepts /tmp/report.txt as an absolute file URI. The raw path is not the URI we emit:
            // send it through From so it acquires file:// and escaping, just as Windows and UNC paths do.
            if (!target.StartsWith(uri.Scheme + ":", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            return uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "mailto" || uri.Scheme == "ftp" || uri.Scheme == "file";
        }

        // URI percent-encoding keeps the destination while reduced tiers keep every emitted code unit in ASCII.
        internal static string? ToAscii(string? target) {
            if (target == null) { return null; }
            foreach (char c in target) {
                if (c <= 0x7f) { continue; }
                // Domains use IDNA, while path/query text uses URI percent-encoding.
                // Percent-encoding a Unicode hostname directly would produce an invalid authority.
                try {
                    Uri uri = new Uri(target, UriKind.Absolute);
                    UriBuilder normalized = new UriBuilder(uri);
                    if (uri.Host.Length > 0) { normalized.Host = uri.IdnHost; }
                    target = normalized.Uri.AbsoluteUri;
                } catch (UriFormatException) { return null; }
                StringBuilder encoded = new StringBuilder(target.Length);
                const string hex = "0123456789ABCDEF";
                foreach (byte value in Encoding.UTF8.GetBytes(target)) {
                    if (value <= 0x7f) { encoded.Append((char)value); }
                    else { encoded.Append('%').Append(hex[value >> 4]).Append(hex[value & 15]); }
                }
                return encoded.ToString();
            }
            return target;
        }

        // A printed destination is a resource identifier, not prose: transliteration would change its identity.
        internal static string ForText(string address, GlyphTier tier) {
            string clean = TextSanitizer.Clean(address);
            if (tier == GlyphTier.Full) return clean;
            bool unicode = false;
            foreach (char c in clean) if (c > 0x7f) { unicode = true; break; }
            if (!unicode) return clean;
            string? target = From(clean);
            string? ascii = ToAscii(target ?? clean);
            if (ascii != null) return ascii;
            // Relative destinations have no authority to normalize. Preserve their Unicode bytes as escapes.
            var encoded = new StringBuilder(clean.Length);
            foreach (byte value in Encoding.UTF8.GetBytes(clean)) {
                if (value <= 0x7f) encoded.Append((char)value);
                else encoded.Append('%').Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }
            return encoded.ToString();
        }

        private static string EscapePath(string path) {
            string[] parts = path.Split('/');
            for (int i = 0; i < parts.Length; i++) {
                parts[i] = Uri.EscapeDataString(parts[i]);
            }
            return string.Join("/", parts);
        }
    }
}
