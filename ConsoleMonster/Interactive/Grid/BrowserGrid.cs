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
using PowerForge.Fluent;

namespace ConsoleMonster.Interactive {
    /// <summary>Options for <see cref="Grid.ShowInBrowser"/>.</summary>
    [Fluent]
    public sealed class BrowserGridOptions {
        /// <summary>
        /// Opens the page; returns false when it could not. Null runs the command in the BROWSER environment variable
        /// when set, otherwise the default browser (Windows, macOS, or xdg-open on Linux). A launcher that exits with a
        /// failure code before the page answers counts as not opened.
        /// </summary>
        [Fluent("OpenBy")]
        public Func<Uri, bool>? Open { get; set; }

        /// <summary>Called with the page address before the browser opens, for example to print it.</summary>
        [Fluent("WhenOpened")]
        public Action<Uri>? Opened { get; set; }

        /// <summary>How long to wait for OK or Cancel; null waits until the page answers or the caller cancels.</summary>
        public TimeSpan? Timeout { get; set; }
    }

    public sealed partial class Grid {
        /// <summary>
        /// Shows the grid in the default browser and returns the chosen rows' original objects. The page is served from
        /// 127.0.0.1 on a random port behind a one-time token, loads nothing external, and offers the same filter, sort
        /// and selection as the screen grid. Commands and row refresh are screen-only. Lazy cells are computed before the
        /// page opens. Sessions where nobody can answer (<see cref="InteractionMode.None"/> or
        /// <see cref="InteractionMode.Snapshot"/>, a non-interactive flag, or CI) never open a browser.
        /// </summary>
        /// <param name="options">Filter, sort, hidden columns and interaction options; null for the defaults.</param>
        /// <param name="browser">How the page opens and how long to wait; null for the defaults.</param>
        /// <param name="cancellationToken">Stops waiting and closes the listener.</param>
        /// <returns>Accepted with the chosen items, or not accepted when the page was cancelled or closed.</returns>
        /// <exception cref="PromptUnavailableException">Nobody can answer in this session, or no browser could be opened.</exception>
        /// <exception cref="TimeoutException">The page did not answer within <see cref="BrowserGridOptions.Timeout"/>.</exception>
        public GridResult ShowInBrowser(GridOptions? options = null, BrowserGridOptions? browser = null, CancellationToken cancellationToken = default) {
            options ??= new GridOptions();
            browser ??= new BrowserGridOptions();
            if (!BrowserGridServer.CanAsk(options.Interactive ?? InteractiveOptions.Default)) {
                throw new PromptUnavailableException("Nobody can answer the browser grid in a non-interactive session.");
            }
            string page = BrowserGridPage.Build(this, options, cancellationToken);
            using BrowserGridServer server = BrowserGridServer.Start(page, Rows.Count);
            browser.Opened?.Invoke(server.Address);
            Process? launcher = null;
            try {
                bool opened = browser.Open != null ? browser.Open(server.Address) : BrowserGridServer.Launch(server.Address, out launcher);
                if (!opened) {
                    throw new PromptUnavailableException("No browser could be opened for the grid. Open " + server.Address + " or use the screen grid.");
                }
                int[]? chosen = server.WaitForAnswer(browser.Timeout, cancellationToken, launcher);
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
            } finally {
                launcher?.Dispose();
            }
        }
    }

    /// <summary>Serves one page on loopback behind a random token and waits for its single answer.</summary>
    internal sealed class BrowserGridServer : IDisposable {
        private static readonly TimeSpan AnswerReadLimit = TimeSpan.FromSeconds(10);
        private const int AccessDenied = 5;
        private readonly HttpListener _listener;
        private readonly byte[] _page;
        private readonly string _token;
        private readonly int _port;
        private readonly long _maxAnswerBytes;

        private BrowserGridServer(HttpListener listener, int port, string token, string page, int rows) {
            _listener = listener;
            _port = port;
            _token = token;
            _page = Encoding.UTF8.GetBytes(page);
            _maxAnswerBytes = MaxAnswerBytes(rows);
        }

        /// <summary>Room for every row index of the grid with its separator: the largest valid answer.</summary>
        internal static long MaxAnswerBytes(int rows) => 64 + (long)Math.Max(rows, 1) * (Math.Max(rows - 1, 0).ToString(CultureInfo.InvariantCulture).Length + 1);

        public Uri Address => new Uri("http://127.0.0.1:" + _port.ToString(CultureInfo.InvariantCulture) + "/" + _token + "/");

        /// <summary>Whether anyone can answer: not an explicit None or Snapshot mode, not a non-interactive session, not CI.</summary>
        /// <remarks>Redirected console output does not matter here: the person answers in the browser, not the console.</remarks>
        internal static bool CanAsk(InteractiveOptions interactive) {
            if (interactive.Mode == InteractionMode.None || interactive.Mode == InteractionMode.Snapshot || interactive.IsNonInteractive) {
                return false;
            }
            return !interactive.ResolveProfile().IsContinuousIntegration;
        }

        public static BrowserGridServer Start(string page, int rows) {
            string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            for (int attempt = 0; ; attempt++) {
                int port = FreePort();
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + "/" + token + "/");
                try {
                    listener.Start();
                    return new BrowserGridServer(listener, port, token, page, rows);
                } catch (HttpListenerException ex) {
                    listener.Close();
                    if (ex.ErrorCode == AccessDenied) {
                        // Retrying cannot change a permission decision; report it once so the caller can fall back.
                        throw new PromptUnavailableException("The browser grid is not allowed to listen on 127.0.0.1 for this account.", ex);
                    }
                    // Otherwise another process took the port between probing and binding: try another one.
                    if (attempt >= 4) {
                        throw new PromptUnavailableException("The browser grid could not listen on 127.0.0.1.", ex);
                    }
                }
            }
        }

        private static int FreePort() {
            TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            try { return ((IPEndPoint)probe.LocalEndpoint).Port; } finally { probe.Stop(); }
        }

        /// <summary>Waits for OK (the chosen row indexes) or Cancel (null).</summary>
        public int[]? WaitForAnswer(TimeSpan? timeout, CancellationToken cancellationToken, Process? launcher = null) {
            DateTime? deadline = timeout.HasValue ? DateTime.UtcNow + timeout.Value : (DateTime?)null;
            Action check = () => {
                cancellationToken.ThrowIfCancellationRequested();
                if (deadline.HasValue && DateTime.UtcNow >= deadline.Value) {
                    throw new TimeoutException("The browser grid was not answered in time.");
                }
                if (launcher != null && LauncherFailed(launcher)) {
                    throw new PromptUnavailableException("The browser could not be opened for the grid. Open " + Address + " or use the screen grid.");
                }
            };
            while (true) {
                Task<HttpListenerContext> next = _listener.GetContextAsync();
                while (!next.Wait(200)) {
                    check();
                }
                if (Handle(next.Result, check, out int[]? answer)) {
                    return answer;
                }
            }
        }

        // A launcher that already exited with a failure code (for example xdg-open without a working browser) never showed
        // the page. One that is still running, or exited cleanly after handing the address to a browser, is fine.
        private static bool LauncherFailed(Process launcher) {
            try {
                return launcher.HasExited && launcher.ExitCode != 0;
            } catch (InvalidOperationException) {
                return false;
            }
        }

        private bool Handle(HttpListenerContext context, Action check, out int[]? answer) {
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
                    string? body = ReadAnswer(request, response, check, _maxAnswerBytes);
                    if (body == null) {
                        return false;
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

        // Reads at most the largest valid answer, gives a sender ten seconds in total, and checks the caller's cancellation
        // and timeout before every read and while a read waits. Returns null after rejecting or abandoning the request,
        // including a connection the browser dropped mid-body, so the grid keeps waiting for a valid answer.
        private static string? ReadAnswer(HttpListenerRequest request, HttpListenerResponse response, Action check, long maxBytes) {
            if (request.ContentLength64 > maxBytes) {
                Respond(response, 413, "text/plain", Encoding.UTF8.GetBytes("Too large"));
                return null;
            }
            DateTime limit = DateTime.UtcNow + AnswerReadLimit;
            using MemoryStream body = new MemoryStream();
            byte[] chunk = new byte[4096];
            while (true) {
                if (!Continue(response, check, limit)) {
                    return null;
                }
                int count;
                try {
                    Task<int> read = request.InputStream.ReadAsync(chunk, 0, chunk.Length);
                    while (!read.Wait(200)) {
                        if (!Continue(response, check, limit)) {
                            return null;
                        }
                    }
                    count = read.Result;
                } catch (Exception ex) when (ex is AggregateException || ex is HttpListenerException || ex is IOException) {
                    response.Abort();
                    return null;
                }
                if (count == 0) {
                    break;
                }
                body.Write(chunk, 0, count);
                if (body.Length > maxBytes) {
                    Respond(response, 413, "text/plain", Encoding.UTF8.GetBytes("Too large"));
                    return null;
                }
            }
            return Encoding.UTF8.GetString(body.ToArray());
        }

        // False when the per-request limit passed; the caller's cancellation and timeout propagate after aborting the request.
        private static bool Continue(HttpListenerResponse response, Action check, DateTime limit) {
            try {
                check();
            } catch {
                response.Abort();
                throw;
            }
            if (DateTime.UtcNow >= limit) {
                response.Abort();
                return false;
            }
            return true;
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

        /// <summary>
        /// Opens a URL with the command named by BROWSER when set, otherwise the platform's default handler. Returns the
        /// launcher process when there is one, so a launcher that fails after starting is noticed while waiting.
        /// </summary>
        internal static bool Launch(Uri address, out Process? launcher) {
            launcher = null;
            try {
                ProcessStartInfo start;
                // BROWSER is the common convention (xdg-open and Python honour it); the first of its separated commands is used.
                string browser = (Environment.GetEnvironmentVariable("BROWSER") ?? string.Empty).Split(Path.PathSeparator)[0].Trim();
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
                launcher = Process.Start(start);
                return true;
            } catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException || ex is FileNotFoundException) {
                launcher?.Dispose();
                launcher = null;
                return false;
            }
        }

        public void Dispose() {
            try { _listener.Stop(); } catch (ObjectDisposedException) { }
            _listener.Close();
        }
    }
}
