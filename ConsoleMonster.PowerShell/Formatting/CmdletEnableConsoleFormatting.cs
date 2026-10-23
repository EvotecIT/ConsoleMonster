using System;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows the default output of common types through ConsoleMonster for this session (opt-in).</para>
    /// <para type="description">Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), commands (Get-Command), modules (Get-Module), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.</para>
    /// </summary>
    /// <example>
    ///   <para>Turn it on for the session</para>
    ///   <code>Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10</code>
    /// </example>
    /// <example>
    ///   <para>The lines to keep it on in every session (printed, never written to the profile)</para>
    ///   <code>Enable-ConsoleFormatting -ProfileSnippet</code>
    /// </example>
    /// <example>
    ///   <para>Default output, the error view, progress and native stream colours</para>
    ///   <code>Enable-ConsoleFormatting -Feature All</code>
    /// </example>
    /// <example>
    ///   <para>Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour</para>
    ///   <code>Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose</code>
    /// </example>
    /// <example>
    ///   <para>Compact errors at the prompt; use -Detailed to include stacks. All does not enable this feature.</para>
    ///   <code>Enable-ConsoleFormatting -Feature Errors; Enable-ConsoleFormatting -Feature Errors -Detailed -ProfileSnippet</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Enable, "ConsoleFormatting")]
    public sealed class CmdletEnableConsoleFormatting : PSCmdlet {
            foreach (ConsoleFormattingFeature feature in Feature ?? new[] { ConsoleFormattingFeature.All | ConsoleFormattingFeature.FormatCommands | ConsoleFormattingFeature.Help | ConsoleFormattingFeature.Errors }) {
