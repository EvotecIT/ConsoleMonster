using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Output {
    /// <summary>
    /// The running status of <see cref="Terminal.Status(string, Action{StatusContext})"/>: change its text with
    /// <see cref="Update(string)"/>. On an interactive VT terminal it is a transient inline live area with a
    /// <see cref="Spinner"/>: console output and <see cref="Terminal"/> writes made meanwhile scroll above it, and the line is
    /// removed when the work ends. In redirected output, CI logs and plain mode each new text is printed once as a plain line
    /// (no spinner, no redraw), as <c>Write-ConsoleStatus</c> does. Updates are thread-safe.
    /// </summary>
    public sealed class StatusContext : IDisposable {
        private readonly object _gate = new object();
        private readonly TerminalProfile _profile;
        private readonly ITerminalWriter? _writer;
        private readonly LiveSession? _session;
        private readonly SpinnerHandle? _spinner;
        private RichText _text = RichText.Empty;
        private string? _lastPrinted;
        private bool _disposed;

        internal StatusContext(TerminalProfile profile, string markup, ITerminalWriter? writer = null) {
            _profile = profile;
            _writer = writer;
            bool live = profile.IsLive && !profile.IsOutputRedirected && !profile.IsContinuousIntegration && writer == null && Terminal.ConsoleOwner == null;
            if (live) {
                Spinner spinner = new Spinner(markup ?? string.Empty, "status") { ShowElapsed = false };
                _session = new LiveSession(spinner, new LiveSessionOptions {
                    Mode = LiveMode.Inline,
                    PrintFinalState = false,
                    HandleCancelKey = false,
                    Discoverable = false,
                    Profile = profile
                });
                _session.Start();
                _spinner = spinner.Handle(_session);
            }
            Update(markup ?? string.Empty);
        }

        /// <summary>Whether the status is drawn in place with a spinner (false: printed as plain lines).</summary>
        public bool IsLive => _session != null;

        /// <summary>The current status text, without markup.</summary>
        public string Text {
            get {
                lock (_gate) {
                    return _text.PlainText;
                }
            }
        }

        /// <summary>Changes the status text.</summary>
        /// <param name="markup">The new text, as markup (<c>[muted]3 of 10[/]</c>); escape values with <see cref="Markup.Escape"/>.</param>
        public void Update(string markup) => Update(RichText.FromMarkup(markup ?? string.Empty));

        /// <summary>Changes the status text.</summary>
        /// <param name="text">The new text.</param>
        public void Update(RichText text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            lock (_gate) {
                if (_disposed) {
                    return;
                }
                _text = text;
                if (_spinner != null) {
                    _spinner.Label(text.ToMarkup());
                    return;
                }
                string plain = text.PlainText;
                if (plain.Length == 0 || string.Equals(plain, _lastPrinted, StringComparison.Ordinal)) {
                    return;
                }
                _lastPrinted = plain;
                RenderContext context = Terminal.CreateContext(_profile);
                Terminal.WriteLines(new[] { StatusLine.Compose(context, text, null, false, false, false, "HH:mm:ss", DateTime.Now, 0, true) }, true, _profile, _writer);
            }
        }

        /// <summary>Ends the status: the live line is removed (output written meanwhile stays).</summary>
        public void Dispose() {
            lock (_gate) {
                if (_disposed) {
                    return;
                }
                _disposed = true;
            }
            _session?.Dispose();
        }
    }
}
