using System;
using System.Linq;
using System.Threading;
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

// A queue fence reads real widgets on the applying thread while all four runspaces are still running.
// It observes public session behavior; it does not replace the PowerShell stream bridge or the widgets.
public sealed class ParallelStreamProbe : ILiveTarget {
    private static readonly object Gate = new object();
    private static readonly ManualResetEventSlim Release = new ManualResetEventSlim(false);
    private static LiveSession Session;
    private static int Observed;
    public static int MaxActivities;
    public static string ActivityFrame;
    public static LogRegion Log;
    public static ActivityList Activities;
    public string Name { get { return "parallel-test-fence"; } }

    public static void Attach() {
        lock (Gate) {
            if (Session != null) { return; }
            Session = LiveSession.Current;
            if (Session == null) { throw new InvalidOperationException("No live session."); }
            Find(Session.Root);
            if (Log == null || Activities == null) { throw new InvalidOperationException("Missing live log or activity list."); }
            Session.Register(new ParallelStreamProbe());
        }
    }

    private static void Find(IWidget widget) {
        if (widget is LogRegion) { Log = (LogRegion)widget; }
        if (widget is ActivityList) { Activities = (ActivityList)widget; }
        IContainer container = widget as IContainer;
        if (container != null) {
            foreach (IWidget child in container.Children) { Find(child); }
        }
    }

    public static void Observe() {
        Session.Post(new LiveEvent("parallel-test-fence", "observe"));
        if (!Release.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("The render thread did not observe all workers."); }
    }

    public void Apply(LiveEvent value) {
        if (Activities.Count > MaxActivities) {
            MaxActivities = Activities.Count;
            ActivityFrame = Terminal.RenderText(Activities, TerminalProfile.Fixed(120, 40, ColorDepth.None));
        }
        if (++Observed == 4) { Release.Set(); }
    }

    public string Describe(LiveEvent value) { return null; }

    public static string[] LogLines() { return Log.Entries.Select(entry => entry.Text).ToArray(); }
    public static long Dropped { get { return Session.EventsDropped; } }
    public static long Errors { get { return Session.ApplyErrors + Session.RenderErrors; } }
}
