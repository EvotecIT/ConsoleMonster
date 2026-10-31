using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Serialization;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.Headless {
    /// <summary>Settings for <see cref="DebugSocket.Start"/>.</summary>
    public sealed class DebugSocketOptions {
        /// <summary>The port on 127.0.0.1 (default 0: the system picks a free one; read it from <see cref="DebugSocket.Endpoint"/> or the discovery file).</summary>
        public int Port { get; set; }

        /// <summary>The token clients must present, at least 16 characters; null generates a random one per start.</summary>
        public string? Token { get; set; }

        /// <summary>
        /// The folder for the discovery file (<c>&lt;process id&gt;.json</c> with the port and token), or null for
        /// <c>%LOCALAPPDATA%\ConsoleMonster\Debug</c> (the user's local application data elsewhere). The file is removed when
        /// the socket stops.
        /// </summary>
        public string? DiscoveryFolder { get; set; }

        /// <summary>Additional regular expressions whose matches are masked on every screen sent (the recording's built-in secret patterns always apply).</summary>
        public IList<string> Redact { get; } = new List<string>();
    }

    /// <summary>
    /// An opt-in agent socket for inspecting and driving the app on a <see cref="HeadlessTerminal"/>, the way browser tests
    /// drive a page: read the screen as text or PNG, read the control tree with bounds and focus, and send keys, text,
    /// resizes and clicks. It listens on 127.0.0.1 only, requires a per-run token, masks secret patterns on every screen it
    /// sends, and never reports input values. The wire protocol (one JSON object per line) is described in
    /// <c>Docs/Headless-Driver.md</c>.
    /// </summary>
    /// <remarks>
    /// Nothing listens unless the host starts it: call <see cref="Start"/>, or set <c>CONSOLEMONSTER_HEADLESS=120x40</c>
    /// together with <c>CONSOLEMONSTER_DEBUG_SOCKET=0</c> (any free port) or a port number, which starts one for the
    /// process's default headless terminal.
    /// </remarks>
    public sealed partial class DebugSocket : IDisposable {
        /// <summary>The protocol version reported by <c>hello</c>.</summary>
        public const int ProtocolVersion = 2;
        private int _layoutListening;
        private const int MaxLine = 1024 * 1024;
        private const int MaxHelloLine = 4096;
        private static readonly HashSet<string> Published = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HeadlessTerminal _terminal;
        private readonly RecordingRedactor _redactor;
        private readonly LoopbackListener _listener;
        private readonly string _token;
        private readonly CancellationTokenSource _stopped = new CancellationTokenSource();
        private readonly CancellationToken _stopToken;
        private int _disposed;

        private DebugSocket(HeadlessTerminal terminal, DebugSocketOptions options) {
            _terminal = terminal;
            _stopToken = _stopped.Token;
            _token = options.Token ?? LoopbackListener.NewToken();
            if (_token.Length < 16) { throw new ArgumentException("The debug socket token needs at least 16 characters.", nameof(options)); }
            _redactor = new RecordingRedactor(options.Redact);
            _listener = new LoopbackListener(options.Port, 4, "ConsoleMonster debug socket", Serve);
            try { DiscoveryPath = WriteDiscovery(options.DiscoveryFolder); }
            catch { _listener.Dispose(); throw; }
            // While the socket runs, app frames note why each control has its size for its tree command.
            _layoutListening = 1;
            LayoutNotes.Listen();
        }

        /// <summary>Starts a debug socket for a headless terminal.</summary>
        /// <param name="terminal">The terminal whose screen and input the socket exposes.</param>
        /// <param name="options">Port, token, discovery folder and extra redaction patterns.</param>
        /// <returns>The running socket; dispose it to stop listening and remove the discovery file.</returns>
        public static DebugSocket Start(HeadlessTerminal terminal, DebugSocketOptions? options = null) =>
            new DebugSocket(terminal ?? throw new ArgumentNullException(nameof(terminal)), options ?? new DebugSocketOptions());

        /// <summary>The address the socket listens on: always 127.0.0.1, with the port clients connect to.</summary>
        public IPEndPoint Endpoint => _listener.Endpoint;

        /// <summary>The token a client sends in its <c>hello</c> request.</summary>
        public string Token => _token;

        /// <summary>The discovery file with the process id, port and token.</summary>
        public string DiscoveryPath { get; }

        /// <summary>Stops listening, closes open connections and removes the discovery file.</summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
            _stopped.Cancel();
            if (Interlocked.Exchange(ref _layoutListening, 0) == 1) { LayoutNotes.Unlisten(); }
            _listener.Dispose();
            _stopped.Dispose();
            lock (Published) {
                if (!Published.Remove(DiscoveryPath)) { return; }
                try { File.Delete(DiscoveryPath); } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
            }
        }

        // The token is the only secret. On Windows the user's local application data is private to that user; elsewhere the
        // folder and the file are created readable by this user alone, which needs .NET 8 or later.
        private string WriteDiscovery(string? folder) {
            string directory = folder ?? Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ConsoleMonster"), "Debug");
            Process self = Process.GetCurrentProcess();
            string path = Path.GetFullPath(Path.Combine(directory, self.Id.ToString(CultureInfo.InvariantCulture) + ".json"));
            Dictionary<string, object?> data = new Dictionary<string, object?> {
                ["protocol"] = ProtocolVersion, ["pid"] = self.Id, ["process"] = self.ProcessName, ["port"] = Endpoint.Port, ["token"] = _token
            };
            byte[] bytes = new UTF8Encoding(false).GetBytes(MiniJson.WriteCompact(data) + "\n");
            lock (Published) {
                // One socket per process and folder: a second would overwrite the first one's address and token.
                if (Published.Contains(path)) { throw new InvalidOperationException("A debug socket in this process already published " + path + "."); }
#if NET8_0_OR_GREATER
                if (!OperatingSystem.IsWindows()) {
                    Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    // A file left by an earlier process with the same id is replaced, never reused with its old mode.
                    File.Delete(path);
                    FileStreamOptions create = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read, UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite };
                    using (FileStream file = new FileStream(path, create)) { file.Write(bytes, 0, bytes.Length); }
                    Published.Add(path);
                    return path;
                }
#else
                if (Environment.OSVersion.Platform != PlatformID.Win32NT) { throw new PlatformNotSupportedException("The debug socket needs .NET 8 or later on this platform, to keep its token readable by this user alone."); }
#endif
                Directory.CreateDirectory(directory);
                using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { file.Write(bytes, 0, bytes.Length); }
                Published.Add(path);
                return path;
            }
        }

        private void Serve(TcpClient client) {
            client.NoDelay = true;
            NetworkStream stream = client.GetStream();
            bool authenticated = false;
            // A client has ten seconds in total to say hello, however slowly it sends; an idle agent may then think for a while.
            using (System.Threading.Timer deadline = new System.Threading.Timer(_ => { if (!Volatile.Read(ref authenticated)) { client.Close(); } }, null, 10000, System.Threading.Timeout.Infinite)) {
                stream.ReadTimeout = 30 * 60 * 1000;
                ServeRequests(stream, ref authenticated);
            }
        }

        private void ServeRequests(NetworkStream stream, ref bool authenticated) {
            while (true) {
                string? line = ReadLine(stream, Volatile.Read(ref authenticated) ? MaxLine : MaxHelloLine);
                if (line == null) { return; }
                if (line.Trim().Length == 0) { continue; }
                Dictionary<string, object?> reply;
                object? id = null;
                try {
                    // Typed numbers, so a numeric id comes back as a number.
                    Dictionary<string, object?> request = MiniJson.Read(line, preserveNumberTypes: true) as Dictionary<string, object?> ?? throw new FormatException("A request is a JSON object.");
                    request.TryGetValue("id", out id);
                    string command = Text(request, "cmd") ?? throw new FormatException("The request has no cmd.");
                    if (!authenticated) {
                        if (command != "hello" || !LoopbackListener.TokenEquals(Text(request, "token"), _token)) {
                            Send(stream, Error(id, "unauthorized", "Send hello with the token from the discovery file first."));
                            return;
                        }
                        Volatile.Write(ref authenticated, true);
                    }
                    reply = Handle(command, request);
                } catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is NotSupportedException || ex is TimeoutException || ex is OverflowException) {
                    // Before hello, any failure ends the connection.
                    if (!Volatile.Read(ref authenticated)) { Send(stream, Error(id, "unauthorized", "Send hello with the token from the discovery file first.")); return; }
                    reply = Error(id, "bad-request", ex.Message);
                } catch (Exception ex) when (!(ex is IOException) && !(ex is SocketException) && !(ex is ObjectDisposedException)) {
                    // A failing command reports its type only: messages from app code could carry data.
                    reply = Error(id, "failed", ex.GetType().Name);
                }
                if (!reply.ContainsKey("id")) { reply["id"] = id; }
                Send(stream, reply);
            }
        }

        private static void Send(NetworkStream stream, Dictionary<string, object?> reply) {
            byte[] bytes = new UTF8Encoding(false).GetBytes(MiniJson.WriteCompact(reply) + "\n");
            stream.Write(bytes, 0, bytes.Length);
        }

        private static Dictionary<string, object?> Error(object? id, string code, string message) =>
            new Dictionary<string, object?> { ["id"] = id, ["ok"] = false, ["error"] = code, ["message"] = message };

        // One request per line, at most 1 MiB (4 KiB before hello), so a client cannot make the process buffer without bound.
        private static string? ReadLine(NetworkStream stream, int maxLine) {
            MemoryStream line = new MemoryStream();
            byte[] one = new byte[1];
            while (true) {
                int read;
                try { read = stream.Read(one, 0, 1); }
                catch (IOException) { return null; }
                if (read == 0) { return line.Length == 0 ? null : Encoding.UTF8.GetString(line.ToArray()); }
                if (one[0] == (byte)'\n') { return Encoding.UTF8.GetString(line.ToArray()).TrimEnd('\r'); }
                if (line.Length >= maxLine) { return null; }
                line.WriteByte(one[0]);
            }
        }

        private static string? Text(Dictionary<string, object?> request, string name) =>
            request.TryGetValue(name, out object? value) ? value as string : null;

        private static int Number(Dictionary<string, object?> request, string name, int? fallback, int minimum, int maximum) {
            if (!request.TryGetValue(name, out object? value) || value == null) { return fallback ?? throw new FormatException("'" + name + "' is required."); }
            string? text = value is string s ? s : value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : null;
            if (text == null || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)) {
                throw new FormatException("'" + name + "' must be a whole number.");
            }
            if (number < minimum || number > maximum) { throw new FormatException("'" + name + "' must be between " + minimum.ToString(CultureInfo.InvariantCulture) + " and " + maximum.ToString(CultureInfo.InvariantCulture) + "."); }
            return number;
        }
    }
}
