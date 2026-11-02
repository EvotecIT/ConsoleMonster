using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleMonster.Headless;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Headless {
    /// <summary>The browser host over real HTTP: a self-contained page, a live screen stream, and input accepted only from that page.</summary>
    [Collection("Live sessions")]
    public sealed class BrowserHostTests {
        /// <summary>The page has no external resources, the stream carries the screen, and the page's keys drive the app.</summary>
        [Fact]
        public async Task Host_ServesPageStreamsScreenAndAcceptsPageInput() {
            using HeadlessTerminal terminal = new HeadlessTerminal(60, 12);
            using BrowserHost host = BrowserHost.Start(terminal, new BrowserHostOptions { Title = "Demo <app>" });
            TextInput name = new TextInput("name") { Label = "Name" };
            AppBinding greeting = new AppBinding(state => TextBlock.Plain("Hello " + state.Get<string>("name")));
            Task<AppState> run = ConsoleApp.RunAsync(new Rows(name, greeting), options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
            terminal.WaitForText("Hello", TimeSpan.FromSeconds(10));
            string token = host.Address.Query.Substring(3);
            string origin = "http://127.0.0.1:" + host.Address.Port;

            Response page = Http(host, "GET", host.Address.PathAndQuery);
            Assert.Equal(200, page.Status);
            Assert.Contains("Demo &lt;app&gt;", page.Body);
            Assert.DoesNotMatch(new Regex("(src|href)\\s*=\\s*[\"']?(https?:)?//", RegexOptions.IgnoreCase), page.Body);
            Assert.Contains("script-src 'nonce-", page.Headers);

            using (TcpClient events = Open(host, "GET", "/events?t=" + token, null, null, null)) {
                string first = ReadUntil(events.GetStream(), "\n\n");
                Assert.Contains("text/event-stream", first);
                string data = first.Contains("data: ") ? first : ReadUntil(events.GetStream(), "\n\n");
                Assert.Contains("Hello", data);
                Assert.Equal(204, Http(host, "POST", "/input?t=" + token, "{\"text\":\"Ada\"}", origin, token).Status);
                Assert.Contains("Hello Ada", WaitForEvent(events.GetStream(), "Hello Ada"));
            }
            Assert.Equal(204, Http(host, "POST", "/input?t=" + token, "{\"keys\":\"Esc\"}", origin, token).Status);
            Assert.Equal("Ada", (await run)["name"]);
        }

        /// <summary>Another host name (DNS rebinding), another origin, a missing token or a wrong token is refused before anything reaches the app.</summary>
        [Fact]
        public void Host_RefusesOtherHostsOriginsAndTokens() {
            using HeadlessTerminal terminal = new HeadlessTerminal(40, 8);
            using BrowserHost host = BrowserHost.Start(terminal);
            string token = host.Address.Query.Substring(3);
            string origin = "http://127.0.0.1:" + host.Address.Port;
            Assert.Equal(403, Http(host, "GET", host.Address.PathAndQuery, hostHeader: "attacker.example:" + host.Address.Port).Status);
            Assert.Equal(403, Http(host, "GET", "/").Status);
            Assert.Equal(403, Http(host, "GET", "/events?t=" + new string('0', token.Length)).Status);
            Assert.Equal(403, Http(host, "POST", "/input?t=" + token, "{\"text\":\"x\"}", "https://attacker.example", token).Status);
            Assert.Equal(403, Http(host, "POST", "/input?t=" + token, "{\"text\":\"x\"}", null, token).Status);
            Assert.Equal(403, Http(host, "POST", "/input?t=" + token, "{\"text\":\"x\"}", origin, null).Status);
            Assert.Equal(204, Http(host, "POST", "/input?t=" + token, "{\"text\":\"x\"}", origin, token).Status);
        }

        private sealed class Response {
            internal int Status;
            internal string Headers = string.Empty;
            internal string Body = string.Empty;
        }

        private static Response Http(BrowserHost server, string method, string path, string? body = null, string? origin = null, string? token = null, string? hostHeader = null) {
            using TcpClient client = Open(server, method, path, body, origin, token, hostHeader);
            using StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            string all = reader.ReadToEnd();
            int split = all.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            Response response = new Response { Headers = all.Substring(0, split), Body = all.Substring(split + 4) };
            response.Status = int.Parse(response.Headers.Split(' ')[1]);
            return response;
        }

        private static TcpClient Open(BrowserHost host, string method, string path, string? body, string? origin, string? token, string? hostHeader = null) {
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Loopback, host.Address.Port);
            client.GetStream().ReadTimeout = 15000;
            byte[] content = Encoding.UTF8.GetBytes(body ?? string.Empty);
            StringBuilder request = new StringBuilder(method + " " + path + " HTTP/1.1\r\nHost: " + (hostHeader ?? "127.0.0.1:" + host.Address.Port) + "\r\n");
            if (origin != null) { request.Append("Origin: ").Append(origin).Append("\r\n"); }
            if (token != null) { request.Append("X-ConsoleMonster-Token: ").Append(token).Append("\r\n"); }
            if (body != null) { request.Append("Content-Type: application/json\r\nContent-Length: ").Append(content.Length).Append("\r\n"); }
            byte[] head = Encoding.ASCII.GetBytes(request.Append("\r\n").ToString());
            client.GetStream().Write(head, 0, head.Length);
            client.GetStream().Write(content, 0, content.Length);
            return client;
        }

        private static string ReadUntil(NetworkStream stream, string end) {
            StringBuilder text = new StringBuilder();
            byte[] one = new byte[1];
            while (!text.ToString().Replace("\r\n", "\n").EndsWith(end, StringComparison.Ordinal)) {
                if (stream.Read(one, 0, 1) == 0) { break; }
                text.Append((char)one[0]);
            }
            return text.ToString().Replace("\r\n", "\n");
        }

        private static string WaitForEvent(NetworkStream stream, string text) {
            for (int i = 0; i < 100; i++) {
                string message = ReadUntil(stream, "\n\n");
                if (message.Contains(text)) { return message; }
            }
            return string.Empty;
        }
    }
}
