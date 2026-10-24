using System;

namespace ConsoleMonster.Clipboard {
    /// <summary>
    /// Copies serialized output as Unicode clipboard text. Uses the Windows clipboard, macOS pbcopy, or Linux
    /// wl-copy/xclip without a runtime package dependency. SSH uses an explicitly supplied OSC 52 writer only.
    /// </summary>
    public sealed class ClipboardService {
        private readonly IClipboardPlatform _platform;

        /// <summary>Creates a service for the current operating system and desktop session.</summary>
        public ClipboardService() : this(new ClipboardPlatform()) { }

        internal ClipboardService(IClipboardPlatform platform) {
            _platform = platform;
        }

        /// <summary>Copies text, Markdown source or HTML source as text, returning a diagnostic instead of throwing for unavailable providers.</summary>
        /// <param name="text">The serialized document. It is passed through standard input, never through command arguments.</param>
        /// <param name="error">Null on success; an actionable diagnostic on failure (without the copied content).</param>
        /// <param name="osc52">Optional terminal writer for SSH sessions. Delivery is sent, not acknowledged by the terminal.</param>
        /// <returns>True only if the clipboard provider reports success.</returns>
        public bool TryCopy(string text, out string? error, Action<string>? osc52 = null) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            if (Has("SSH_CONNECTION") || Has("SSH_CLIENT") || Has("SSH_TTY")) {
                if (osc52 != null) {
                    osc52("\u001b]52;c;" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text)) + "\u0007");
                    error = null;
                    return true;
                }
                error = "No local clipboard is available over SSH. Export to a string or file and copy it on your local machine.";
                return false;
            }
            if (_platform.OS == ClipboardOS.Windows) {
                return _platform.CopyWindows(text, out error);
            }
            if (_platform.OS == ClipboardOS.MacOS) {
                return CopyWith("pbcopy", string.Empty, text, out error);
            }
            if (_platform.OS != ClipboardOS.Linux) {
                error = "Clipboard copying is supported on Windows, macOS and Linux.";
                return false;
            }
            if (Has("WAYLAND_DISPLAY")) {
                string? wayland = _platform.FindExecutable("wl-copy");
                if (wayland != null) {
                    return _platform.Run(wayland, "--type text/plain;charset=utf-8", text, out error);
                }
            }
            if (Has("DISPLAY")) {
                return CopyWith("xclip", "-selection clipboard -in", text, out error);
            }
            error = Has("WAYLAND_DISPLAY")
                ? "No clipboard provider was found. Install wl-clipboard (wl-copy) in your desktop session."
                : "No local display session is available. Export to a string or file instead.";
            return false;
        }

        private bool Has(string name) => !string.IsNullOrWhiteSpace(_platform.Environment(name));

        private bool CopyWith(string name, string arguments, string text, out string? error) {
            string? executable = _platform.FindExecutable(name);
            if (executable == null) {
                error = "Clipboard provider '" + name + "' was not found on PATH. Export to a string or file, or install the provider in your desktop session.";
                return false;
            }
            return _platform.Run(executable, arguments, text, out error);
        }
    }

    internal enum ClipboardOS { Windows, MacOS, Linux, Other }

    /// <summary>The environment, executable and native clipboard boundaries; replaceable in tests without touching a real clipboard.</summary>
    internal interface IClipboardPlatform {
        ClipboardOS OS { get; }
        string? Environment(string name);
        string? FindExecutable(string name);
        bool Run(string executable, string arguments, string text, out string? error);
        bool CopyWindows(string text, out string? error);
    }
}
