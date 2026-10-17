using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>A server in the toolbox example (invented data).</summary>
    public sealed class ToolboxServer {
        /// <summary>Creates a server.</summary>
        public ToolboxServer(string name, string site, string role, string os, string? offline = null) {
            Name = name;
            Site = site;
            Role = role;
            OperatingSystem = os;
            Offline = offline;
        }

        /// <summary>The host name.</summary>
        public string Name { get; }

        /// <summary>The site.</summary>
        public string Site { get; }

        /// <summary>The role.</summary>
        public string Role { get; }

        /// <summary>The operating system.</summary>
        public string OperatingSystem { get; }

        /// <summary>Why the server cannot be used, or null.</summary>
        public string? Offline { get; }

        /// <inheritdoc />
        public override string ToString() => Name;
    }

    /// <summary>A maintenance task in the toolbox example.</summary>
    public sealed class ToolboxTask {
        /// <summary>Creates a task.</summary>
        public ToolboxTask(string id, string name, string category, string description, int minutes, bool changes) {
            Id = id;
            Name = name;
            Category = category;
            Description = description;
            Minutes = minutes;
            Changes = changes;
        }

        /// <summary>The parameter value.</summary>
        public string Id { get; }

        /// <summary>The display name.</summary>
        public string Name { get; }

        /// <summary>The group.</summary>
        public string Category { get; }

        /// <summary>What it does.</summary>
        public string Description { get; }

        /// <summary>Typical minutes per server.</summary>
        public int Minutes { get; }

        /// <summary>Whether it changes the server (otherwise it only reads).</summary>
        public bool Changes { get; }

        /// <inheritdoc />
        public override string ToString() => Id;
    }

    /// <summary>The invented servers and tasks of the toolbox example, and their preview content.</summary>
    public static class ToolboxData {
        /// <summary>Every server.</summary>
        public static IReadOnlyList<ToolboxServer> Servers { get; } = new[] {
            new ToolboxServer("FS01", "Warsaw", "File", "Windows Server 2022"),
            new ToolboxServer("FS02", "Warsaw", "File", "Windows Server 2022"),
            new ToolboxServer("WEB01", "Warsaw", "Web", "Windows Server 2025"),
            new ToolboxServer("SQL01", "Warsaw", "Database", "Windows Server 2019"),
            new ToolboxServer("FS11", "Berlin", "File", "Windows Server 2022"),
            new ToolboxServer("WEB11", "Berlin", "Web", "Windows Server 2025"),
            new ToolboxServer("PRN11", "Berlin", "Print", "Windows Server 2016", "offline since 09:12"),
            new ToolboxServer("BKP21", "Dublin", "Backup", "Windows Server 2022"),
            new ToolboxServer("SQL21", "Dublin", "Database", "Windows Server 2022"),
            new ToolboxServer("LAB01", "Lab", "Test", "Windows Server 2025"),
            new ToolboxServer("LAB02", "Lab", "Test", "Windows Server 2025", "in maintenance until Friday")
        };

        /// <summary>Every task.</summary>
        public static IReadOnlyList<ToolboxTask> Tasks { get; } = new[] {
            new ToolboxTask("DiskCleanup", "Disk cleanup", "Maintenance", "Removes temporary files, old update caches and crash dumps older than 30 days.", 3, true),
            new ToolboxTask("LogRotation", "Log rotation", "Maintenance", "Compresses application logs older than 7 days and deletes archives older than 90 days.", 2, true),
            new ToolboxTask("RestartPending", "Pending restart check", "Maintenance", "Reports servers waiting for a restart after updates or role changes.", 1, false),
            new ToolboxTask("PatchStatus", "Patch status", "Security", "Lists missing security updates and the date of the last successful install.", 2, false),
            new ToolboxTask("Certificates", "Certificate expiry", "Security", "Finds machine certificates that expire within 30 days and the services that use them.", 1, false),
            new ToolboxTask("LocalAdmins", "Local administrators audit", "Security", "Compares local Administrators membership with the approved list.", 1, false),
            new ToolboxTask("Firewall", "Firewall rules review", "Security", "Flags inbound allow rules open to any address.", 2, false),
            new ToolboxTask("EventErrors", "Event log errors", "Health", "Summarizes System and Application errors from the last 24 hours.", 1, false),
            new ToolboxTask("Services", "Service health", "Health", "Checks that automatic services are running and restarts the ones that stopped.", 1, true),
            new ToolboxTask("DiskSpace", "Disk space", "Health", "Warns below 15% free and fails below 5% free on every fixed volume.", 1, false),
            new ToolboxTask("Uptime", "Uptime and reboots", "Health", "Reports uptime and unexpected reboots in the last 30 days.", 1, false)
        };

        /// <summary>Loads the servers asynchronously, as a real tool would query an inventory.</summary>
        public static async Task<IEnumerable<Choice>> LoadServersAsync(string? site, CancellationToken token) {
            await Task.Delay(1100, token).ConfigureAwait(false);
            List<Choice> choices = new List<Choice>();
            foreach (ToolboxServer server in Servers) {
                if (site != null && site != "All sites" && server.Site != site) {
                    continue;
                }
                choices.Add(new Choice(server, server.Name) {
                    Group = server.Site,
                    Badge = RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted)),
                    Description = server.Role + " server · " + server.OperatingSystem,
                    DisabledReason = server.Offline
                });
            }
            return choices;
        }

        /// <summary>The task choices with groups, durations and change markers.</summary>
        public static List<Choice> TaskChoices() {
            List<Choice> choices = new List<Choice>();
            foreach (ToolboxTask task in Tasks) {
                RichTextBuilder badge = new RichTextBuilder();
                if (task.Changes) {
                    badge.Append("changes ", ThemeToken.Warn);
                }
                badge.Append("~" + task.Minutes + " min", ThemeToken.Muted);
                choices.Add(new Choice(task, task.Name) { Group = task.Category, Badge = badge.Build(), Description = task.Description });
            }
            return choices;
        }

        /// <summary>The preview of a task: what it does and what it touches.</summary>
        public static IWidget TaskPreview(ToolboxTask task) {
            KeyValueList facts = new KeyValueList()
                .Add("Group", Markup.Escape(task.Category))
                .Add("Duration", "about " + task.Minutes + " min per server")
                .Add("Changes", task.Changes ? "[warn]yes[/], makes changes" : "[pass]no[/], read only")
                .Add("Parameter", "[accent]" + Markup.Escape(task.Id) + "[/]");
            return new Rows(new TextBlock(Markup.Escape(task.Description)), facts) { Gap = 1 };
        }
    }
}
