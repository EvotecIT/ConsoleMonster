using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

MaintenanceTask[] all = {
    new("DiskCleanup", "Disk cleanup", "Maintenance", "Removes temporary files, old update caches and crash dumps older than 30 days.", 3, true),
    // <more>
    new("LogRotation", "Log rotation", "Maintenance", "Compresses application logs older than 7 days and deletes archives older than 90 days.", 2, true),
    new("RestartPending", "Pending restart check", "Maintenance", "Reports servers waiting for a restart after updates or role changes.", 1, false),
    new("PatchStatus", "Patch status", "Security", "Lists missing security updates and the date of the last successful install.", 2, false),
    new("Certificates", "Certificate expiry", "Security", "Finds machine certificates that expire within 30 days and the services that use them.", 1, false),
    new("LocalAdmins", "Local administrators audit", "Security", "Compares local Administrators membership with the approved list.", 1, false),
    new("Firewall", "Firewall rules review", "Security", "Flags inbound allow rules open to any address.", 2, false),
    new("EventErrors", "Event log errors", "Health", "Summarizes System and Application errors from the last 24 hours.", 1, false),
    new("Services", "Service health", "Health", "Checks that automatic services are running and restarts the ones that stopped.", 1, true),
    new("DiskSpace", "Disk space", "Health", "Warns below 15% free and fails below 5% free on every fixed volume.", 1, false),
    new("Uptime", "Uptime and reboots", "Health", "Reports uptime and unexpected reboots in the last 30 days.", 1, false)
    // </more>
};

IReadOnlyList<MaintenanceTask> tasks = new MultiSelectPrompt<MaintenanceTask>("Tasks", all, t => t.Name, t => t.Category) {
    Minimum = 1,
    Preview = t => new Rows(
        new TextBlock(Markup.Escape(t.Description)),
        new KeyValueList()
            .Add("Group", Markup.Escape(t.Category))
            .Add("Duration", "about " + t.Minutes + " min per server")
            .Add("Changes", t.Changes ? "[warn]yes[/], makes changes" : "[pass]no[/], read only")
            .Add("Parameter", "[accent]" + Markup.Escape(t.Id) + "[/]")).Gap(1)
}.Show();
Terminal.WriteMarkupLine("Running [bold]" + tasks.Count + "[/] tasks");

record MaintenanceTask(string Id, string Name, string Category, string Description, int Minutes, bool Changes);