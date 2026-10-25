using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

// Data: the same fictional snapshots used by the PowerShell twin.
var services = new Grid("Service browser");
services.AddColumn("Name").AddColumn("Status").AddColumn("StartType").AddColumn("DisplayName").AddColumn("ProcessId").AddColumn("Account").AddColumn("Dependencies").AddColumn("Description");
var spooler = new { Name = "Spooler", Status = "Running", StartType = "Automatic", DisplayName = "Print Spooler", ProcessId = 1234, Account = "LocalSystem", Dependencies = "RPC", Description = "Queues print jobs for lab printers." };
services.AddRow(spooler, spooler.Name, spooler.Status, spooler.StartType, spooler.DisplayName, spooler.ProcessId, spooler.Account, spooler.Dependencies, spooler.Description);
var winrm = new { Name = "WinRM", Status = "Running", StartType = "Automatic", DisplayName = "Windows Remote Management", ProcessId = 2456, Account = "NetworkService", Dependencies = "RPC", Description = "Receives remote administration requests." };
services.AddRow(winrm, winrm.Name, winrm.Status, winrm.StartType, winrm.DisplayName, winrm.ProcessId, winrm.Account, winrm.Dependencies, winrm.Description);
var wsearch = new { Name = "WSearch", Status = "Stopped", StartType = "Manual", DisplayName = "Windows Search", ProcessId = 0, Account = "LocalSystem", Dependencies = "RPC", Description = "Indexes lab documents for search." };
services.AddRow(wsearch, wsearch.Name, wsearch.Status, wsearch.StartType, wsearch.DisplayName, wsearch.ProcessId, wsearch.Account, wsearch.Dependencies, wsearch.Description);
var bits = new { Name = "BITS", Status = "Running", StartType = "Automatic", DisplayName = "Background Transfer", ProcessId = 3100, Account = "LocalSystem", Dependencies = "RPC", Description = "Transfers deployment packages." };
services.AddRow(bits, bits.Name, bits.Status, bits.StartType, bits.DisplayName, bits.ProcessId, bits.Account, bits.Dependencies, bits.Description);
var dnscache = new { Name = "Dnscache", Status = "Running", StartType = "Automatic", DisplayName = "DNS Client", ProcessId = 3220, Account = "NetworkService", Dependencies = "NSI", Description = "Caches DNS lookups." };
services.AddRow(dnscache, dnscache.Name, dnscache.Status, dnscache.StartType, dnscache.DisplayName, dnscache.ProcessId, dnscache.Account, dnscache.Dependencies, dnscache.Description);
var w32time = new { Name = "W32Time", Status = "Running", StartType = "Automatic", DisplayName = "Windows Time", ProcessId = 3400, Account = "LocalService", Dependencies = "RPC", Description = "Keeps lab clocks synchronized." };
services.AddRow(w32time, w32time.Name, w32time.Status, w32time.StartType, w32time.DisplayName, w32time.ProcessId, w32time.Account, w32time.Dependencies, w32time.Description);
var eventlog = new { Name = "EventLog", Status = "Running", StartType = "Automatic", DisplayName = "Windows Event Log", ProcessId = 3500, Account = "LocalService", Dependencies = "RPC", Description = "Collects service and security events." };
services.AddRow(eventlog, eventlog.Name, eventlog.Status, eventlog.StartType, eventlog.DisplayName, eventlog.ProcessId, eventlog.Account, eventlog.Dependencies, eventlog.Description);
var schedule = new { Name = "Schedule", Status = "Running", StartType = "Automatic", DisplayName = "Task Scheduler", ProcessId = 3600, Account = "LocalSystem", Dependencies = "RPC", Description = "Runs scheduled maintenance tasks." };
services.AddRow(schedule, schedule.Name, schedule.Status, schedule.StartType, schedule.DisplayName, schedule.ProcessId, schedule.Account, schedule.Dependencies, schedule.Description);
var remoteregistry = new { Name = "RemoteRegistry", Status = "Stopped", StartType = "Manual", DisplayName = "Remote Registry", ProcessId = 0, Account = "LocalService", Dependencies = "RPC", Description = "Allows approved remote configuration." };
services.AddRow(remoteregistry, remoteregistry.Name, remoteregistry.Status, remoteregistry.StartType, remoteregistry.DisplayName, remoteregistry.ProcessId, remoteregistry.Account, remoteregistry.Dependencies, remoteregistry.Description);
var backupagent = new { Name = "BackupAgent", Status = "Running", StartType = "Automatic", DisplayName = "Lab Backup Agent", ProcessId = 4120, Account = "LabBackup", Dependencies = "RPC", Description = "Verifies nightly backup snapshots." };
services.AddRow(backupagent, backupagent.Name, backupagent.Status, backupagent.StartType, backupagent.DisplayName, backupagent.ProcessId, backupagent.Account, backupagent.Dependencies, backupagent.Description);

// Behaviour: asynchronous commands receive captured snapshots and cancellation.
var options = new GridOptions();
foreach (string field in new[] { "DisplayName", "ProcessId", "Account", "Dependencies", "Description" }) { options.HiddenColumns.Add(field); }
foreach (var definition in new[] { ("Restart", System.ConsoleKey.F5), ("Stop", System.ConsoleKey.F7), ("Start", System.ConsoleKey.F9) }) {
    string operation = definition.Item1;
    options.Commands.Add(new AppCommand(operation, operation, async (context, cancellation) => {
        foreach (object? row in context.Rows) {
            var type = row!.GetType();
            string name = type.GetProperty("Name")!.GetValue(row)?.ToString() ?? "";
            context.Write($"{operation} {name} in Lab");
            await Task.Delay(200, cancellation);
            context.Write($"Verifying {name}: {type.GetProperty("Status")!.GetValue(row)}");
        }
    }) { Shortcut = new KeyChord(definition.Item2), ConfirmPrompt = operation + " {Selection}?" });
}

// Layout: the shared host owns details, confirmation, output and terminal lifetime.
services.Show(options);
