using System;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class HandleConcurrencyTests {
        [Fact]
        public async Task HandlesAndSlotSet_KeepEveryUpdateAndEachProducersLastValue() {
            const int producers = 8;
            const int updates = 64;
            LogRegion log = new LogRegion { Capacity = producers * updates };
            ProgressBar progress = new ProgressBar("Progress", "progress") { Total = producers * updates };
            KeyValueList values = new KeyValueList();
            SummaryBar summary = new SummaryBar { ShowDuration = false };
            Slot slot = new Slot("result");
            using (LiveSession session = new LiveSession(new Rows(log, progress, values, summary, slot), LiveSessionTests.Options(new FakeOutput()))) {
                var logging = log.Handle(session);
                var advancing = progress.Handle(session);
                var setting = values.Handle(session);
                var counting = summary.Handle(session);
                var swapping = session.Region("result");
                session.Start();
                Task all = Task.WhenAll(Enumerable.Range(0, producers).Select(producer => Task.Run(() => {
                    string key = "worker" + producer.ToString("D2");
                    for (int sequence = 0; sequence < updates; sequence++) {
                        logging.Write("log-" + producer + "-" + sequence);
                        advancing.Advance();
                        setting.Set(key, sequence.ToString());
                        counting.SetCounter(key, sequence, StatusKind.Pass);
                        swapping.Set(new TextBlock("slot-" + producer + "-" + sequence));
                    }
                })));
                Assert.Same(all, await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(30))));
                await all;
                counting.SetProgress(producers * updates, producers * updates);
                counting.SetProgress(done: producers * updates + 1);
                counting.SetProgress(total: producers * updates * 2);
                session.Stop();
                Assert.Equal(producers * updates * 5 + 3, session.EventsApplied);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(0, session.ApplyErrors);
                Assert.Equal(producers * updates, log.TotalLines);
                Assert.Equal(producers * updates, log.Entries.Count);
                Assert.Equal(Enumerable.Range(0, producers).SelectMany(p => Enumerable.Range(0, updates).Select(i => "log-" + p + "-" + i)).OrderBy(s => s), log.Entries.Select(e => e.Text).OrderBy(s => s));
                Assert.Equal(producers * updates, progress.Value);
                Assert.Equal(producers, values.Items.Count);
                Assert.All(values.Items, item => Assert.Equal("63", item.Value));
                Assert.Equal(producers * updates + 1, summary.Done);
                Assert.Equal(producers * updates * 2, summary.Total);
                string counters = summary.Line(1000, Terminal.CreateContext(TerminalProfile.Fixed(1000, 10, ColorDepth.None))).PlainText;
                foreach (int producer in Enumerable.Range(0, producers)) { Assert.Contains("63 worker" + producer.ToString("D2"), counters); }
                Assert.Matches("^slot-[0-7]-63$", Terminal.RenderText(slot, TerminalProfile.Fixed(80, 10, ColorDepth.None)).Trim());
            }
        }
    }
}
