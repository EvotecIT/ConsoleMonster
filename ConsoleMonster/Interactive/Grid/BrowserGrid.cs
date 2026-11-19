using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>Options for <see cref="Grid.ShowInBrowser"/>.</summary>
    public sealed class BrowserGridOptions {
        /// <summary>
        /// Opens the page; returns false when it could not. Null runs the command in the BROWSER environment variable
        /// when set, otherwise the default browser (Windows, macOS, or xdg-open on Linux).
        /// </summary>
        public Func<Uri, bool>? Open { get; set; }

        /// <summary>Called with the page address before the browser opens, for example to print it.</summary>
        public Action<Uri>? Opened { get; set; }

        /// <summary>How long to wait for OK or Cancel; null waits until the page answers or the caller cancels.</summary>
        public TimeSpan? Timeout { get; set; }
    }

    public sealed partial class Grid {
        /// <summary>
        /// Shows the grid in the default browser and returns the chosen rows' original objects. The page is served from
        /// 127.0.0.1 on a random port behind a one-time token, loads nothing external, and offers the same filter, sort
        /// and selection as the screen grid. Commands and row refresh are screen-only.
        /// </summary>
        /// <param name="options">Filter, sort and hidden columns to start with; null for the defaults.</param>
        /// <param name="browser">How the page opens and how long to wait; null for the defaults.</param>
        /// <param name="cancellationToken">Stops waiting and closes the listener.</param>
        /// <returns>Accepted with the chosen items, or not accepted when the page was cancelled or closed.</returns>
        /// <exception cref="PromptUnavailableException">No browser could be opened.</exception>
        /// <exception cref="TimeoutException">The page did not answer within <see cref="BrowserGridOptions.Timeout"/>.</exception>
        public GridResult ShowInBrowser(GridOptions? options = null, BrowserGridOptions? browser = null, CancellationToken cancellationToken = default) {
            options ??= new GridOptions();
            browser ??= new BrowserGridOptions();
            string page = BrowserGridPage.Build(this, options);
            using BrowserGridServer server = BrowserGridServer.Start(page);
            browser.Opened?.Invoke(server.Address);
            Func<Uri, bool> open = browser.Open ?? BrowserGridServer.OpenDefaultBrowser;
            if (!open(server.Address)) {
                throw new PromptUnavailableException("No browser could be opened for the grid. Open " + server.Address + " or use the screen grid.");
            }
            int[]? chosen = server.WaitForAnswer(browser.Timeout, cancellationToken);
            if (chosen == null) {
                return new GridResult(false, Array.Empty<object?>());
            }
            List<object?> items = new List<object?>();
            if (OutputMode != GridSelection.None) {
                foreach (int index in chosen) {
                    if (index >= 0 && index < Rows.Count) {
                        items.Add(Rows[index].Item);
                    }
                    if (OutputMode == GridSelection.Single) {
                        break;
                    }
                }
            }
            return new GridResult(true, items);
        }
    }

    /// <summary>Serves one page on loopback behind a random token and waits for its single answer.</summary>
    internal sealed class BrowserGridServer : IDisposable {
        private readonly HttpListener _listener;
        private readonly byte[] _page;
        private readonly string _token;
        private readonly int _port;

        private BrowserGridServer(HttpListener listener, int port, string token, string page) {
            _listener = listener;
            _port = port;
            _token = token;
            _page = Encoding.UTF8.GetBytes(page);
        }

        public Uri Address => new Uri("http://127.0.0.1:" + _port.ToString(CultureInfo.InvariantCulture) + "/" + _token + "/");

        public static BrowserGridServer Start(string page) {
            string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            Exception? last = null;
            for (int attempt = 0; attempt < 5; attempt++) {
                int port = FreePort();
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + "/" + token + "/");
                try {
                    listener.Start();
                    return new BrowserGridServer(listener, port, token, page);
                } catch (HttpListenerException ex) {
                    // Another process took the port between probing and binding.
                    last = ex;
                    listener.Close();
                }
            }
            throw last == null
                ? new PromptUnavailableException("The browser grid could not listen on 127.0.0.1.")
                : new PromptUnavailableException("The browser grid could not listen on 127.0.0.1.", last);
        }

        private static int FreePort() {
            TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            try { return ((IPEndPoint)probe.LocalEndpoint).Port; } finally { probe.Stop(); }
        }

        /// <summary>Waits for OK (the chosen row indexes) or Cancel (null).</summary>
        public int[]? WaitForAnswer(TimeSpan? timeout, CancellationToken cancellationToken) {
            DateTime? deadline = timeout.HasValue ? DateTime.UtcNow + timeout.Value : (DateTime?)null;
            while (true) {
                Task<HttpListenerContext> next = _listener.GetContextAsync();
                while (!next.Wait(200)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (deadline.HasValue && DateTime.UtcNow >= deadline.Value) {
                        throw new TimeoutException("The browser grid was not answered in time.");
                    }
                }
                HttpListenerContext context = next.Result;
                if (Handle(context, out int[]? answer)) {
                    return answer;
                }
            }
        }

        private bool Handle(HttpListenerContext context, out int[]? answer) {
            answer = null;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            try {
                string expectedHost = "127.0.0.1:" + _port.ToString(CultureInfo.InvariantCulture);
                if (!string.Equals(request.UserHostName, expectedHost, StringComparison.OrdinalIgnoreCase)) {
                    // Only a page opened on this exact address may talk to the grid (no DNS rebinding through another name).
                    Respond(response, 403, "text/plain", Encoding.UTF8.GetBytes("Forbidden"));
                    return false;
                }
                string path = request.Url?.AbsolutePath ?? string.Empty;
                string root = "/" + _token + "/";
                if (request.HttpMethod == "GET" && path == root) {
                    response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self'; base-uri 'none'; form-action 'none'";
                    response.Headers["Cache-Control"] = "no-store";
                    response.Headers["Referrer-Policy"] = "no-referrer";
                    Respond(response, 200, "text/html; charset=utf-8", _page);
                    return false;
                }
                if (request.HttpMethod == "POST" && path == root + "answer") {
                    string origin = request.Headers["Origin"] ?? string.Empty;
                    if (origin.Length > 0 && !string.Equals(origin, "http://" + expectedHost, StringComparison.OrdinalIgnoreCase)) {
                        Respond(response, 403, "text/plain", Encoding.UTF8.GetBytes("Forbidden"));
                        return false;
                    }
                    string body;
                    using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8)) {
                        body = reader.ReadToEnd();
                    }
                    answer = ParseAnswer(body);
                    Respond(response, 200, "text/plain", Encoding.UTF8.GetBytes("ok"));
                    return true;
                }
                Respond(response, 404, "text/plain", Encoding.UTF8.GetBytes("Not found"));
                return false;
            } catch (HttpListenerException) {
                // The browser went away mid-response; keep waiting for an answer.
                return false;
            }
        }

        /// <summary>"ok:3,0,7" returns those indexes; anything else is a cancel.</summary>
        internal static int[]? ParseAnswer(string body) {
            if (!body.StartsWith("ok:", StringComparison.Ordinal)) {
                return null;
            }
            List<int> indexes = new List<int>();
            foreach (string part in body.Substring(3).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && !indexes.Contains(index)) {
                    indexes.Add(index);
                }
            }
            return indexes.ToArray();
        }

        private static void Respond(HttpListenerResponse response, int status, string contentType, byte[] body) {
            response.StatusCode = status;
            response.ContentType = contentType;
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.ContentLength64 = body.Length;
            response.OutputStream.Write(body, 0, body.Length);
            response.Close();
        }

        /// <summary>Opens a URL with the command named by BROWSER when set, otherwise the platform's default handler.</summary>
        internal static bool OpenDefaultBrowser(Uri address) {
            try {
                ProcessStartInfo start;
                // BROWSER is the common convention (xdg-open and Python honour it); the first of its ':'-separated commands is used.
                string browser = (Environment.GetEnvironmentVariable("BROWSER") ?? string.Empty).Split(System.IO.Path.PathSeparator)[0].Trim();
                if (browser.Length > 0) {
                    start = new ProcessStartInfo(browser, "\"" + address.AbsoluteUri + "\"") { UseShellExecute = false };
                } else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                    start = new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true };
                } else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
                    start = new ProcessStartInfo("open", address.AbsoluteUri) { UseShellExecute = false };
                } else {
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))) {
                        return false;
                    }
                    start = new ProcessStartInfo("xdg-open", address.AbsoluteUri) { UseShellExecute = false };
                }
                using Process? process = Process.Start(start);
                return true;
            } catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException || ex is FileNotFoundException) {
                return false;
            }
        }

        public void Dispose() {
            try { _listener.Stop(); } catch (ObjectDisposedException) { }
            _listener.Close();
        }
    }
}
