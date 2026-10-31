using System;
using System.Globalization;
using ConsoleMonster.Output;

namespace ConsoleMonster.Headless {
    /// <summary>
    /// The process default target from the environment: <c>CONSOLEMONSTER_HEADLESS=120x40</c> runs the process on a headless
    /// terminal (so an unmodified app can be driven without a window), and <c>CONSOLEMONSTER_DEBUG_SOCKET</c> (<c>0</c> for
    /// any free port, or a port number) additionally starts the debug socket for it. Both are off unless set.
    /// </summary>
    internal static class HeadlessEnvironment {
        private static DebugSocket? _socket;

        internal static ITerminalTarget CreateDefaultTarget() {
            string? size = Environment.GetEnvironmentVariable("CONSOLEMONSTER_HEADLESS");
            string? debug = Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEBUG_SOCKET");
            string? token = Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEBUG_TOKEN");
            if (!string.IsNullOrEmpty(size) || !string.IsNullOrEmpty(debug) || !string.IsNullOrEmpty(token)) {
                // The settings are for this process only: a child it starts (a shell, a handoff) gets a normal console and no
                // socket of its own, and never inherits the token.
                foreach (string name in new[] { "CONSOLEMONSTER_HEADLESS", "CONSOLEMONSTER_DEBUG_SOCKET", "CONSOLEMONSTER_DEBUG_TOKEN" }) { Environment.SetEnvironmentVariable(name, null); }
            }
            if (string.IsNullOrWhiteSpace(size) || size == "0" || string.Equals(size, "false", StringComparison.OrdinalIgnoreCase)) {
                if (!string.IsNullOrWhiteSpace(debug)) { Warn("CONSOLEMONSTER_DEBUG_SOCKET drives a headless terminal; set CONSOLEMONSTER_HEADLESS=WIDTHxHEIGHT as well. The socket was not started."); }
                return ConsoleTarget.Instance;
            }
            if (!TryParseSize(size!, out int width, out int height)) {
                Warn("CONSOLEMONSTER_HEADLESS must be WIDTHxHEIGHT (for example 120x40) or 1; using the console.");
                return ConsoleTarget.Instance;
            }
            HeadlessTerminal terminal = HeadlessTerminal.LikeConsole(width, height);
            if (!string.IsNullOrWhiteSpace(debug)) { StartSocket(terminal, debug!.Trim(), token); }
            return terminal;
        }

        internal static bool TryParseSize(string value, out int width, out int height) {
            value = value.Trim();
            if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) { width = 120; height = 40; return true; }
            string[] parts = value.Split('x', 'X');
            width = height = 0;
            return parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out width)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out height)
                && width >= 2 && height >= 2 && width <= 1000 && height <= 1000 && (long)width * height <= 250000;
        }

        private static void StartSocket(HeadlessTerminal terminal, string value, string? token) {
            int port;
            if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) { port = 0; }
            else if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out port) || (port != 0 && (port < 1024 || port > 65535))) {
                Warn("CONSOLEMONSTER_DEBUG_SOCKET must be 0, 1 or a port from 1024 to 65535; the socket was not started.");
                return;
            }
            try {
                DebugSocket socket = DebugSocket.Start(terminal, new DebugSocketOptions { Port = port, Token = string.IsNullOrEmpty(token) ? null : token });
                _socket = socket;
                AppDomain.CurrentDomain.ProcessExit += (sender, args) => socket.Dispose();
                // The token stays in the user's discovery file; only the address is printed.
                Warn("debug socket on 127.0.0.1:" + socket.Endpoint.Port.ToString(CultureInfo.InvariantCulture) + " (discovery file " + socket.DiscoveryPath + ")");
            } catch (Exception ex) when (ex is System.Net.Sockets.SocketException || ex is ArgumentException || ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is PlatformNotSupportedException) {
                Warn("the debug socket could not start: " + ex.Message);
            }
        }

        private static void Warn(string message) {
            try { Console.Error.WriteLine("ConsoleMonster: " + message); }
            catch (Exception ex) when (ex is System.IO.IOException || ex is ObjectDisposedException) { }
        }

        // Keeps the environment-started socket reachable for the life of the process.
        internal static DebugSocket? Socket => _socket;
    }
}
