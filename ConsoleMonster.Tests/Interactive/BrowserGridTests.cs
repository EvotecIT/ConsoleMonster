using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The browser grid: a loopback page behind a one-time address that returns the chosen original objects.</summary>
    public sealed class BrowserGridTests {
        private sealed class Server {
            public Server(string name, int cpu) { Name = name; Cpu = cpu; }
            public string Name { get; }
            public int Cpu { get; }
        }

        private static Grid Servers(GridSelection selection = GridSelection.Multiple) {
            Grid grid = new Grid("Servers") { OutputMode = selection, Presentation = new PresentationOptions { Culture = CultureInfo.InvariantCulture } };
            grid.Column("Name");
            grid.Column(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grid.Column("CPU");
            Server[] servers = { new Server("web01", 12), new Server("sql01", 85), new Server("dc01", 3) };
            foreach (Server server in servers) {
                grid.Row(server, server.Name, server.Cpu > 80 ? "Fail" : "Pass", server.Cpu);
            }
            return grid;
        }

        // An interactive session with a fixed terminal, whatever the CI environment of the test run says.
        private static GridOptions Interactive(InteractionMode? mode = null, bool nonInteractive = false, bool ci = false) => new GridOptions {
            Interactive = new InteractiveOptions {
                Mode = mode,
                NonInteractive = nonInteractive,
                Profile = ci ? new TerminalProfile(ColorDepth.TrueColor, GlyphTier.Full, OutputMode.Interactive, 120, 40, TerminalKind.Unknown, true, false, true, true, null, null, null, false, ThemeMode.Auto, CultureInfo.InvariantCulture) : TerminalProfile.Fixed(120, 40)
            }
        };

        // A stand-in browser: it loads the page, then sends one answer, all on another thread like a real browser.
        private static BrowserGridOptions Browser(Func<HttpClient, Uri, Task> visit, Action<string>? page = null) => new BrowserGridOptions {
            Timeout = TimeSpan.FromSeconds(30),
            Open = address => {
                _ = Task.Run(async () => {
                    using HttpClient client = new HttpClient();
                    string html = await client.GetStringAsync(address).ConfigureAwait(false);
                    page?.Invoke(html);
                    await visit(client, address).ConfigureAwait(false);
                });
                return true;
            }
        };

        private static Task<HttpResponseMessage> Answer(HttpClient client, Uri address, string body) =>
            client.PostAsync(new Uri(address, "answer"), new StringContent(body, Encoding.UTF8, "text/plain"));

        private static string Page(Grid grid) {
            string html = string.Empty;
            grid.ShowInBrowser(Interactive(), Browser((client, address) => Answer(client, address, "cancel"), page => html = page));
            return html;
        }

        [Fact]
        public void OkReturnsTheChosenOriginalObjectsInTheOrderTheyWerePicked() {
            Grid grid = Servers();
            GridResult result = grid.ShowInBrowser(Interactive(), Browser((client, address) => Answer(client, address, "ok:1,0")));

            Assert.True(result.Accepted);
            Assert.Equal(new object?[] { grid.Rows[1].Item, grid.Rows[0].Item }, result.Items.ToArray());
        }

        [Fact]
        public void CancelReturnsNothing() {
            GridResult result = Servers().ShowInBrowser(Interactive(), Browser((client, address) => Answer(client, address, "cancel")));

            Assert.False(result.Accepted);
            Assert.Empty(result.Items);
        }

        [Theory]
        [InlineData(GridSelection.Single, 1)]
        [InlineData(GridSelection.None, 0)]
        public void OutputModeLimitsWhatReturns(GridSelection selection, int expected) {
            GridResult result = Servers(selection).ShowInBrowser(Interactive(), Browser((client, address) => Answer(client, address, "ok:2,1")));

            Assert.True(result.Accepted);
            Assert.Equal(expected, result.Items.Count);
        }

        [Fact]
        public void OnlyTheExactAddressIsServed() {
            HttpStatusCode otherPath = 0, otherHost = 0;
            Grid grid = Servers();
            GridResult result = grid.ShowInBrowser(Interactive(), Browser(async (client, address) => {
                otherPath = (await client.GetAsync(new Uri(address, "/not-the-token/")).ConfigureAwait(false)).StatusCode;
                using HttpRequestMessage renamed = new HttpRequestMessage(HttpMethod.Post, new Uri(address, "answer")) { Content = new StringContent("ok:0") };
                renamed.Headers.Host = "attacker.example";
                otherHost = (await client.SendAsync(renamed).ConfigureAwait(false)).StatusCode;
                await Answer(client, address, "ok:2").ConfigureAwait(false);
            }));

            // Windows answers the other host with this server's 403; the managed listener elsewhere refuses it itself.
            Assert.Equal(HttpStatusCode.NotFound, otherPath);
            Assert.InRange((int)otherHost, 400, 499);
            Assert.Same(grid.Rows[2].Item, result.Items.Single());
        }

        [Fact]
        public void ThePageCarriesPresentedValuesAndCannotBeEscapedByRowText() {
            Grid grid = new Grid("Report") { Presentation = new PresentationOptions { Culture = CultureInfo.InvariantCulture } };
            grid.Column(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grid.Column("Note");
            grid.Row("item", "Fail", "</script><script>alert(1)</script>");
            string html = Page(grid);

            Assert.Contains("\"r\":\"fail\"", html);
            Assert.DoesNotContain("</script><script>alert(1)", html);
            Assert.Contains("\\u003c/script\\u003e", html);
        }

        [Fact]
        public void ColumnToneRulesColourTheirCells() {
            Grid grid = new Grid("Load") { Presentation = new PresentationOptions { Culture = CultureInfo.InvariantCulture } };
            TableColumn cpu = new TableColumn("CPU");
            cpu.RoleRules.Add(RoleRule.Above(80, ThemeToken.Fail));
            grid.Column(cpu);
            grid.Row("busy", 95);
            grid.Row("idle", 5);
            string html = Page(grid);

            Assert.Contains("{\"t\":\"95\",\"k\":1,\"r\":\"fail\"}", html);
            Assert.Contains("{\"t\":\"5\",\"k\":0}", html);
        }

        [Fact]
        public void LazyCellsShowTheirValuesNotPlaceholders() {
            Grid grid = new Grid("Sizes");
            grid.Column("Size");
            grid.Row("folder", new DataGridCell(() => "42 files", "computing"));
            string html = Page(grid);

            Assert.Contains("\"t\":\"42 files\"", html);
            Assert.DoesNotContain("computing", html);
        }

        [Fact]
        public void TextSortsOrdinallyIgnoringCaseLikeTheScreenGrid() {
            Grid grid = new Grid("Names");
            grid.Column("Name");
            foreach (string name in new[] { "a2", "B1", "a10" }) { grid.Row(name, name); }
            string html = Page(grid);

            // Ordinal order is a10, a2, B1: the rank of each row in its original position.
            Assert.Contains("[{\"t\":\"a2\",\"k\":1}],[{\"t\":\"B1\",\"k\":2}],[{\"t\":\"a10\",\"k\":0}]", html);
        }

        [Theory]
        [InlineData(InteractionMode.None, false, false)]
        [InlineData(InteractionMode.Snapshot, false, false)]
        [InlineData(null, true, false)]
        [InlineData(null, false, true)]
        public void NoBrowserOpensWhenNobodyCanAnswer(InteractionMode? mode, bool nonInteractive, bool ci) {
            bool opened = false;
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => opened = true };

            Assert.Throws<PromptUnavailableException>(() => Servers().ShowInBrowser(Interactive(mode, nonInteractive, ci), browser));
            Assert.False(opened);
        }

        [Fact]
        public void NoBrowserIsReportedSoTheCallerCanFallBack() {
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => false };

            Assert.Throws<PromptUnavailableException>(() => Servers().ShowInBrowser(Interactive(), browser));
        }

        [Fact]
        public void AnUnansweredPageTimesOut() {
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => true, Timeout = TimeSpan.FromMilliseconds(300) };

            Assert.Throws<TimeoutException>(() => Servers().ShowInBrowser(Interactive(), browser));
        }

        [Fact]
        public void CancellingTheCallerStopsWaiting() {
            using CancellationTokenSource stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => true };

            Assert.ThrowsAny<OperationCanceledException>(() => Servers().ShowInBrowser(Interactive(), browser, stop.Token));
        }

        [Fact]
        public void ASelectionOfEveryRowInALargeGridIsAccepted() {
            Grid grid = new Grid("Many");
            grid.Column("Index");
            for (int i = 0; i < 13000; i++) { grid.Row(i, i); }
            string all = "ok:" + string.Join(",", Enumerable.Range(0, 13000));
            GridResult result = grid.ShowInBrowser(Interactive(), Browser((client, address) => Answer(client, address, all)));

            Assert.True(result.Accepted);
            Assert.Equal(13000, result.Items.Count);
        }

        [Fact]
        public void TheResolvedProfileCultureFormatsTheCells() {
            Grid grid = new Grid("Amounts");
            grid.Column("Amount");
            grid.Row("total", 1234.5);
            GridOptions options = Interactive();
            options.Interactive!.Profile = options.Interactive.Profile!.WithCulture(CultureInfo.GetCultureInfo("de-DE"));
            string html = string.Empty;
            grid.ShowInBrowser(options, Browser((client, address) => Answer(client, address, "cancel"), page => html = page));

            Assert.Contains("1234,5", html.Replace(".", string.Empty));
            Assert.DoesNotContain("1234.5", html);
        }

        [Fact]
        public void ASlowlyTrickledAnswerStillHonoursCancellation() {
            using CancellationTokenSource stop = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            BrowserGridOptions browser = new BrowserGridOptions {
                Open = address => {
                    _ = Task.Run(() => {
                        // Sends one byte every 100 ms, so no single read ever stalls.
                        using System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(address.Host, address.Port);
                        byte[] head = Encoding.ASCII.GetBytes("POST " + address.AbsolutePath + "answer HTTP/1.1\r\nHost: " + address.Authority + "\r\nContent-Length: 1000\r\n\r\n");
                        System.Net.Sockets.NetworkStream stream = client.GetStream();
                        stream.Write(head, 0, head.Length);
                        try {
                            for (int i = 0; i < 100; i++) { stream.WriteByte((byte)'0'); stream.Flush(); Thread.Sleep(100); }
                        } catch (System.IO.IOException) {
                        }
                    });
                    return true;
                }
            };

            DateTime started = DateTime.UtcNow;
            Assert.ThrowsAny<OperationCanceledException>(() => Servers().ShowInBrowser(Interactive(), browser, stop.Token));
            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(6), "cancellation waited for the trickling body");
        }

        [Fact]
        public void ADroppedAnswerKeepsTheGridWaitingForTheNextOne() {
            Grid grid = Servers();
            GridResult result = grid.ShowInBrowser(Interactive(), Browser(async (client, address) => {
                using (System.Net.Sockets.TcpClient dropped = new System.Net.Sockets.TcpClient(address.Host, address.Port)) {
                    byte[] head = Encoding.ASCII.GetBytes("POST " + address.AbsolutePath + "answer HTTP/1.1\r\nHost: " + address.Authority + "\r\nContent-Length: 100\r\n\r\nok:");
                    dropped.GetStream().Write(head, 0, head.Length);
                    await Task.Delay(300).ConfigureAwait(false);
                }
                await Answer(client, address, "ok:1").ConfigureAwait(false);
            }));

            Assert.True(result.Accepted);
            Assert.Same(grid.Rows[1].Item, result.Items.Single());
        }

        [Fact]
        public void ATruncatedAnswerDoesNotBlockCancellation() {
            using CancellationTokenSource stop = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            BrowserGridOptions browser = new BrowserGridOptions {
                Open = address => {
                    _ = Task.Run(() => {
                        // Declares a longer body than it sends, then keeps the connection open.
                        using System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(address.Host, address.Port);
                        byte[] head = Encoding.ASCII.GetBytes("POST " + address.AbsolutePath + "answer HTTP/1.1\r\nHost: " + address.Authority + "\r\nContent-Length: 100\r\n\r\nok:");
                        client.GetStream().Write(head, 0, head.Length);
                        Thread.Sleep(5000);
                    });
                    return true;
                }
            };

            Assert.ThrowsAny<OperationCanceledException>(() => Servers().ShowInBrowser(Interactive(), browser, stop.Token));
        }
    }
}
