using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The browser grid: a loopback page behind a one-time address that returns the chosen original objects.</summary>
    public sealed class BrowserGridTests {
        private sealed class Server { public Server(string name, int cpu) { Name = name; Cpu = cpu; } public string Name { get; } public int Cpu { get; } }

        private static Grid Servers(GridSelection selection = GridSelection.Multiple) {
            Grid grid = new Grid("Servers") { OutputMode = selection, Presentation = new PresentationOptions { Culture = CultureInfo.InvariantCulture } };
            grid.AddColumn("Name");
            grid.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grid.AddColumn("CPU");
            Server[] servers = { new Server("web01", 12), new Server("sql01", 85), new Server("dc01", 3) };
            foreach (Server server in servers) {
                grid.AddRow(server, server.Name, server.Cpu > 80 ? "Fail" : "Pass", server.Cpu);
            }
            return grid;
        }

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

        [Fact]
        public void OkReturnsTheChosenOriginalObjectsInTheOrderTheyWerePicked() {
            Grid grid = Servers();
            GridResult result = grid.ShowInBrowser(null, Browser((client, address) => Answer(client, address, "ok:1,0")));

            Assert.True(result.Accepted);
            Assert.Equal(new object?[] { grid.Rows[1].Item, grid.Rows[0].Item }, result.Items.ToArray());
        }

        [Fact]
        public void CancelReturnsNothing() {
            GridResult result = Servers().ShowInBrowser(null, Browser((client, address) => Answer(client, address, "cancel")));

            Assert.False(result.Accepted);
            Assert.Empty(result.Items);
        }

        [Theory]
        [InlineData(GridSelection.Single, 1)]
        [InlineData(GridSelection.None, 0)]
        public void OutputModeLimitsWhatReturns(GridSelection selection, int expected) {
            GridResult result = Servers(selection).ShowInBrowser(null, Browser((client, address) => Answer(client, address, "ok:2,1")));

            Assert.True(result.Accepted);
            Assert.Equal(expected, result.Items.Count);
        }

        [Fact]
        public void OnlyTheExactAddressIsServed() {
            HttpStatusCode otherPath = 0, otherHost = 0;
            Grid grid = Servers();
            GridResult result = grid.ShowInBrowser(null, Browser(async (client, address) => {
                otherPath = (await client.GetAsync(new Uri(address, "/not-the-token/")).ConfigureAwait(false)).StatusCode;
                using HttpRequestMessage renamed = new HttpRequestMessage(HttpMethod.Post, new Uri(address, "answer")) { Content = new StringContent("ok:0") };
                renamed.Headers.Host = "attacker.example";
                otherHost = (await client.SendAsync(renamed).ConfigureAwait(false)).StatusCode;
                await Answer(client, address, "ok:2").ConfigureAwait(false);
            }));

            Assert.Equal(HttpStatusCode.NotFound, otherPath);
            Assert.Equal(HttpStatusCode.Forbidden, otherHost);
            Assert.Same(grid.Rows[2].Item, result.Items.Single());
        }

        [Fact]
        public void ThePageCarriesPresentedValuesAndCannotBeEscapedByRowText() {
            Grid grid = new Grid("Report") { Presentation = new PresentationOptions { Culture = CultureInfo.InvariantCulture } };
            grid.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grid.AddColumn("Note");
            grid.AddRow("item", "Fail", "</script><script>alert(1)</script>");
            string html = string.Empty;
            grid.ShowInBrowser(null, Browser((client, address) => Answer(client, address, "cancel"), page => html = page));

            Assert.Contains("\"r\":\"fail\"", html);
            Assert.DoesNotContain("</script><script>alert(1)", html);
            Assert.Contains("\\u003c/script\\u003e", html);
        }

        [Fact]
        public void NumberColumnsAreRightAlignedAndTextColumnsAreNot() {
            string html = string.Empty;
            Servers().ShowInBrowser(null, Browser((client, address) => Answer(client, address, "cancel"), page => html = page));

            Assert.Contains("{\"name\":\"CPU\",\"hidden\":false,\"align\":\"right\"}", html);
            Assert.Contains("{\"name\":\"Name\",\"hidden\":false,\"align\":\"left\"}", html);
        }

        [Fact]
        public void NoBrowserIsReportedSoTheCallerCanFallBack() {
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => false };

            Assert.Throws<PromptUnavailableException>(() => Servers().ShowInBrowser(null, browser));
        }

        [Fact]
        public void AnUnansweredPageTimesOut() {
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => true, Timeout = TimeSpan.FromMilliseconds(300) };

            Assert.Throws<TimeoutException>(() => Servers().ShowInBrowser(null, browser));
        }

        [Fact]
        public void CancellingTheCallerStopsWaiting() {
            using CancellationTokenSource stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            BrowserGridOptions browser = new BrowserGridOptions { Open = _ => true };

            Assert.ThrowsAny<OperationCanceledException>(() => Servers().ShowInBrowser(null, browser, stop.Token));
        }
    }
}
