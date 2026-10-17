using System;
using System.Diagnostics;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Benchmarks {
    /// <summary>
    /// Measures the renderer's own cost for a stress-sized dashboard (32 lanes, statistics, sparkline, log) at 140x45:
    /// event application throughput, frame composition, diff + VT encoding, bytes per frame and allocations per frame.
    /// The live session does this work on one thread about 15 times a second, so per-frame cost x 15 is its CPU budget.
    /// </summary>
    public static class Program {
        /// <summary>Runs the benchmarks.</summary>
        /// <param name="args">Optional: number of frames (default 600).</param>
        /// <returns>Exit code.</returns>
        public static int Main(string[] args) {
            int frames = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 600;
            Lanes lanes = new Lanes("workers");
            for (int i = 0; i < 32; i++) {
                lanes.Add("w" + i, "worker " + (i + 1).ToString("00", CultureInfo.InvariantCulture), 100000);
            }
            KeyValueList stats = new KeyValueList("stats").Add("Updates", "0").Add("Rate", "0").Add("Frames", "0");
            Sparkline rate = new Sparkline("rate") { Height = 3 };
            LogRegion log = new LogRegion();
            Dashboard dashboard = new Dashboard("Benchmark", new Rows()
                .Add(new Columns().Add(new Panel(lanes, "Workers"), LayoutSize.Fraction(2.2)).Add(new Panel(new Rows(stats, rate), "Session")))
                .Add(new Panel(log, "Log"), LayoutSize.Fraction()));
            TerminalProfile profile = TerminalProfile.Fixed(140, 45, ColorDepth.TrueColor, GlyphTier.Full);
            RenderContext context = new RenderContext(profile, Theme.Dark);
            Random random = new Random(7);

            // 1. Event application (what the render thread does per queued update).
            LiveEvent[] events = new LiveEvent[1000000];
            for (int i = 0; i < events.Length; i++) {
                events[i] = new LiveEvent("workers", "advance", "w" + (i % 32), i % 97 == 0 ? "item " + i : null, i % 30 == 0 ? "fail" : "pass", 1, 0, i);
            }
            Stopwatch watch = Stopwatch.StartNew();
            foreach (LiveEvent e in events) {
                lanes.Apply(e);
            }
            watch.Stop();
            double applyPerSecond = events.Length / watch.Elapsed.TotalSeconds;

            // 2. Frames: compose + diff + VT encode with a realistic amount of change per frame.
            StringTerminalWriter writer = new StringTerminalWriter();
            VtOutput output = new VtOutput(writer, profile, false);
            output.BeginLive();
            CellBuffer frame = new CellBuffer(140, 45);
            double composeMs = 0;
            double encodeMs = 0;
            long bytes = 0;
            long allocated = 0;
            for (int f = 0; f < frames; f++) {
                for (int i = 0; i < 5000; i++) {
                    lanes.Apply(events[(f * 5000 + i) % events.Length]);
                }
                rate.Add(80000 + random.Next(5000));
                stats.Add("Frames", f.ToString(CultureInfo.InvariantCulture));
                if (f % 3 == 0) {
                    log.Write("FakeLib: cache miss " + random.Next(9999), "out", TimeSpan.FromMilliseconds(f * 66));
                }
                long before = GC.GetAllocatedBytesForCurrentThread();
                Stopwatch compose = Stopwatch.StartNew();
                FrameComposer.Compose(dashboard, 140, 45, context.WithClock(f, TimeSpan.FromMilliseconds(f * 66)), frame);
                compose.Stop();
                writer.Clear();
                Stopwatch encode = Stopwatch.StartNew();
                output.RenderFrame(frame);
                encode.Stop();
                allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (f >= 10) {
                    composeMs += compose.Elapsed.TotalMilliseconds;
                    encodeMs += encode.Elapsed.TotalMilliseconds;
                    bytes += output.LastFrameLength;
                }
            }
            int measured = frames - 10;
            double perFrame = (composeMs + encodeMs) / measured;
            Console.WriteLine("ConsoleMonster benchmark (" + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + ", 140x45, 32 lanes)");
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  event apply:        {0:N0} events/s on one thread", applyPerSecond));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  compose per frame:  {0:0.000} ms", composeMs / measured));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  diff+VT per frame:  {0:0.000} ms", encodeMs / measured));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  chars per frame:    {0:N0} (diffed, 5,000 updates between frames)", bytes / (double)measured));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  alloc per frame:    {0:N0} bytes", allocated / (double)frames));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  render CPU at 15 fps: {0:0.0}% of one core (+ apply cost {1:0.0}% at 80k updates/s)", perFrame * 15 / 10.0, 80000 / applyPerSecond * 100));
            return 0;
        }
    }
}
