using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

// Data: the same fictional snapshots used by the PowerShell twin.
Service[] services = {
    new("Spooler", "Running", "Automatic", "Print Spooler", 1234, "LocalSystem", "RPC", "Queues print jobs for lab printers."),
    new("WinRM", "Running", "Automatic", "Windows Remote Management", 2456, "NetworkService", "RPC", "Receives remote administration requests."),
    new("WSearch", "Stopped", "Manual", "Windows Search", 0, "LocalSystem", "RPC", "Indexes lab documents for search."),
    new("BITS", "Running", "Automatic", "Background Transfer", 3100, "LocalSystem", "RPC", "Transfers deployment packages."),
    new("Dnscache", "Running", "Automatic", "DNS Client", 3220, "NetworkService", "NSI", "Caches DNS lookups."),
    new("W32Time", "Running", "Automatic", "Windows Time", 3400, "LocalService", "RPC", "Keeps lab clocks synchronized."),
    new("EventLog", "Running", "Automatic", "Windows Event Log", 3500, "LocalService", "RPC", "Collects service and security events."),
    new("Schedule", "Running", "Automatic", "Task Scheduler", 3600, "LocalSystem", "RPC", "Runs scheduled maintenance tasks."),
    new("RemoteRegistry", "Stopped", "Manual", "Remote Registry", 0, "LocalService", "RPC", "Allows approved remote configuration."),
    new("BackupAgent", "Running", "Automatic", "Lab Backup Agent", 4120, "LabBackup", "RPC", "Verifies nightly backup snapshots."),
};
var grid = new Grid("Service browser");
grid.AddColumn("Name").AddColumn("Status").AddColumn("StartType").AddColumn("DisplayName")
    .AddColumn("ProcessId").AddColumn("Account").AddColumn("DependsOn").AddColumn("Description");
foreach (var service in services) {
    grid.AddRow(service, service.Name, service.Status, service.StartType, service.DisplayName,
        service.ProcessId, service.Account, service.DependsOn, service.Description);
}
// Behaviour: asynchronous commands receive captured snapshots and cancellation.
var options = new GridOptions();
foreach (string field in new[]
{
    "DisplayName",
    "ProcessId",
    "Account",
    "DependsOn",
    "Description"
}

)
{
    options.HiddenColumns.Add(field);
}

foreach (var definition in new CommandDefinition[]
{
    new("Restart", System.ConsoleKey.F5),
    new("Stop", System.ConsoleKey.F7),
    new("Start", System.ConsoleKey.F9)
}

)
{
    string operation = definition.Label;
    options.Command.Add(new AppCommand(operation, operation, async (context, cancellation) =>
    {
        foreach (object? row in context.Rows)
        {
            string name = row switch {
                Service service => service.Name,
                ServiceRefresh refreshed => refreshed.Name,
                _ => string.Empty
            };
            if (operation == "Restart")
            {
                context.Write($"Stopping {name} (Lab)");
                await Task.Delay(1000, cancellation);
                context.Write($"Starting {name} (Lab)");
            }
            else
            {
                context.Write($"{operation} {name} (Lab)");
            }

            await Task.Delay(1000, cancellation);
            context.Write($"Verifying {name}");
            await Task.Delay(700, cancellation);
        }
    }) { Shortcut = new KeyChord(definition.Shortcut), ConfirmPrompt = operation + " {Selection}?" });
}

// Refresh the acted-on snapshot on the input thread, preserving selection and row position.
options.RefreshRow = (command, row) =>
{
    var values = row.Values.ToArray();
    string status = command.Key == "Stop" ? "Stopped" : "Running";
    values[1] = status;
    values[4] = command.Key == "Stop" ? 0 : Convert.ToInt32(values[4]) + 100;
    var item = new ServiceRefresh(values[0]?.ToString() ?? string.Empty, status);
    return new GridRow(item, values);
};
// Layout: the shared host owns details, confirmation, output and terminal lifetime.
grid.Show(options);
record Service(string Name, string Status, string StartType, string DisplayName, int ProcessId, string Account, string DependsOn, string Description);
record ServiceRefresh(string Name, string Status);

record CommandDefinition(string Label, ConsoleKey Shortcut);
