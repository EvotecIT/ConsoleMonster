using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>Every prompt type in a row (inline, in the scrollback), and a prompt that pauses a live dashboard.</summary>
    public static class PromptTour {
        /// <summary>Asks one of each prompt, or only the named one (select, multi, confirm, text, secret, number, path, typed).</summary>
        /// <param name="only">The prompt to show, or null for all.</param>
        public static void Run(string? only) {
            bool All(string name) => only == null || string.Equals(only, name, StringComparison.OrdinalIgnoreCase);
            if (All("select")) {
                SelectPrompt<ToolboxServer> select = new SelectPrompt<ToolboxServer>("Target server", ToolboxData.Servers, s => s.Name, s => s.Site) {
                    Description = "Type to filter; offline servers show why they cannot be picked."
                };
                foreach (Choice c in select.Choices) {
                    ToolboxServer s = (ToolboxServer)c.Value!;
                    c.Badge = RichText.Plain(s.Role, new RichStyle(ThemeToken.Muted));
                    c.DisabledReason = s.Offline;
                    c.Description = s.Role + " server · " + s.OperatingSystem;
                }
                ToolboxServer server = select.Show();
                Terminal.WriteMarkupLine("[muted]returned:[/] " + Markup.Escape(server.Name + " (" + server.GetType().Name + ")"));
            }
            if (All("multi")) {
                MultiSelectPrompt<ToolboxTask> multi = new MultiSelectPrompt<ToolboxTask>("Tasks", ToolboxData.Tasks, t => t.Name, t => t.Category) {
                    Minimum = 1,
                    Preview = ToolboxData.TaskPreview
                };
                IReadOnlyList<ToolboxTask> tasks = multi.Show();
                Terminal.WriteMarkupLine("[muted]returned:[/] " + Markup.Escape(string.Join(", ", tasks.Select(t => t.Id))));
            }
            if (All("confirm")) {
                Prompt.Confirm("Restart the print spooler on FS01?", true);
            }
            if (All("text")) {
                new TextPrompt("Share name") {
                    Default = "Reports",
                    Pattern = "[A-Za-z][A-Za-z0-9_-]{1,23}",
                    PatternMessage = "Start with a letter; letters, digits, - and _ only",
                    Description = "Shown to users when they browse the server."
                }.Show();
            }
            if (All("secret")) {
                new SecretPrompt("Password for svc-backup") { MinLength = 8 }.Show();
            }
            if (All("number")) {
                new NumberPrompt("Port") { Integer = true, Minimum = 1, Maximum = 65535, Default = 443 }.Show();
            }
            if (All("path")) {
                new PathPrompt("Report folder") { Kind = PathKind.Folder, Default = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }.Show();
            }
            if (All("typed")) {
                new TypedConfirmPrompt("Remove FS01 from the inventory?", "FS01") { Warning = "Its history and reports are deleted too. This cannot be undone." }.Show();
            }
        }

        /// <summary>A live dashboard that asks a question halfway through: the dashboard pauses behind the prompt and resumes.</summary>
        public static void Overlay() {
            Lanes lanes = new Lanes("lanes");
            foreach (ToolboxServer s in ToolboxData.Servers.Take(5)) {
                lanes.Add(s.Name, s.Name, 6);
            }
            LogRegion log = new LogRegion { PreferredHeight = 6 };
            Dashboard dashboard = Ui.Dashboard().Title("[bold]Patching[/]").Subtitle("5 servers")
                .Content(body => body
                    .Panel("Servers", lanes).Panel("Log", log, fill: true));
            using (LiveSession session = new LiveSession(dashboard, new LiveSessionOptions { PrintFinalState = true })) {
                session.Run(s => {
                    LanesHandle handle = lanes.Handle(s);
                    LogHandle output = log.Handle(s);
                    for (int step = 1; step <= 6; step++) {
                        foreach (ToolboxServer server in ToolboxData.Servers.Take(5)) {
                            handle.Advance(server.Name, server.Name == "WEB01" && step == 3 ? StatusKind.Fail : StatusKind.Pass, "step " + step);
                        }
                        output.Write("step " + step + " done");
                        Thread.Sleep(500);
                        if (step == 3) {
                            output.Error("WEB01: update KB5034441 failed (0x80070643)");
                            bool keepGoing = Prompt.Confirm("WEB01 failed. Continue with the other servers?", true);
                            output.Write(keepGoing ? "continuing" : "stopping after this step");
                            if (!keepGoing) {
                                break;
                            }
                        }
                    }
                });
            }
        }
    }
}
