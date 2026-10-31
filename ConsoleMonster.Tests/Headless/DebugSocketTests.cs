using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ConsoleMonster.Headless;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Serialization;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Headless {
    /// <summary>The agent socket's access model and wire protocol, exercised over real TCP connections.</summary>
    [Collection("Live sessions")]
    public sealed class DebugSocketTests : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-debug-" + Guid.NewGuid().ToString("N"));

        public DebugSocketTests() { Directory.CreateDirectory(_folder); }

        public void Dispose() { Directory.Delete(_folder, true); }

        /// <summary>Nothing listens unless the process opts in, and the debug variable alone never starts a socket on a real console.</summary>
        [Fact]
        public void EnvironmentWithoutHeadless_StartsNoSocket() {
            string? headless = Environment.GetEnvironmentVariable("CONSOLEMONSTER_HEADLESS");
            string? debug = Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEBUG_SOCKET");
            TextWriter error = Console.Error;
            try {
                Console.SetError(TextWriter.Null);
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_HEADLESS", null);
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_DEBUG_SOCKET", null);
                Assert.Same(ConsoleTarget.Instance, HeadlessEnvironment.CreateDefaultTarget());
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_DEBUG_SOCKET", "0");
                Assert.Same(ConsoleTarget.Instance, HeadlessEnvironment.CreateDefaultTarget());
                Assert.Null(HeadlessEnvironment.Socket);
            } finally {
                Console.SetError(error);
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_HEADLESS", headless);
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_DEBUG_SOCKET", debug);
            }
        }

        /// <summary>The socket is bound to loopback (unreachable on the machine's other addresses) and closes a connection that does not present the token first.</summary>
        [Fact]
        public async Task Socket_RefusesOtherAddressesAndUnauthenticatedClients() {
            using HeadlessTerminal terminal = new HeadlessTerminal(40, 10);
            using DebugSocket socket = DebugSocket.Start(terminal, new DebugSocketOptions { DiscoveryFolder = _folder });
            Assert.True(IPAddress.IsLoopback(socket.Endpoint.Address));
            IPAddress? external = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses).Select(unicast => unicast.Address)
                .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address));
            if (external != null) {
                using TcpClient remote = new TcpClient();
                Task connect = remote.ConnectAsync(external, socket.Endpoint.Port);
                // Refused at once, or dropped by a firewall (still unreachable); a completed connection would be the failure.
                await Task.WhenAny(connect, Task.Delay(TimeSpan.FromSeconds(3)));
                Assert.False(connect.Status == TaskStatus.RanToCompletion, "The socket accepted a connection on " + external + ".");
            }

            using (Client wrongToken = new Client(socket.Endpoint.Port)) {
                Dictionary<string, object?> reply = wrongToken.Call(new Dictionary<string, object?> { ["cmd"] = "hello", ["token"] = new string('0', socket.Token.Length) });
                Assert.Equal("unauthorized", reply["error"]);
                Assert.True(wrongToken.Closed());
            }
            using (Client skipped = new Client(socket.Endpoint.Port)) {
                Assert.Equal("unauthorized", skipped.Call(new Dictionary<string, object?> { ["cmd"] = "screen" })["error"]);
                Assert.True(skipped.Closed());
            }
            using (Client http = new Client(socket.Endpoint.Port)) {
                Assert.Equal("unauthorized", http.CallRaw("GET / HTTP/1.1")["error"]);
                Assert.True(http.Closed());
            }
            Dictionary<string, object?> discovery = (Dictionary<string, object?>)MiniJson.Read(File.ReadAllText(socket.DiscoveryPath))!;
            Assert.Equal(socket.Token, discovery["token"]);
            socket.Dispose();
            Assert.False(File.Exists(socket.DiscoveryPath));
        }

        /// <summary>An agent drives an app end to end: hello, type, wait, read the screen and tree, resize, click and quit.</summary>
        [Fact]
        public async Task Protocol_DrivesAnAppEndToEnd() {
            using HeadlessTerminal terminal = new HeadlessTerminal(60, 16);
            using DebugSocket socket = DebugSocket.Start(terminal, new DebugSocketOptions { DiscoveryFolder = _folder });
            TextInput name = new TextInput("name") { Label = "Name" };
            Toggle agree = new Toggle("agree") { Label = "Agree" };
            AppBinding greeting = new AppBinding(state => TextBlock.Plain("Hello " + state.Get<string>("name") + (state.Get<bool>("agree") ? " (agreed)" : string.Empty)));
            Task<AppState> run = ConsoleApp.RunAsync(new Rows(name, agree, greeting), options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
            using Client client = new Client(socket.Endpoint.Port);
            Dictionary<string, object?> hello = client.Call(new Dictionary<string, object?> { ["id"] = "1", ["cmd"] = "hello", ["token"] = socket.Token });
            Assert.Equal(true, hello["ok"]);
            Assert.Equal("1", hello["id"]);
            Assert.Equal("60", hello["width"]);

            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "text", ["text"] = "Ada" })["ok"]);
            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "wait", ["text"] = "Hello Ada", ["timeout"] = "10000" })["found"]);
            Assert.Contains("Hello Ada", (string)client.Call(new Dictionary<string, object?> { ["cmd"] = "screen" })["text"]!);

            Dictionary<string, object?> tree = client.Call(new Dictionary<string, object?> { ["cmd"] = "tree" });
            Assert.Equal("name", tree["focus"]);
            List<Dictionary<string, object?>> inputs = Flatten((Dictionary<string, object?>)tree["root"]!).Where(node => node.ContainsKey("key")).ToList();
            Dictionary<string, object?> toggle = inputs.Single(node => (string?)node["key"] == "agree");
            Assert.DoesNotContain(inputs, node => node.ContainsKey("value"));
            Dictionary<string, object?> bounds = (Dictionary<string, object?>)toggle["bounds"]!;
            Assert.Equal(true, tree["layout"]);
            Dictionary<string, object?> layout = (Dictionary<string, object?>)toggle["layout"]!;
            Assert.Equal("auto", layout["rule"]);
            Assert.Equal(bounds["height"], ((Dictionary<string, object?>)layout["arranged"]!)["height"]);

            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "click", ["x"] = bounds["x"], ["y"] = bounds["y"] })["ok"]);
            System.Diagnostics.Stopwatch focusWait = System.Diagnostics.Stopwatch.StartNew();
            while ((string?)client.Call(new Dictionary<string, object?> { ["cmd"] = "tree" })["focus"] != "agree") {
                Assert.True(focusWait.Elapsed < TimeSpan.FromSeconds(10), "The click did not move focus to the toggle.");
                System.Threading.Thread.Sleep(20);
            }
            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "keys", ["keys"] = "Space" })["ok"]);
            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "wait", ["text"] = "(agreed)", ["timeout"] = "10000" })["found"]);

            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "resize", ["width"] = "50", ["height"] = "12" })["ok"]);
            Assert.Equal("50", client.Call(new Dictionary<string, object?> { ["cmd"] = "screen" })["width"]);
            Assert.Equal(false, client.Call(new Dictionary<string, object?> { ["cmd"] = "wait", ["text"] = "never shown", ["timeout"] = "50" })["found"]);
            Assert.Equal("bad-request", client.Call(new Dictionary<string, object?> { ["cmd"] = "explode" })["error"]);
            Assert.Equal("bad-request", client.Call(new Dictionary<string, object?> { ["cmd"] = "keys", ["keys"] = "Down*999999999" })["error"]);
            Assert.Equal("bad-request", client.Call(new Dictionary<string, object?> { ["cmd"] = "click", ["y"] = "1" })["error"]);

            Assert.Equal(true, client.Call(new Dictionary<string, object?> { ["cmd"] = "keys", ["keys"] = "Escape" })["ok"]);
            AppState state = await run;
            Assert.Equal("Ada", state["name"]);
            Assert.Equal(true, state["agree"]);
        }

        /// <summary>Screens leave the process with secret patterns masked, as recordings do.</summary>
        [Fact]
        public void Screen_MasksSecretPatterns() {
            using HeadlessTerminal terminal = new HeadlessTerminal(70, 6);
            using DebugSocket socket = DebugSocket.Start(terminal, new DebugSocketOptions { DiscoveryFolder = _folder });
            using (Terminal.Use(terminal)) { Terminal.Write(TextBlock.Plain("deploy password=synthetic-0123456789abcdef0123")); }
            terminal.WaitForText("deploy password", TimeSpan.FromSeconds(5));
            using Client client = new Client(socket.Endpoint.Port);
            client.Call(new Dictionary<string, object?> { ["cmd"] = "hello", ["token"] = socket.Token });
            string text = (string)client.Call(new Dictionary<string, object?> { ["cmd"] = "screen" })["text"]!;
            Assert.DoesNotContain("synthetic-0123456789abcdef0123", text);
            Assert.Contains("****", text);
            // Waiting is matched against the masked screen too, so it cannot confirm a guess.
            Assert.Equal(false, client.Call(new Dictionary<string, object?> { ["cmd"] = "wait", ["text"] = "synthetic-0123", ["timeout"] = "50" })["found"]);
            // A link target holding a secret is dropped before a screen leaves the process (the browser host sends links).
            ConsoleMonster.Canvas.CellBuffer linked = new ConsoleMonster.Canvas.CellBuffer(4, 1);
            linked.Set(0, 0, "x", ConsoleMonster.Styling.Style.Plain.WithLink("https://example.test/?token=synthetic-abc123"));
            linked.Set(1, 0, "y", ConsoleMonster.Styling.Style.Plain.WithLink("https://example.test/docs"));
            ConsoleMonster.Canvas.CellBuffer masked = ScreenRedaction.Apply(linked, new ConsoleMonster.Sessions.RecordingRedactor(Array.Empty<string>()));
            Assert.Null(masked[0, 0].Style.Link);
            Assert.Equal("https://example.test/docs", masked[1, 0].Style.Link);
        }

        private static IEnumerable<Dictionary<string, object?>> Flatten(Dictionary<string, object?> node) {
            yield return node;
            if (node.TryGetValue("children", out object? children)) {
                foreach (object? child in (List<object?>)children!) {
                    foreach (Dictionary<string, object?> item in Flatten((Dictionary<string, object?>)child!)) { yield return item; }
                }
            }
        }

        private sealed class Client : IDisposable {
            private readonly TcpClient _client = new TcpClient();
            private readonly NetworkStream _stream;
            private readonly StreamReader _reader;

            internal Client(int port) {
                _client.Connect(IPAddress.Loopback, port);
                _stream = _client.GetStream();
                _stream.ReadTimeout = 15000;
                _reader = new StreamReader(_stream, new UTF8Encoding(false));
            }

            internal Dictionary<string, object?> Call(Dictionary<string, object?> request) {
                byte[] line = new UTF8Encoding(false).GetBytes(MiniJson.WriteCompact(request) + "\n");
                _stream.Write(line, 0, line.Length);
                string reply = _reader.ReadLine() ?? throw new IOException("The socket closed.");
                return (Dictionary<string, object?>)MiniJson.Read(reply)!;
            }

            internal Dictionary<string, object?> CallRaw(string line) {
                byte[] bytes = new UTF8Encoding(false).GetBytes(line + "\n");
                _stream.Write(bytes, 0, bytes.Length);
                return (Dictionary<string, object?>)MiniJson.Read(_reader.ReadLine() ?? throw new IOException("The socket closed."))!;
            }

            internal bool Closed() {
                try { return _reader.ReadLine() == null; }
                catch (IOException) { return true; }
            }

            public void Dispose() { _client.Close(); }
        }
    }
}
