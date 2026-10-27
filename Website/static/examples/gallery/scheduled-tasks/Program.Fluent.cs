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
    new("Nightly SQL backup", "Daily 02:00", "Backup-Sql.ps1", "exit 1", @"Target share \\nas-01\sql is full", "Free space or move the target, then run the task once by hand."),
    new("Certificate renewal", "Mon 03:00", "Renew-Certificates.ps1", "access denied", "svc-certs lost Log on as a batch job", "Restore the right in the server baseline policy.")
};

// Layout: the task table first, then one trigger → task → action trace per failure, side by side.
var table = Ui.Table(tasks)
    .Column(row => row.Task)
    .Column(row => row.Trigger)
    .Column(row => row.LastRun)
    .Column(row => row.Result)
    .Column(row => row.NextRun)
    .Column(row => row.Status, column => column.Presenter(CellPresenters.StatusWord))
    .Expand();

// Behaviour: print once; fix the cause, then start the task by hand and rerun this check.
Ui.Rows().Gap(1).Content(c =>
{
    c.Rule("[bold]Scheduled tasks[/] [muted]· app-01.adatum.example.test · 2026-10-26 09:05[/]");
    c.Add(table);
    c.Columns(columns => columns.Content(row =>
    {
        foreach (var failure in failures)
        {
            row.Panel(panel => panel.Title($"{failure.Task} · {failure.Outcome}").Status(StatusKind.Fail).Content(trace =>
            {
                trace.Rows(rows => rows.Gap(1).Content(steps =>
                {
                    steps.Flow(flow => flow
                        .Add(new FlowNode("trigger", failure.Trigger, StatusKind.Pass))
                        .Add(new FlowNode("task", failure.Task, StatusKind.Pass))
                        .Add(new FlowNode("action", failure.Action, StatusKind.Fail))
                        .Add(new FlowEdge("fires", "trigger", "task", "fired"))
                        .Add(new FlowEdge("runs", "task", "action", failure.Outcome, StatusKind.Fail, true)));
                    steps.KeyValue(details => details.KeyRole(ThemeToken.Fail)
                        .Add("Cause", failure.Cause)
                        .Add("Fix", failure.Fix));
                }));
            }));
        }
    }));
}).Show();

record TaskRow(string Task, string Trigger, string LastRun, string Result, string NextRun, string Status);
record Failure(string Task, string Trigger, string Action, string Outcome, string Cause, string Fix);
