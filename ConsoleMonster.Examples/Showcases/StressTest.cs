using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConsoleMonster.Building;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// The stress test: 32 workers post thousands of updates per second, a fake library writes to Console.Out and
    /// Console.Error at random from several threads, and the dashboard shows live statistics including CPU use.
    /// Workers never touch the console; the session coalesces everything into about 15 frames per second.
    /// </summary>
    public static class StressTest {
        private const int Workers = 32;

        /// <summary>Runs the stress test.</summary>
        /// <param name="seconds">How long to run.</param>
        /// <returns>A one-line result summary (also printed after the session).</returns>
        public static string Run(int seconds) {
            Dashboard dashboard = Build(seconds, out Lanes lanes, out KeyValueList stats, out Sparkline rate, out LineChart cpu, out LogRegion capturedOutput);
            ConcurrentQueue<string> expectedLines = new ConcurrentQueue<string>();

            string result = string.Empty;
            LiveSessionOptions options = new LiveSessionOptions();
            using (LiveSession session = new LiveSession(dashboard, options)) {
                session.Run(s => result = Drive(s, lanes, stats, rate, cpu, seconds, expectedLines));
                string[] expected = expectedLines.OrderBy(line => line, StringComparer.Ordinal).ToArray();
                string[] actual = capturedOutput.Entries.Select(entry => entry.Text).OrderBy(line => line, StringComparer.Ordinal).ToArray();
                if (session.EventsDropped != 0 || session.ApplyErrors != 0 || session.EventsPosted != session.EventsApplied) {
                    throw new InvalidOperationException("The stress run lost updates or failed to apply accepted work.");
                }
                if (!expected.SequenceEqual(actual) || capturedOutput.TotalLines != expected.Length) {
                    throw new InvalidOperationException("The stress run's captured log does not contain exactly the stray lines that were written.");
                }
                result += " verifiedCaptured=" + actual.Length.ToString(CultureInfo.InvariantCulture) + " missing=0 applied=" + session.EventsApplied.ToString(CultureInfo.InvariantCulture);
            }
            Console.WriteLine(result);
            return result;
        }

        /// <summary>Builds the stress dashboard without starting workers, for print-once previews and embedding.</summary>
        /// <param name="seconds">The advertised run duration.</param>
        /// <returns>The dashboard in its initial state.</returns>
        public static Dashboard Build(int seconds = 10) => Build(seconds, out _, out _, out _, out _, out _);

        private static Dashboard Build(int seconds, out Lanes lanes, out KeyValueList stats, out Sparkline rate, out LineChart cpu, out LogRegion capturedOutput) {
            lanes = new Lanes("workers") { MaxLabelWidth = 10, ShowDetail = true };
            for (int i = 0; i < Workers; i++) {
                lanes.Add("w" + i.ToString(CultureInfo.InvariantCulture), "worker " + (i + 1).ToString("00", CultureInfo.InvariantCulture), 100000);
            }
            stats = new KeyValueList("stats")
                .Add("Updates posted", "0")
                .Add("Updates applied", "0")
                .Add("Update rate", "-")
                .Add("Frames", "0")
                .Add("Frame rate", "-")
                .Add("CPU", "-")
                .Add("CPU share", "-")
                .Add("Stray writes", "0")
                .Add("Dropped / errors", "0 / 0");
            rate = new Sparkline("rate") { Height = 3, Unit = "/s" };
            cpu = new LineChart("cpu") { Height = 6, Min = 0, Unit = "%" };
            cpu.AddSeries("cpu", ThemeToken.Accent2);

            // Keep the finite stress run's complete log so delivery is verified by identity, not attempted-write counts.
            capturedOutput = new LogRegion { PreferredHeight = 6, Capacity = int.MaxValue };
            Lanes workers = lanes;
            Rows session = new Rows(stats, new Rule("updates/s"), rate, new Rule("CPU % (100% = one core)"), cpu);
            return new DashboardBuilder("[bold]Stress test[/]")
                .Subtitle(Workers + " workers · random Console.WriteLine · " + seconds + "s")
                .Columns(c => c
                    .Panel("Workers", workers, 1.6)
                    .Panel("Session", session, 1))
                .Panel("Captured output", capturedOutput, fill: true)
                .BuildDashboard();

        }

        private static string Drive(LiveSession session, Lanes lanesWidget, KeyValueList statsWidget, Sparkline rateWidget, LineChart cpuWidget, int seconds, ConcurrentQueue<string> expectedLines) {
            LanesHandle lanes = lanesWidget.Handle(session);
            KeyValueHandle stats = statsWidget.Handle(session);
            SparklineHandle rate = rateWidget.Handle(session);
            SparklineHandle cpu = cpuWidget.Handle(session);
            long captured = 0;
            using (CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(session.Cancellation)) {
                Thread[] workers = new Thread[Workers];
                for (int i = 0; i < Workers; i++) {
                    int id = i;
                    workers[i] = new Thread(() => Worker(id, lanes, stop.Token)) { IsBackground = true, Name = "worker " + id.ToString(CultureInfo.InvariantCulture) };
                    workers[i].Start();
                }
                Thread[] noisy = new Thread[4];
                for (int i = 0; i < noisy.Length; i++) {
                    int id = i;
                    noisy[i] = new Thread(() => FakeLibrary(id, stop.Token, ref captured, expectedLines)) { IsBackground = true };
                    noisy[i].Start();
                }

                Process process = Process.GetCurrentProcess();
                Stopwatch wall = Stopwatch.StartNew();
                TimeSpan cpuStart = process.TotalProcessorTime;
                TimeSpan lastCpu = cpuStart;
                double lastWall = 0;
                long lastPosted = 0;
                double peakRate = 0;
                while (wall.Elapsed.TotalSeconds < seconds && !session.Cancellation.IsCancellationRequested) {
                    Thread.Sleep(500);
                    process.Refresh();
                    double now = wall.Elapsed.TotalSeconds;
                    TimeSpan cpuNow = process.TotalProcessorTime;
                    double interval = now - lastWall;
                    double core = (cpuNow - lastCpu).TotalMilliseconds / (interval * 1000) * 100;
                    long posted = session.EventsPosted;
                    double perSecond = (posted - lastPosted) / interval;
                    peakRate = Math.Max(peakRate, perSecond);
                    lastCpu = cpuNow;
                    lastWall = now;
                    lastPosted = posted;
                    rate.Add(perSecond);
                    cpu.Add(core, "cpu");
                    stats.Set("Updates posted", NumberFormat.Grouped(posted));
                    stats.Set("Updates applied", NumberFormat.Grouped(session.EventsApplied));
                    stats.Set("Update rate", NumberFormat.Grouped(perSecond) + "/s");
                    stats.Set("Frames", NumberFormat.Grouped(session.FramesRendered));
                    stats.Set("Frame rate", (session.FramesRendered / now).ToString("0.0", CultureInfo.InvariantCulture) + " fps");
                    stats.Set("CPU", core.ToString("0", CultureInfo.InvariantCulture) + "% (" + (core / 100).ToString("0.0", CultureInfo.InvariantCulture) + " cores)");
                    stats.Set("CPU share", (core / Environment.ProcessorCount).ToString("0.0", CultureInfo.InvariantCulture) + "% of " + Environment.ProcessorCount + " cores");
                    stats.Set("Stray writes", NumberFormat.Grouped(Interlocked.Read(ref captured)));
                    stats.Set("Dropped / errors", session.EventsDropped + " / " + session.ApplyErrors);
                }
                stop.Cancel();
                foreach (Thread t in workers) {
                    t.Join();
                }
                foreach (Thread t in noisy) {
                    t.Join();
                }
                for (int i = 0; i < Workers; i++) {
                    lanes.Complete("w" + i.ToString(CultureInfo.InvariantCulture), StatusKind.Pass);
                }
                process.Refresh();
                double elapsed = wall.Elapsed.TotalSeconds;
                double avgCore = (process.TotalProcessorTime - cpuStart).TotalMilliseconds / (elapsed * 1000) * 100;
                return string.Format(CultureInfo.InvariantCulture,
                    "STRESS seconds={0:0.0} workers={1} posted={2} avgRate={3:0}/s peakRate={4:0}/s frames={5} fps={6:0.0} cpuOneCore={7:0.0}% cpuMachine={8:0.0}% captured={9} dropped={10} errors={11} runtime={12}",
                    elapsed, Workers, session.EventsPosted, session.EventsPosted / elapsed, peakRate, session.FramesRendered, session.FramesRendered / elapsed,
                    avgCore, avgCore / Environment.ProcessorCount, Interlocked.Read(ref captured), session.EventsDropped, session.ApplyErrors,
                    System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            }
        }

        private static void Worker(int id, LanesHandle lanes, CancellationToken token) {
            string key = "w" + id.ToString(CultureInfo.InvariantCulture);
            Random random = new Random(id * 7919);
            string[] steps = { "parsing", "resolving", "evaluating", "scoring", "writing" };
            lanes.Start(key, "starting");
            int n = 0;
            while (!token.IsCancellationRequested) {
                for (int burst = 0; burst < 40; burst++) {
                    double roll = random.NextDouble();
                    StatusKind result = roll < 0.03 ? StatusKind.Fail : roll < 0.1 ? StatusKind.Warn : StatusKind.Pass;
                    lanes.Advance(key, result, (n++ % 97 == 0) ? steps[random.Next(steps.Length)] + " item " + n.ToString(CultureInfo.InvariantCulture) : null);
                }
                Thread.Sleep(1);
            }
        }

        private static void FakeLibrary(int id, CancellationToken token, ref long captured, ConcurrentQueue<string> expectedLines) {
            Random random = new Random(1000 + id);
            string[] messages = {
                "FakeLib: cache miss for key {0}", "FakeLib: retrying request {0} (attempt 2)", "FakeLib: connection pool at {0}% capacity",
                "FakeLib: GC pause {0} ms", "FakeLib: flushed {0} records"
            };
            int sequence = 0;
            while (!token.IsCancellationRequested) {
                Thread.Sleep(random.Next(40, 400));
                string text = "stray-" + id.ToString(CultureInfo.InvariantCulture) + "-" + (sequence++).ToString(CultureInfo.InvariantCulture) + ": "
                    + string.Format(CultureInfo.InvariantCulture, messages[random.Next(messages.Length)], random.Next(1, 999));
                if (random.NextDouble() < 0.15) {
                    text = "ERROR " + text;
                    Console.Error.WriteLine(text);
                } else {
                    Console.WriteLine(text);
                }
                expectedLines.Enqueue(text);
                Interlocked.Increment(ref captured);
            }
        }
    }
}
