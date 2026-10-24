// Stress test: thirty-two workers, fast handles, session telemetry and captured tool output.
using System.Diagnostics;
using System.Globalization;
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: seeded snapshot samples; live work is bounded and touches no external service or file.
const int Workers = 32;
const int Seconds = 6;
string[] actions = { "parsing", "resolving", "evaluating", "scoring", "writing" };
double[] sampleRates = Enumerable.Range(0, 48).Select(i => (double)(18000 + (i * 7919 % 11000))).ToArray();
double[] sampleCpu = Enumerable.Range(0, 48).Select(i => (double)(42 + (i * 17 % 74))).ToArray();
string[] messages = { "cache miss for key 412", "retrying request 28 (attempt 2)", "connection pool at 76% capacity", "GC pause 8 ms", "flushed 960 records" };
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], CultureInfo.InvariantCulture);
if (step < 0 || step > 6) throw new ArgumentOutOfRangeException(nameof(step));
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
string Number(double value) => value.ToString("#,0", CultureInfo.InvariantCulture);

// Layout: retain every worker row, the session counters, update sparkline, CPU chart and captured output.
Lanes lanes = new Lanes("workers");
for (int id = 0; id < Workers; id++) {
    Lane lane = lanes.Add("w" + id, "worker " + (id + 1).ToString("00", CultureInfo.InvariantCulture), 100000);
    if (snapshot && step > 0) {
        lane.Done = step == 6 ? 100000 : step * 3200 + id * 97 % 400;
        lane.Failed = (int)lane.Done / 97;
        lane.Warned = (int)lane.Done / 17;
        lane.Passed = (int)lane.Done - lane.Failed - lane.Warned;
        lane.Status = step == 6 ? StatusKind.Pass : StatusKind.Running;
        lane.Detail = actions[(id + step) % actions.Length] + " item " + Number(lane.Done);
    }
}
int samples = snapshot ? step * 8 : 0;
long posted = snapshot ? step * 128000L : 0;
KeyValueList stats = new KeyValueList("stats").Add("Updates posted", Number(posted))
    .Add("Updates applied", Number(posted)).Add("Update rate", samples == 0 ? "-" : Number(sampleRates[samples - 1]) + "/s")
    .Add("Frames", snapshot ? Number(step * 15) : "0").Add("Frame rate", step == 0 ? "-" : "15.0 fps")
    .Add("CPU", samples == 0 ? "-" : Number(sampleCpu[samples - 1]) + "%")
    .Add("CPU share", samples == 0 ? "-" : (sampleCpu[samples - 1] / 8).ToString("0.0", CultureInfo.InvariantCulture) + "% of 8 cores")
    .Add("Stray writes", (step * 4).ToString(CultureInfo.InvariantCulture)).Add("Dropped / errors", "0 / 0");
Sparkline rate = new Sparkline("rate") { Height = 3, Unit = "/s" };
rate.AddRange(sampleRates.Take(samples));
LineChart cpu = new LineChart("cpu") { Height = 6, Minimum = 0, Unit = "%" };
cpu.AddSeries("cpu", ThemeToken.Accent2);
foreach (double value in sampleCpu.Take(samples)) cpu.Add("cpu", value);
LogRegion output = new LogRegion { PreferredHeight = 6, ShowElapsed = false };
if (snapshot) for (int i = 0; i < step * 4; i++) output.Write("stray-" + i % 4 + "-" + i + ": FakeLib: " + messages[i % 5], i % 7 == 0 ? "err" : "info");
Rows body = new Rows(new Columns().Add(new Panel(lanes, "Workers"), LayoutSize.Fraction(2.2))
    .Add(new Panel(new Rows(stats, new Rule("updates/s"), rate, new Rule("CPU % (100% = one core)"), cpu), "Session")))
    .Add(new Panel(output, "Captured output"), LayoutSize.Fraction());
Dashboard dashboard = new Dashboard("[bold]Stress test[/]", body) {
    Subtitle = "32 workers · captured tool output · 6s", ShowElapsed = false,
    Footer = "[muted]32 workers    15 frames/s    Ctrl+C stop[/]"
};

// Behaviour: snapshots use fixed samples; live telemetry measures this run and fast handles queue worker updates.
if (snapshot) { Terminal.Write(dashboard); return; }
using LiveSession session = new LiveSession(dashboard);
session.Run(live => {
    var workerHandle = lanes.Handle(live);
    var statHandle = stats.Handle(live);
    var rateHandle = rate.Handle(live);
    var cpuHandle = cpu.Handle(live);
    using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(live.Cancellation);
    Thread[] workers = new Thread[Workers];
    long captured = 0;
    for (int id = 0; id < Workers; id++) {
        int worker = id;
        workers[id] = new Thread(() => {
            Random random = new Random(worker * 7919);
            string key = "w" + worker;
            workerHandle.Start(key, "starting");
            int done = 0;
            while (!stop.IsCancellationRequested) {
                for (int burst = 0; burst < 40; burst++) {
                    double roll = random.NextDouble();
                    workerHandle.Advance(key, roll < 0.03 ? StatusKind.Fail : roll < 0.1 ? StatusKind.Warn : StatusKind.Pass,
                        done % 97 == 0 ? actions[done % actions.Length] + " item " + done : null);
                    done++;
                }
                if (worker < 4 && done % 400 == 0) {
                    string line = "stray-" + worker + "-" + done + ": FakeLib: " + messages[done / 400 % messages.Length];
                    if (done % 2800 == 0) Console.Error.WriteLine(line); else Console.WriteLine(line);
                    Interlocked.Increment(ref captured);
                }
                if (stop.Token.WaitHandle.WaitOne(2)) break;
            }
        }) { IsBackground = true };
        workers[id].Start();
    }
    using Process process = Process.GetCurrentProcess();
    Stopwatch wall = Stopwatch.StartNew();
    TimeSpan lastCpu = process.TotalProcessorTime;
    double lastWall = 0;
    long lastPosted = 0;
    try {
        while (wall.Elapsed.TotalSeconds < Seconds && !live.Cancellation.IsCancellationRequested) {
            if (live.Cancellation.WaitHandle.WaitOne(250)) break;
            process.Refresh();
            double now = wall.Elapsed.TotalSeconds;
            double interval = now - lastWall;
            double core = (process.TotalProcessorTime - lastCpu).TotalSeconds / interval * 100;
            long count = live.EventsPosted;
            double perSecond = (count - lastPosted) / interval;
            rateHandle.Add(perSecond);
            cpuHandle.Add(core, "cpu");
            statHandle.Set("Updates posted", Number(count));
            statHandle.Set("Updates applied", Number(live.EventsApplied));
            statHandle.Set("Update rate", Number(perSecond) + "/s");
            statHandle.Set("Frames", Number(live.FramesRendered));
            statHandle.Set("Frame rate", (live.FramesRendered / now).ToString("0.0", CultureInfo.InvariantCulture) + " fps");
            statHandle.Set("CPU", Number(core) + "%");
            statHandle.Set("CPU share", (core / Environment.ProcessorCount).ToString("0.0", CultureInfo.InvariantCulture) + "% of " + Environment.ProcessorCount + " cores");
            statHandle.Set("Stray writes", Number(Interlocked.Read(ref captured)));
            statHandle.Set("Dropped / errors", live.EventsDropped + " / " + live.ApplyErrors);
            lastCpu = process.TotalProcessorTime;
            lastWall = now;
            lastPosted = count;
        }
    } finally {
        stop.Cancel();
        foreach (Thread worker in workers) worker.Join();
    }
    for (int id = 0; id < Workers; id++) workerHandle.Complete("w" + id, StatusKind.Pass);
});
if (session.EventsDropped != 0 || session.ApplyErrors != 0 || session.EventsPosted != session.EventsApplied)
    throw new InvalidOperationException("Accepted stress updates did not drain successfully.");
