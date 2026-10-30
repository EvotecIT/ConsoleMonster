// Check scheduled task health and trace each failure from its trigger to the action that broke.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional tasks on one server; Task Scheduler is not queried and nothing is run.
var tasks = new TaskRow[]
{
    new("Nightly SQL backup", "Daily 02:00", "10-26 02:00", "0x1", "10-27 02:00", "Fail"),
    new("Certificate renewal", "Mon 03:00", "10-26 03:00", "0x80070005", "11-02 03:00", "Fail"),
    new("Report export", "Daily 06:30", "10-26 06:30", "0x41301", "10-27 06:30", "Info"),
    new("Log cleanup", "Daily 01:00", "10-26 01:00", "0x0", "10-27 01:00", "Pass"),
    new("Inventory sync", "Hourly", "10-26 09:00", "0x0", "10-26 10:00", "Pass"),
    new("Defragment volumes", "Sun 04:00", "10-25 04:00", "0x0", "11-01 04:00", "Pass"),
    new("Legacy FTP upload", "Disabled", "09-30 23:00", "0x0", "-", "Skip")
};
var failures = new Failure[]
{
    new("Nightly SQL backup", "Daily 02:00", "Backup-Sql.ps1", "exit 1",
        @"Target share \\nas-01\sql is full",
        "Free space or move the target, then run the task once by hand."),
    new("Certificate renewal", "Mon 03:00", "Renew-Certificates.ps1", "access denied",
        "svc-certs lost Log on as a batch job", "Restore the right in the server baseline policy.")
};

// Layout: the task table first, then one trigger → task → action trace per failure, side by side.
var table = Table.For(tasks)
    .Column(row => row.Task)
    .Column(row => row.Trigger)
    .Column(row => row.LastRun)
    .Column(row => row.Result)
    .Column(row => row.NextRun)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);
table.Expand = true;

var traces = new Columns();
foreach (var failure in failures)
{
    var flow = new Flow();
    flow.Add(new FlowNode("trigger", failure.Trigger, StatusKind.Pass));
    flow.Add(new FlowNode("task", failure.Task, StatusKind.Pass));
    flow.Add(new FlowNode("action", failure.Action, StatusKind.Fail));
    flow.Add(new FlowEdge("fires", "trigger", "task", "fired"));
    flow.Add(new FlowEdge("runs", "task", "action", failure.Outcome, StatusKind.Fail, true));
    var details = new KeyValueList { KeyRole = ThemeToken.Fail }
        .Add("Cause", failure.Cause)
        .Add("Fix", failure.Fix);
    traces.Add(new Panel(
        new Rows(flow, details) { Gap = 1 },
        $"{failure.Task} · {failure.Outcome}") { Status = StatusKind.Fail });
}

var report = new Rows(
    new Rule("[bold]Scheduled tasks[/] [muted]· app-01.adatum.example.test · 2026-10-26 09:05[/]"),
    table,
    traces)
{
    Gap = 1
};

// Behaviour: print once; fix the cause, then start the task by hand and rerun this check.
Terminal.Write(report);

record TaskRow(string Task, string Trigger, string LastRun, string Result, string NextRun,
    string Status);

record Failure(string Task, string Trigger, string Action, string Outcome, string Cause,
    string Fix);
