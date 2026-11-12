using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Forces fragment interleavings instead of relying on scheduler speed.</summary>
    public sealed class CaptureWriterConcurrencyTests {
        [Fact]
        public async Task ConcurrentFragments_StayWithTheirProducerUntilNewline() {
            ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
            using (CaptureWriter writer = new CaptureWriter(lines.Enqueue, TextWriter.Null))
            using (Barrier fragments = new Barrier(8)) {
                await Run(Enumerable.Range(0, 8).Select(id => (Action)(() => {
                    writer.Write("producer-" + id + ":");
                    if (!fragments.SignalAndWait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Missing producer fragment."); }
                    writer.WriteLine("complete");
                })).ToArray());
                writer.Complete();
                Assert.Equal(Enumerable.Range(0, 8).Select(id => "producer-" + id + ":complete").OrderBy(s => s), lines.OrderBy(s => s));
            }
        }

        // A producer that never ends its line (a progress bar redrawn with backspaces, binary output) is written in
        // bounded pieces; no text is lost and a surrogate pair is never split.
        [Fact]
        public void AnUnfinishedLine_IsWrittenInBoundedPiecesWithoutLosingText() {
            ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
            CaptureWriter writer = new CaptureWriter(lines.Enqueue, TextWriter.Null);
            string text = new string('a', CaptureWriter.MaxPartialLine - 1) + "\U0001F600" + new string('b', CaptureWriter.MaxPartialLine + 10);
            writer.Write(text);
            Assert.All(lines, line => Assert.InRange(line.Length, 1, CaptureWriter.MaxPartialLine + 1));
            Assert.Equal(2, lines.Count);
            writer.Complete();
            Assert.Equal(text, string.Concat(lines));
            Assert.All(lines, line => Assert.False(char.IsHighSurrogate(line[line.Length - 1])));
        }
        [Fact]
        public async Task Complete_FlushesEveryProducerTailExactlyOnce() {
            ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
            CaptureWriter writer = new CaptureWriter(lines.Enqueue, TextWriter.Null);
            await Run(Enumerable.Range(0, 8).Select(id => (Action)(() => writer.Write("tail-" + id))).ToArray());
            Assert.Empty(lines);
            writer.Complete();
            writer.Complete();
            Assert.Equal(Enumerable.Range(0, 8).Select(id => "tail-" + id).OrderBy(s => s), lines.OrderBy(s => s));
        }

        [Fact]
        public async Task CarriageReturnState_BelongsToTheProducer() {
            ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
            using (CaptureWriter writer = new CaptureWriter(lines.Enqueue, TextWriter.Null))
            using (ManualResetEventSlim first = new ManualResetEventSlim())
            using (ManualResetEventSlim second = new ManualResetEventSlim())
            using (ManualResetEventSlim completed = new ManualResetEventSlim()) {
                await Run(() => {
                    writer.Write("first\r");
                    first.Set();
                    if (!second.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Second producer did not write."); }
                    writer.Write('\n');
                    completed.Set();
                }, () => {
                    if (!first.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("First producer did not write."); }
                    writer.Write("second");
                    second.Set();
                    if (!completed.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("First producer did not finish CRLF."); }
                    writer.Write('\n');
                });
                writer.Complete();
                Assert.Equal(new[] { "first", "second" }, lines);
            }
        }

        private static async Task Run(params Action[] producers) {
            Task[] tasks = producers.Select(action => Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            Task all = Task.WhenAll(tasks);
            Assert.Same(all, await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(30))));
            await all;
        }
    }
}
