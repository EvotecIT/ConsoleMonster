using ConsoleMonster;
using ConsoleMonster.Widgets;

// Each source keeps one colour; the filter shows warnings and worse, and hidden lines stay in the history.
LogRegion log = new LogRegion()
{
    PreferredHeight = 5,
    ShowElapsed = false,
    Filter = new LogFilter(null, "warn")
};
log.Write(new LogEntry(TimeSpan.Zero, "info", "Snapshot of 12 mailboxes taken", null, "snapshot"));
log.Write(new LogEntry(TimeSpan.Zero, "warn", "Mailbox for frank is over quota (51 GB)", null,
    "migrate"));
log.Write(new LogEntry(TimeSpan.Zero, "info", "alice moved in 1.1s", null, "migrate"));
log.Write(new LogEntry(TimeSpan.Zero, "err", "frank: access denied on the target database", null,
    "migrate"));
log.Write(new LogEntry(TimeSpan.Zero, "warn", "Throttling: 3 requests delayed", null,
    "exchange-online"));
Terminal.Write(log);
