using ConsoleMonster.Input;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;

// Run beside synthetic.log. This application owns its reader and input lease.
using var source = new FileLogTail("synthetic.log", initialLineCount: 16);
var view = new LogTailView(capacity: 1000, highlight: "database");
using var session = new LiveSession(view, new LiveSessionOptions {
    CaptureConsole = false,
    PrintFinalState = false,
    TaskbarProgress = false
});
session.Start();
try {
    using var input = ConsoleInput.Acquire();
    while (!session.Cancellation.IsCancellationRequested) {
        foreach (var entry in source.ReadAvailable()) {
            view.Post(session, entry);
        }
        var key = input.Source.Read(TimeSpan.FromMilliseconds(250), session.Cancellation);
        if (key == null) { continue; }
        if (key.IsChar('q') || key.Is(ConsoleKey.Escape)) { break; }
        view.Navigate(session, key);
    }
} catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested) {
    // Ctrl+C cancels the session; finally restores the terminal.
} finally {
    session.Stop();
}
