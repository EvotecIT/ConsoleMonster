using System;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Output;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Follows a log file or a local Windows event log with a live, scrollable log view.</para>
    /// <para type="description">File lines are decoded incrementally and followed across observed truncation and rotation. Event logs use Get-WinEvent on Windows. The view follows new entries; scrolling pauses following and End resumes it. Filter and Highlight are literal case-insensitive substrings. Piped, redirected, CI and -NoLive output is ordinary literal text and never reads keyboard input. Count is source polls, as in Watch-Console: one takes a snapshot, zero follows until q, Escape or Ctrl+C. Capacity bounds retained history. The default interval is 0.25 seconds.</para>
    /// </summary>
    /// <example>
    /// <para>Inspect an existing log and highlight a component</para>
    /// <code># Requires an existing service.log file; reads two snapshots without changing the file.
    /// Watch-ConsoleLog -Path ./service.log -Highlight database -Count 2</code>
    /// </example>
    /// <example>
    /// <para>Return matching lines from an existing log as strings</para>
    /// <code># Requires an existing service.log file.
    /// Watch-ConsoleLog -Path ./service.log -Tail 50 -Filter ERROR -Count 1</code>
    /// </example>
    /// <example>
    /// <para>Follow only warnings and errors from the System event log</para>
    /// <code>Watch-ConsoleLog -LogName System -Level Warn</code>
    /// </example>
    [Cmdlet(VerbsCommon.Watch, "ConsoleLog", DefaultParameterSetName = "Path")]
    [OutputType(typeof(string))]
    public sealed class CmdletWatchConsoleLog : NoPipelineInputCmdlet {
        private readonly object _stopGate = new object();
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">One literal FileSystem path. Wildcards are not expanded.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]

        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">One literal local Windows event-log name, such as Application or System. Wildcards are not supported.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "EventLog")]
        [ValidateNotNullOrEmpty]
        [ValidatePattern(@"^[^*?\[\]]+$")]
        public string LogName { get; set; } = string.Empty;

        /// <summary><para type="description">File encoding without a BOM, default utf8. A UTF-8, UTF-16 or UTF-32 BOM takes precedence. Uses the same encoding names as Write-ConsoleText.</para></summary>
        [Parameter(ParameterSetName = "Path")]
        [ValidateSet("unknown", "string", "unicode", "bigendianunicode", "utf8", "utf8BOM", "utf8NoBOM", "utf32", "ascii", "default", "oem", IgnoreCase = true)]
        public string Encoding { get; set; } = "utf8";

        /// <summary><para type="description">Completed history lines or events shown initially; zero starts with later entries.</para></summary>
        [Parameter]
        [ValidateRange(0, 100000)]
        public int Tail { get; set; } = 10;

        /// <summary><para type="description">Source polls before returning, or zero to follow until cancelled.</para></summary>
        [Parameter]
        [ValidateRange(0L, long.MaxValue)]
        public long Count { get; set; }

        /// <summary><para type="description">Seconds between polls, from 0.05 to 86400; defaults to 0.25.</para></summary>
        [Parameter]
        [ValidateRange(0.05, 86400)]
        public double PollIntervalSeconds { get; set; } = 0.25;

        /// <summary><para type="description">Literal case-insensitive text required in a line. Null or empty accepts every line.</para></summary>
        [Parameter]
        public string? Filter { get; set; }

        /// <summary><para type="description">Keeps lines at this level or worse: Warn keeps warnings and errors, Verbose drops only debug lines. Levels are read from each line, as the live view colours them.</para></summary>
        [Parameter]
        [Binding.LogLevelTransformation]
        [ArgumentCompleter(typeof(Binding.LogLevelCompleter))]
        public string? Level { get; set; }

        /// <summary><para type="description">Literal case-insensitive text emphasized in live output. Plain output stays unchanged.</para></summary>
        [Parameter]
        public string? Highlight { get; set; }

        /// <summary><para type="description">Explicit invariant .NET source timestamp format for the live view, at every terminal width. By default, HH:mm:ss expands to yyyy-MM-dd HH:mm:ss when retained source dates differ. Missing timestamps stay blank; plain output keeps the original text.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? TimestampFormat { get; set; }

        /// <summary><para type="description">Maximum retained history lines, default 1000.</para></summary>
        [Parameter]
        [ValidateRange(1, 100000)]
        public int Capacity { get; set; } = 1000;

        /// <summary><para type="description">Writes literal lines without a live screen or keyboard input.</para></summary>
        [Parameter]
        public SwitchParameter NoLive { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            // The first Terminal access can enable Windows VT, including for plain output or a failing source.
            using (TerminalGuard modeGuard = new TerminalGuard(() => { }, false))
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                lock (_stopGate) { _stop = stop; }
                try {
                    TerminalProfile profile = Terminal.GetCurrentProfile();
                    bool last = MyInvocation.PipelinePosition >= MyInvocation.PipelineLength;
                    LogTailOptions options = new LogTailOptions {
                        Count = Count, PollInterval = TimeSpan.FromSeconds(PollIntervalSeconds), Capacity = Capacity,
                        Filter = Filter, Level = Level, Highlight = Highlight, TimestampFormat = TimestampFormat, HandleCancelKey = false, Profile = profile,
                        Live = !NoLive.IsPresent && last && !HostOutput.UseHost(this, profile) && profile.Mode == OutputMode.Interactive,
                        WriteLine = text => WriteObject(text)
                    };
                    if (ParameterSetName == "EventLog") {
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { throw new PlatformNotSupportedException(PSLogTailMessages.Get("WindowsRequired")); }
                        CommandInfo command = InvokeCommand.GetCommand("Get-WinEvent", CommandTypes.All) ?? throw new CommandNotFoundException("Get-WinEvent");
                        ConsoleEventLogTail source = new ConsoleEventLogTail(LogName, Tail, command);
                        ConsoleLogTail.Run(source.ReadAvailable, options, stop.Token);
                    } else {
                        string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out ProviderInfo provider, out _);
                        if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) { throw new PSArgumentException(PSLogTailMessages.Get("FileSystemRequired")); }
                        using (FileLogTail source = new FileLogTail(path, Tail, ConsoleLogFile.ResolveEncoding(Encoding))) { ConsoleLogTail.Run(source.ReadAvailable, options, stop.Token); }
                    }
                } catch (OperationCanceledException) when (stop.IsCancellationRequested) {
                    // StopProcessing owns PowerShell cancellation; the core has restored its terminal first.
                } finally { lock (_stopGate) { _stop = null; } }
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() { lock (_stopGate) { _stop?.Cancel(); } }
    }
}
