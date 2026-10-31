using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace ConsoleMonster.Headless {
    /// <summary>
    /// The listening half of the browser host and the debug socket: bound to 127.0.0.1 only (never a wildcard or external
    /// address), exclusive where the platform supports it, with a small connection limit. A connection whose remote end is
    /// not loopback is closed before any byte is read.
    /// </summary>
    internal sealed class LoopbackListener : IDisposable {
        private readonly TcpListener _listener;
        private readonly Action<TcpClient> _handle;
        private readonly int _maxConnections;
        private readonly object _gate = new object();
        private readonly HashSet<TcpClient> _clients = new HashSet<TcpClient>();
        private readonly Thread _accept;
        private volatile bool _disposed;

        internal LoopbackListener(int port, int maxConnections, string name, Action<TcpClient> handle) {
            if (port < 0 || port > 65535) { throw new ArgumentOutOfRangeException(nameof(port)); }
            _handle = handle;
            _maxConnections = maxConnections;
            _listener = new TcpListener(IPAddress.Loopback, port);
            try { _listener.ExclusiveAddressUse = true; } catch (Exception ex) when (ex is SocketException || ex is PlatformNotSupportedException || ex is InvalidOperationException) { }
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _accept = new Thread(Accept) { IsBackground = true, Name = name };
            _accept.Start();
        }

        internal int Port { get; }

        internal IPEndPoint Endpoint => (IPEndPoint)_listener.LocalEndpoint;

        private void Accept() {
            while (!_disposed) {
                TcpClient client;
                try { client = _listener.AcceptTcpClient(); }
                catch (SocketException) when (!_disposed) {
                    // A connection that failed while being accepted; keep listening for the next one.
                    Thread.Sleep(10);
                    continue;
                } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException || ex is InvalidOperationException) { return; }
                try {
                    if (!(client.Client.RemoteEndPoint is IPEndPoint remote) || !IPAddress.IsLoopback(remote.Address)) {
                        client.Close();
                        continue;
                    }
                } catch (SocketException) {
                    // The peer may reset after accept, before its address can be read.
                    client.Close();
                    continue;
                }
                lock (_gate) {
                    if (_disposed || _clients.Count >= _maxConnections) { client.Close(); continue; }
                    _clients.Add(client);
                }
                Thread worker = new Thread(() => Serve(client)) { IsBackground = true, Name = _accept.Name + " connection" };
                worker.Start();
            }
        }

        private void Serve(TcpClient client) {
            try { _handle(client); }
            catch (Exception) {
                // One connection's failure (a peer that went away, a stopped listener, a malformed request) must never
                // end the app it inspects; nothing else depends on this connection.
            } finally {
                lock (_gate) { _clients.Remove(client); }
                client.Close();
            }
        }

        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            _listener.Stop();
            TcpClient[] open;
            lock (_gate) { open = new TcpClient[_clients.Count]; _clients.CopyTo(open); }
            foreach (TcpClient client in open) { client.Close(); }
            _accept.Join(TimeSpan.FromSeconds(2));
        }

        internal static string NewToken() {
            byte[] bytes = new byte[24];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) { random.GetBytes(bytes); }
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        // Equal-time comparison, so a wrong token reveals nothing about how much of it matched.
        internal static bool TokenEquals(string? supplied, string expected) {
            if (supplied == null || supplied.Length != expected.Length) { return false; }
            int difference = 0;
            for (int i = 0; i < expected.Length; i++) { difference |= supplied[i] ^ expected[i]; }
            return difference == 0;
        }
    }
}
