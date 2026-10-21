using System;
using System.IO;

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
            return uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "mailto" || uri.Scheme == "ftp" || uri.Scheme == "file";
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
