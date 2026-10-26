using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Names the replacement when a script calls a command that 2.1.0 retired. There are no aliases or forwarding commands:
    /// the call still fails at once with a CommandNotFoundException, but the message says what to use and where the
    /// upgrade guide is, instead of PowerShell's generic "not recognized". A command of that name defined by the user (a
    /// shim function, another module) is found first, because PowerShell only asks when nothing matched.
    /// </summary>
    internal static class RetiredCommands {
        /// <summary>The upgrade guide with every retired command and its replacement.</summary>
        internal const string GuideUrl = "https://consolemonster.dev/guides/upgrading/";

        /// <summary>Every command retired since 2.0.1, including names that only existed between releases, and what to use now.</summary>
        internal static IReadOnlyDictionary<string, string> Replacements { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["Add-ConsoleValue"] = "Update-Console -Append -Value",
            ["ForEach-ConsoleItem"] = "Invoke-ConsoleRun",
            ["Format-ConsoleCode"] = "Show-Console -View Code",
            ["Format-ConsoleError"] = "Show-Console -View Error",
            ["Format-ConsoleJson"] = "Show-Console -View Json",
            ["Format-ConsoleList"] = "Show-ConsoleList",
            ["Format-ConsoleTable"] = "Show-ConsoleTable",
            ["Format-ConsoleTree"] = "Show-Console -View Tree",
            ["Invoke-ConsoleDemo"] = "Invoke-ConsolePresentation or Show-ConsolePresentation",
            ["New-ConsoleBarChart"] = "New-ConsoleChart -Type Bar",
            ["New-ConsoleBarItem"] = "[pscustomobject] rows with Label and Value on New-ConsoleChart -Type Bar",
            ["New-ConsoleChartSeries"] = "[pscustomobject] rows on New-ConsoleChart -Type Line",
            ["New-ConsoleClock"] = "New-ConsoleSimulatedClock",
            ["New-ConsoleFlowBranch"] = "[pscustomobject] rows on New-ConsoleFlow",
            ["New-ConsoleFlowEdge"] = "[pscustomobject] rows on New-ConsoleFlow",
            ["New-ConsoleFlowNode"] = "[pscustomobject] rows on New-ConsoleFlow",
            ["New-ConsoleFormPreset"] = "New-ConsoleForm -Preset with records",
            ["New-ConsoleGrid"] = "New-ConsoleLayoutGrid",
            ["New-ConsoleHistogram"] = "New-ConsoleChart -Type Histogram",
            ["New-ConsoleKeyValueItem"] = "[pscustomobject] rows on New-ConsoleKeyValue",
            ["New-ConsoleLane"] = "[pscustomobject] rows on New-ConsoleLanes",
            ["New-ConsoleLineChart"] = "New-ConsoleChart -Type Line",
            ["New-ConsoleLiveTable"] = "New-ConsoleTable -Live",
            ["New-ConsoleLog"] = "New-ConsoleLogRegion",
            ["New-ConsoleLogEntry"] = "[pscustomobject] rows on New-ConsoleLogRegion",
            ["New-ConsoleMenuPreset"] = "New-ConsoleMenu -Preset with records",
            ["New-ConsoleMultiSelect"] = "New-ConsoleSelect -Multiple",
            ["New-ConsoleRun"] = "Start-ConsoleRun",
            ["New-ConsoleRunResult"] = "New-ConsoleMenuRunResult",
            ["New-ConsoleSparkline"] = "New-ConsoleChart -Type Sparkline",
            ["New-ConsoleSpinner"] = "New-ConsoleProgress -Indeterminate",
            ["New-ConsoleStatusItem"] = "[pscustomobject] rows on New-ConsoleStatusList or New-ConsolePhases",
            ["New-ConsoleTextArea"] = "New-ConsoleTextInput -Multiline",
            ["New-ConsoleTreeNode"] = "[pscustomobject] rows on New-ConsoleTree",
            ["Out-ConsoleChart"] = "Show-Console -View Chart -Type <kind>",
            ["Read-ConsoleMultiSelect"] = "Read-ConsoleSelect -Multiple",
            ["Read-ConsoleMultiSelection"] = "Read-ConsoleSelect -Multiple",
            ["Read-ConsoleSecret"] = "Read-ConsoleText -AsSecret",
            ["Read-ConsoleSelection"] = "Read-ConsoleSelect",
            ["Set-ConsoleRegion"] = "Update-Console -Content",
            ["Set-ConsoleValue"] = "Update-Console -Key -Value",
            ["Show-ConsoleChart"] = "Show-Console -View Chart -Type <kind>",
            ["Show-ConsoleCode"] = "Show-Console -View Code",
            ["Show-ConsoleError"] = "Show-Console -View Error",
            ["Show-ConsoleJson"] = "Show-Console -View Json",
            ["Show-ConsoleMarkdown"] = "Show-Console -View Markdown",
            ["Show-ConsoleRule"] = "Show-Console -View Rule",
            ["Show-ConsoleTree"] = "Show-Console -View Tree",
            ["Start-ConsoleDashboard"] = "Invoke-ConsoleDashboard",
            ["Start-ConsoleDemo"] = "Invoke-ConsolePresentation or Show-ConsolePresentation",
            ["Start-ConsoleRecording"] = "Invoke-ConsoleRecording -Interactive",
            ["Start-ConsoleReplay"] = "Show-ConsoleReplay -PlaybackMode Automatic",
            ["Update-ConsoleActivity"] = "Update-Console -Key",
            ["Update-ConsoleCell"] = "Update-Console -Index -Status",
            ["Update-ConsoleFlow"] = "Update-Console -NodeKey or -EdgeKey",
            ["Update-ConsoleLane"] = "Update-Console -Key",
            ["Update-ConsoleLiveTable"] = "Update-Console -Row",
            ["Update-ConsolePhase"] = "Update-Console -Key",
            ["Update-ConsoleProgress"] = "Update-Console",
            ["Update-ConsoleRun"] = "Update-Console -Run",
            ["Update-ConsoleSpinner"] = "Update-Console",
            ["Update-ConsoleStatus"] = "Update-Console -Key",
            ["Update-ConsoleStatusItem"] = "Update-Console -Key",
            ["Update-ConsoleSummaryBar"] = "Update-Console",
            ["Update-ConsoleTable"] = "Update-Console -Row",
            ["Wait-ConsoleRun"] = "Complete-ConsoleRun -Wait",
            ["Write-ConsoleException"] = "Show-Console -View Error",
            ["Write-ConsoleKeyValue"] = "Show-ConsoleList",
            ["Write-ConsoleLog"] = "Write-ConsoleLogEntry",
            ["Write-ConsoleMarkup"] = "Write-ConsoleText -Markup",
            ["Write-ConsoleProgress"] = "native Write-Progress inside Use-ConsoleProgress",
            ["Write-ConsoleRule"] = "Show-Console -View Rule",
            ["Write-ConsoleStatus"] = "Show-ConsoleStatusLine",
            ["Write-ConsoleTable"] = "Show-ConsoleTable",
            ["Write-ConsoleTree"] = "Show-Console -View Tree",
            ["Write-ConsoleWidget"] = "Show-Console",
        };

        /// <summary>The message for a retired command, or null when the name was never a ConsoleMonster command.</summary>
        /// <param name="name">The command name as typed.</param>
        /// <returns>The message.</returns>
        internal static string? Message(string? name) {
            if (name == null || !Replacements.TryGetValue(name, out string? replacement)) { return null; }
            return name + " was retired in ConsoleMonster 2.1.0. Use " + replacement + " instead. Every renamed command: " + GuideUrl;
        }

        // A function that accepts whatever the old call passed, so the message is the first and only error.
        private const string Thrower = "[CmdletBinding()] param([Parameter(ValueFromRemainingArguments = $true)] $Arguments, [Parameter(ValueFromPipeline = $true)] $InputObject) " +
            "begin { $record = [System.Management.Automation.ErrorRecord]::new([System.Management.Automation.CommandNotFoundException]::new($args[0]), 'CommandNotFoundException', 'ObjectNotFound', $args[1]); $record.CategoryInfo.Activity = $args[1]; $PSCmdlet.ThrowTerminatingError($record) }";

        // Import runs inside the Import-Module pipeline, where the runspace's SessionStateProxy is not available: the
        // runspace's own $ExecutionContext is. A copy of the handler from another load of the assembly is found by name,
        // so the runspace never has two.
        internal static void Register() {
            CommandInvocationIntrinsics? commands = Commands();
            if (commands == null) { return; }
            RemoveAll(commands);
            commands.CommandNotFoundAction += OnCommandNotFound;
        }

        internal static void Unregister() {
            CommandInvocationIntrinsics? commands = Commands();
            if (commands != null) { RemoveAll(commands); }
        }

        private static void RemoveAll(CommandInvocationIntrinsics commands) {
            EventHandler<CommandLookupEventArgs>? handlers = commands.CommandNotFoundAction;
            if (handlers == null) { return; }
            foreach (Delegate handler in handlers.GetInvocationList()) {
                if (handler.Method.DeclaringType?.FullName == typeof(RetiredCommands).FullName && handler.Method.Name == nameof(OnCommandNotFound)) {
                    commands.CommandNotFoundAction -= (EventHandler<CommandLookupEventArgs>)handler;
                }
            }
        }

        // A hint is never worth a failed import: a host that does not give out $ExecutionContext simply goes without it.
        private static CommandInvocationIntrinsics? Commands() {
            if (Runspace.DefaultRunspace == null) { return null; }
            try {
                foreach (PSObject value in ScriptBlock.Create("$ExecutionContext").Invoke()) {
                    if (value?.BaseObject is EngineIntrinsics engine) { return engine.InvokeCommand; }
                }
            } catch (RuntimeException) {
            }
            return null;
        }

        private static void OnCommandNotFound(object? sender, CommandLookupEventArgs e) {
            if (e.StopSearch || e.Command != null || e.CommandScriptBlock != null) { return; }
            string? message = Message(e.CommandName);
            if (message == null) { return; }
            string name = e.CommandName.Replace("'", "''");
            e.CommandScriptBlock = ScriptBlock.Create(Thrower.Replace("$args[0]", "'" + message.Replace("'", "''") + "'").Replace("$args[1]", "'" + name + "'"));
            e.StopSearch = true;
        }
    }
}
