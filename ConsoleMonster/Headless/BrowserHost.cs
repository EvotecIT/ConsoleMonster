using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Serialization;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.Headless {
    /// <summary>Settings for <see cref="BrowserHost.Start"/>.</summary>
    public sealed class BrowserHostOptions {
        /// <summary>The port on 127.0.0.1 (default 0: the system picks a free one; read it from <see cref="BrowserHost.Address"/>).</summary>
        public int Port { get; set; }

        /// <summary>The page title (default <c>ConsoleMonster</c>).</summary>
        public string Title { get; set; } = "ConsoleMonster";
    }

    /// <summary>
    /// Shows a <see cref="HeadlessTerminal"/> in a web page and sends the page's keys and clicks back, for docs, demos and
    /// visual tests. The page is self-contained (no external scripts, styles or fonts) and updates as the screen changes.
    /// The server listens on 127.0.0.1 only and every request must carry the per-run token from <see cref="Address"/>;
    /// requests for another host name (DNS rebinding) or posted from another origin are refused.
    /// </summary>
    public sealed class BrowserHost : IDisposable {
        private const int MaxHeader = 16 * 1024;
        private const int MaxBody = 16 * 1024;
        private readonly HeadlessTerminal _terminal;
        private readonly BrowserHostOptions _options;
        private readonly LoopbackListener _listener;
        private readonly RecordingRedactor _redactor = new RecordingRedactor(Array.Empty<string>());
        private readonly string _token = LoopbackListener.NewToken();
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private BrowserHost(HeadlessTerminal terminal, BrowserHostOptions options) {
            _terminal = terminal;
            _options = options;
            _listener = new LoopbackListener(options.Port, 8, "ConsoleMonster browser host", Serve);
            Address = new Uri("http://127.0.0.1:" + _listener.Port.ToString(CultureInfo.InvariantCulture) + "/?t=" + _token);
        }

        /// <summary>Starts serving a headless terminal.</summary>
        /// <param name="terminal">The terminal to show and drive.</param>
        /// <param name="options">Port and page title.</param>
        /// <returns>The running host; dispose it to stop.</returns>
        public static BrowserHost Start(HeadlessTerminal terminal, BrowserHostOptions? options = null) =>
            new BrowserHost(terminal ?? throw new ArgumentNullException(nameof(terminal)), options ?? new BrowserHostOptions());

        /// <summary>The page address, including the token (<c>http://127.0.0.1:port/?t=…</c>). Treat it like a password.</summary>
        public Uri Address { get; }

        /// <summary>Stops serving and closes open pages' connections.</summary>
        public void Dispose() {
            _stopped.Set();
            _listener.Dispose();
            _stopped.Dispose();
        }

        private void Serve(TcpClient client) {
            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = 10000;
            Request? request;
            // The whole request must arrive within ten seconds, however slowly it is sent.
            using (Timer deadline = new Timer(_ => client.Close(), null, 10000, Timeout.Infinite)) {
                request = Request.Read(stream);
                deadline.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (request == null) { return; }
            string port = _listener.Port.ToString(CultureInfo.InvariantCulture);
            // DNS rebinding: a page on another name that resolves to 127.0.0.1 still sends its own name as Host.
            if (request.Header("Host") != "127.0.0.1:" + port && request.Header("Host") != "localhost:" + port) { Respond(stream, 403, "text/plain", "Forbidden host."); return; }
            if (!LoopbackListener.TokenEquals(request.Query("t"), _token)) { Respond(stream, 403, "text/plain", "A valid token is required."); return; }
            if (request.Method == "GET" && request.Path == "/") { Respond(stream, 200, "text/html; charset=utf-8", Page(out string nonce), nonce); }
            else if (request.Method == "GET" && request.Path == "/events") { Stream(stream); }
            else if (request.Method == "POST" && request.Path == "/input") {
                string? origin = request.Header("Origin");
                if ((origin != "http://127.0.0.1:" + port && origin != "http://localhost:" + port) || !LoopbackListener.TokenEquals(request.Header("X-ConsoleMonster-Token"), _token)) {
                    Respond(stream, 403, "text/plain", "Input is accepted only from this page.");
                    return;
                }
                try { Apply(request.Body); }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException) { Respond(stream, 400, "text/plain", ex.Message); return; }
                Respond(stream, 204, null, null);
            } else { Respond(stream, 404, "text/plain", "Not found."); }
        }

        private string Page(out string nonce) {
            nonce = LoopbackListener.NewToken();
            StringBuilder title = new StringBuilder();
            ExportText.AppendEscaped(title, _options.Title);
            return ConsoleExporter.ReadAsset("browser-host.html").Replace("__TITLE__", title.ToString()).Replace("__TOKEN__", _token).Replace("__NONCE__", nonce);
        }

        // Server-sent events: the current screen at once, then each change, at most about 30 times a second.
        private void Stream(NetworkStream stream) {
            stream.ReadTimeout = Timeout.Infinite;
            using (AutoResetEvent changed = new AutoResetEvent(true)) {
                Action<CellBuffer> notify = frame => { try { changed.Set(); } catch (ObjectDisposedException) { } };
                _terminal.FrameRendered += notify;
                try {
                    Write(stream, "HTTP/1.1 200 OK\r\nContent-Type: text/event-stream; charset=utf-8\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n");
                    WaitHandle[] handles = { changed, _stopped };
                    while (true) {
                        int signal = WaitHandle.WaitAny(handles, TimeSpan.FromSeconds(15));
                        if (signal == 1) { return; }
                        if (signal == WaitHandle.WaitTimeout) { Write(stream, ": keep-alive\n\n"); continue; }
                        CellBuffer frame = ScreenRedaction.Apply(_terminal.Snapshot(), _redactor);
                        string html = ConsoleExporter.ToHtml(frame, new ExportOptions { Fragment = true, Width = frame.Width, Height = frame.Height, Theme = ExportTheme.Dark, Padding = 10 });
                        Write(stream, "data: " + MiniJson.WriteCompact(new Dictionary<string, object?> { ["width"] = frame.Width, ["height"] = frame.Height, ["html"] = html }) + "\n\n");
                        if (_stopped.WaitOne(33)) { return; }
                    }
                } finally { _terminal.FrameRendered -= notify; }
            }
        }

        private void Apply(string body) {
            Dictionary<string, object?> input = MiniJson.Read(body) as Dictionary<string, object?> ?? throw new FormatException("Input is a JSON object.");
            if (input.TryGetValue("keys", out object? keys) && keys is string script) { _terminal.Send(script); }
            else if (input.TryGetValue("text", out object? text) && text is string typed) {
                foreach (char c in typed) { _terminal.Send(c == '\n' ? InputEvent.Press(ConsoleKey.Enter) : InputEvent.Character(c)); }
            } else if (input.TryGetValue("click", out object? click) && click is Dictionary<string, object?> point) {
                int x = Coordinate(point, "x", _terminal.Width), y = Coordinate(point, "y", _terminal.Height);
                int button = point.TryGetValue("button", out object? value) && value is string b && (b == "2" || b == "3") ? b[0] - '0' : 1;
                _terminal.Send(InputEvent.Mouse(x, y, button, true));
                _terminal.Send(InputEvent.Mouse(x, y, button, false));
            } else { throw new FormatException("Send keys, text or click."); }
        }

        private static int Coordinate(Dictionary<string, object?> point, string name, int limit) {
            if (point.TryGetValue(name, out object? value) && value is string text && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number < limit) { return number; }
            throw new FormatException("'" + name + "' must be a cell on the screen.");
        }

        private static void Respond(NetworkStream stream, int status, string? type, string? body, string? nonce = null) {
            byte[] content = body == null ? Array.Empty<byte>() : new UTF8Encoding(false).GetBytes(body);
            StringBuilder head = new StringBuilder("HTTP/1.1 ").Append(status.ToString(CultureInfo.InvariantCulture)).Append(status == 200 ? " OK" : status == 204 ? " No Content" : status == 400 ? " Bad Request" : status == 403 ? " Forbidden" : " Not Found").Append("\r\n");
            if (type != null) { head.Append("Content-Type: ").Append(type).Append("\r\n"); }
            head.Append("Content-Length: ").Append(content.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            head.Append("Cache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\nConnection: close\r\n");
            if (nonce != null) {
                head.Append("Content-Security-Policy: default-src 'none'; script-src 'nonce-").Append(nonce)
                    .Append("'; style-src 'unsafe-inline'; connect-src 'self'; img-src data:; base-uri 'none'; form-action 'none'; frame-ancestors 'none'\r\n");
            }
            head.Append("\r\n");
            Write(stream, head.ToString());
            stream.Write(content, 0, content.Length);
        }

        private static void Write(NetworkStream stream, string text) {
            byte[] bytes = new UTF8Encoding(false).GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private sealed class Request {
            private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> _query = new Dictionary<string, string>(StringComparer.Ordinal);
            internal string Method { get; private set; } = string.Empty;
            internal string Path { get; private set; } = string.Empty;
            internal string Body { get; private set; } = string.Empty;
            internal string? Header(string name) => _headers.TryGetValue(name, out string? value) ? value : null;
            internal string? Query(string name) => _query.TryGetValue(name, out string? value) ? value : null;

            // A deliberately small HTTP/1.1 reader: one request per connection, bounded headers and body.
            internal static Request? Read(NetworkStream stream) {
                MemoryStream head = new MemoryStream();
                byte[] one = new byte[1];
                while (true) {
                    if (stream.Read(one, 0, 1) == 0) { return null; }
                    head.WriteByte(one[0]);
                    long length = head.Length;
                    if (length >= 4) {
                        byte[] buffer = head.GetBuffer();
                        if (buffer[length - 4] == '\r' && buffer[length - 3] == '\n' && buffer[length - 2] == '\r' && buffer[length - 1] == '\n') { break; }
                    }
                    if (length > MaxHeader) { return null; }
                }
                string[] lines = Encoding.ASCII.GetString(head.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
                string[] start = lines[0].Split(' ');
                if (start.Length != 3) { return null; }
                Request request = new Request { Method = start[0] };
                string target = start[1];
                int question = target.IndexOf('?');
                request.Path = question < 0 ? target : target.Substring(0, question);
                if (question >= 0) {
                    foreach (string pair in target.Substring(question + 1).Split('&')) {
                        int equals = pair.IndexOf('=');
                        if (equals > 0) { request._query[Uri.UnescapeDataString(pair.Substring(0, equals))] = Uri.UnescapeDataString(pair.Substring(equals + 1)); }
                    }
                }
                for (int i = 1; i < lines.Length; i++) {
                    int colon = lines[i].IndexOf(':');
                    if (colon > 0) { request._headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim(); }
                }
                if (int.TryParse(request.Header("Content-Length"), NumberStyles.None, CultureInfo.InvariantCulture, out int size) && size > 0) {
                    if (size > MaxBody) { return null; }
                    byte[] body = new byte[size];
                    int read = 0;
                    while (read < size) {
                        int count = stream.Read(body, read, size - read);
                        if (count == 0) { return null; }
                        read += count;
                    }
                    request.Body = new UTF8Encoding(false).GetString(body);
                }
                return request;
            }
        }
    }
}
