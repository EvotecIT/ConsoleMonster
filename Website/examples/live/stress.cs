using System.Diagnostics;
using System.Globalization;
using ConsoleMonster.Building;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// A stress test: 32 workers post thousands of updates a second while a noisy "library" writes to Console.Out and
// Console.Error from four threads. Workers never touch the console; the session draws about 15 frames a second and
// captures the stray lines into the output panel. Run it for N seconds: dotnet run -- 12
int seconds = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 12;
const int Workers = 32;

Lanes lanes = new Lanes("workers") { MaxLabelWidth = 10, ShowDetail = true };
for (int i = 0; i < Workers; i++) {
    lanes.Add("w" + i, "worker " + (i + 1).ToString("00", CultureInfo.InvariantCulture), 100000);
}
KeyValueList stats = new KeyValueList("stats")
    .Add("Updates posted", "0")
    .Add("Updates applied", "0")
    .Add("Update rate", "-")
    .Add("Frames", "0")
    .Add("Frame rate", "-")
    .Add("CPU", "-")
    .Add("CPU share", "-")
    .Add("Captured lines", "0")
    .Add("Dropped / errors", "0 / 0");
Sparkline rate = new Sparkline("rate").Height(3).Unit("/s");
LineChart cpu = new LineChart("cpu").Height(6).Minimum(0).Unit("%");
cpu.AddSeries("cpu", ThemeToken.Accent2);

Dashboard dashboard = new DashboardBuilder("[bold]Stress test[/]")
    .Subtitle(Workers + " workers · random Console.WriteLine · " + seconds + "s")
    .Columns(c => c
        .Panel("Workers", lanes, 2.2)
        .Panel("Session", new Rows(stats, new Rule("updates/s"), rate, new Rule("CPU % (100% = one core)"), cpu), 1))
    .Panel("Captured output", new LogRegion { PreferredHeight = 6 }, fill: true)
    .BuildDashboard();

string result = string.Empty;
using (LiveSession session = new LiveSession(dashboard)) {
    session.Run(s => result = Drive(s));
}
Console.WriteLine(result);

// <more>
string Drive(LiveSession session) {
    LanesHandle lanesHandle = lanes.Handle(session);
    KeyValueHandle statsHandle = stats.Handle(session);
    SparklineHandle rateHandle = rate.Handle(session);
    SparklineHandle cpuHandle = cpu.Handle(session);
    long captured = 0;
    using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(session.Cancellation);
    Thread[] workers = new Thread[Workers];
    for (int i = 0; i < Workers; i++) {
        int id = i;
        workers[i] = new Thread(() => Worker(id, lanesHandle, stop.Token)) { IsBackground = true };
        workers[i].Start();
    }
    Thread[] noisy = new Thread[4];
    for (int i = 0; i < noisy.Length; i++) {
        int id = i;
        noisy[i] = new Thread(() => FakeLibrary(id, stop.Token, ref captured)) { IsBackground = true };
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
        rateHandle.Add(perSecond);
        cpuHandle.Add(core, "cpu");
        statsHandle.Set("Updates posted", NumberFormat.Grouped(posted));
        statsHandle.Set("Updates applied", NumberFormat.Grouped(session.EventsApplied));
        statsHandle.Set("Update rate", NumberFormat.Grouped(perSecond) + "/s");
        statsHandle.Set("Frames", NumberFormat.Grouped(session.FramesRendered));
        statsHandle.Set("Frame rate", (session.FramesRendered / now).ToString("0.0", CultureInfo.InvariantCulture) + " fps");
        statsHandle.Set("CPU", core.ToString("0", CultureInfo.InvariantCulture) + "% (" + (core / 100).ToString("0.0", CultureInfo.InvariantCulture) + " cores)");
        statsHandle.Set("CPU share", (core / Environment.ProcessorCount).ToString("0.0", CultureInfo.InvariantCulture) + "% of " + Environment.ProcessorCount + " cores");
        statsHandle.Set("Captured lines", NumberFormat.Grouped(Interlocked.Read(ref captured)));
        statsHandle.Set("Dropped / errors", session.EventsDropped + " / " + session.ApplyErrors);
    }
    stop.Cancel();
    foreach (Thread t in workers) {
        t.Join();
    }
    foreach (Thread t in noisy) {
        t.Join();
    }
    for (int i = 0; i < Workers; i++) {
        lanesHandle.Complete("w" + i, StatusKind.Pass);
    }
    Thread.Sleep(300);
    process.Refresh();
    double elapsed = wall.Elapsed.TotalSeconds;
    double avgCore = (process.TotalProcessorTime - cpuStart).TotalMilliseconds / (elapsed * 1000) * 100;
    return string.Format(CultureInfo.InvariantCulture,
        "STRESS seconds={0:0.0} workers={1} posted={2} avgRate={3:0}/s peakRate={4:0}/s frames={5} fps={6:0.0} cpuOneCore={7:0.0}% cpuMachine={8:0.0}% captured={9} dropped={10} errors={11} runtime={12}",
        elapsed, Workers, session.EventsPosted, session.EventsPosted / elapsed, peakRate, session.FramesRendered, session.FramesRendered / elapsed,
        avgCore, avgCore / Environment.ProcessorCount, Interlocked.Read(ref captured), session.EventsDropped, session.ApplyErrors,
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
}

void Worker(int id, LanesHandle handle, CancellationToken token) {
    string key = "w" + id;
    Random random = new Random(id * 7919);
    string[] steps = { "parsing", "resolving", "evaluating", "scoring", "writing" };
    handle.Start(key, "starting");
    int n = 0;
    while (!token.IsCancellationRequested) {
        for (int burst = 0; burst < 40; burst++) {
            double roll = random.NextDouble();
            StatusKind outcome = roll < 0.03 ? StatusKind.Fail : roll < 0.1 ? StatusKind.Warn : StatusKind.Pass;
            handle.Advance(key, outcome, (n++ % 97 == 0) ? steps[random.Next(steps.Length)] + " item " + n : null);
        }
        Thread.Sleep(1);
    }
}

void FakeLibrary(int id, CancellationToken token, ref long captured) {
    Random random = new Random(1000 + id);
    string[] messages = {
        "FakeLib: cache miss for key {0}", "FakeLib: retrying request {0} (attempt 2)", "FakeLib: connection pool at {0}% capacity",
        "FakeLib: GC pause {0} ms", "FakeLib: flushed {0} records"
    };
    while (!token.IsCancellationRequested) {
        Thread.Sleep(random.Next(40, 400));
        string text = string.Format(CultureInfo.InvariantCulture, messages[random.Next(messages.Length)], random.Next(1, 999));
        if (random.NextDouble() < 0.15) {
            Console.Error.WriteLine("ERROR " + text);
        } else {
            Console.WriteLine(text);
        }
        Interlocked.Increment(ref captured);
    }
}
// </more>