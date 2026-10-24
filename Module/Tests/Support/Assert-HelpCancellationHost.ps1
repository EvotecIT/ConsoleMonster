<#
.SYNOPSIS
Qualifies real keyboard cancellation and normal quit in an owned Windows ConsoleHost.
.DESCRIPTION
Run Initialize, Cancel, VerifyCancellation, Quit and Finish as separate prompt commands
in the same fresh -NoProfile ConsoleHost. Wait for the corresponding .ready.json file,
send actual Ctrl+C for Cancel and q for Quit, then run the next phase at the prompt.
No keyboard override is used for Cancel or Quit. A managed observer only reads the
existing test helper's Win32 mode and the real ConsoleInput lease; it cannot send keys.
.PARAMETER Phase
The next phase of the interactive qualification. Cleanup restores test-owned state.
.PARAMETER ResultPath
Absolute JSON receipt path, reused by every phase. Sidecars record PID and raw input readiness.
.EXAMPLE
& ./Assert-HelpCancellationHost.ps1 -Phase Initialize -ResultPath D:/Scratch/keyboard.json
& ./Assert-HelpCancellationHost.ps1 -Phase Cancel -ResultPath D:/Scratch/keyboard.json
# Send Ctrl+C after keyboard.json.cancel.ready.json exists, then at the fresh prompt:
& ./Assert-HelpCancellationHost.ps1 -Phase VerifyCancellation -ResultPath D:/Scratch/keyboard.json
& ./Assert-HelpCancellationHost.ps1 -Phase Quit -ResultPath D:/Scratch/keyboard.json
# Send q after keyboard.json.quit.ready.json exists, then at the fresh prompt:
& ./Assert-HelpCancellationHost.ps1 -Phase Finish -ResultPath D:/Scratch/keyboard.json
.NOTES
Requires the existing local module build. Does not build, launch a host or change product settings.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Initialize', 'Cancel', 'VerifyCancellation', 'Quit', 'Finish', 'Cleanup')]
    [string] $Phase,
    [Parameter(Mandatory)]
    [string] $ResultPath
)

$ErrorActionPreference = 'Stop'
if ($Host.Name -ne 'ConsoleHost' -or [Console]::IsInputRedirected -or [Console]::IsOutputRedirected) {
    throw 'An actual interactive ConsoleHost with attached input and output is required.'
}
$ResultPath = [IO.Path]::GetFullPath($ResultPath)

if ($Phase -eq 'Initialize') {
    if (Get-Variable ConsoleMonsterHelpCancellation -Scope Global -ErrorAction Ignore) {
        throw 'This host already has a qualification state. Clean it up before restarting.'
    }
    Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
    Import-Module (Join-Path $PSScriptRoot '../Fixtures/HelpFormatting/HelpFormatting.psd1')
    . (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
    if (([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired -or $null -ne ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override) {
        throw 'The fresh host must have no input lease or override.'
    }
    if (-not ('ConsoleMonsterHelpLeaseObserver' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
public sealed class ConsoleMonsterHelpLeaseObserver : IDisposable {
    private readonly Thread worker;
    private readonly EventHandler<FirstChanceExceptionEventArgs> exceptionObserver;
    private volatile bool stopped;
    public string Error;
    public int PipelineStoppedCount;
    public ConsoleMonsterHelpLeaseObserver(Type input, Type frame, string path) {
        exceptionObserver = (sender, args) => {
            if (args.Exception.GetType().FullName == "System.Management.Automation.PipelineStoppedException")
                Interlocked.Increment(ref PipelineStoppedCount);
        };
        AppDomain.CurrentDomain.FirstChanceException += exceptionObserver;
        worker = new Thread(() => {
            try {
                PropertyInfo acquired = input.GetProperty("IsAcquired");
                PropertyInfo source = input.GetProperty("Override");
                MethodInfo mode = frame.GetMethod("InputMode");
                DateTime deadline = DateTime.UtcNow.AddSeconds(45);
                while (!stopped && DateTime.UtcNow < deadline) {
                    if ((bool)acquired.GetValue(null, null)) {
                        uint raw = (uint)mode.Invoke(null, null);
                        bool native = source.GetValue(null, null) == null;
                        File.WriteAllText(path, "{\"IsAcquired\":true,\"InputMode\":" + raw +
                            ",\"OverrideIsNull\":" + (native ? "true" : "false") + "}");
                        return;
                    }
                    Thread.Sleep(10);
                }
                if (!stopped) Error = "No real input lease observed within 45 seconds.";
            } catch (Exception error) { Error = error.ToString(); }
        });
        worker.IsBackground = true;
        worker.Start();
    }
    public void Dispose() {
        stopped = true;
        AppDomain.CurrentDomain.FirstChanceException -= exceptionObserver;
        if (!worker.Join(2000)) throw new InvalidOperationException("Observer did not stop.");
    }
}
'@
    }
    $global:ConsoleMonsterHelpCancellation = @{
        Pid = $PID
        Runtime = $PSVersionTable.PSVersion.ToString()
        Host = $Host.Name
        ResultPath = $ResultPath
        SavedProfile = [ConsoleMonster.Terminal]::Profile
        SavedInput = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override
        NativeHelp = (Get-Command help).Definition
        Checks = [Collections.Generic.List[string]]::new()
        Cancel = @{}
        Quit = @{}
    }
    Enable-ConsoleFormatting -Feature Help
    [ConsoleMonster.Terminal]::Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed([Console]::BufferWidth, 10, 'None')
    [IO.File]::WriteAllText($ResultPath + '.pid.txt', [string]$PID)
    'HELP_CANCELLATION_INITIALIZED'
    return
}

$state = $global:ConsoleMonsterHelpCancellation
if (-not $state -or $state.ResultPath -cne $ResultPath -or $state.Pid -ne $PID) {
    throw 'Initialize this exact receipt in the same host first.'
}

if ($Phase -eq 'Cancel' -or $Phase -eq 'Quit') {
    if ($Phase -eq 'Quit' -and -not $state.Cancel.Verified) {
        throw 'Verify cancellation from a fresh pipeline before testing q.'
    }
    $attempt = $state[$Phase]
    if ($attempt.Started) { throw 'Each keyboard phase runs once per fresh host.' }
    $attempt.Started = $true
    $attempt.BeforeMode = [ConsoleMonsterTestConsoleFrame]::InputMode()
    $attempt.AfterMarker = $false
    $attempt.FinallyRan = $false
    $readyPath = $ResultPath + '.' + $Phase.ToLowerInvariant() + '.ready.json'
    if (Test-Path -LiteralPath $readyPath) { throw 'Use a new receipt path; readiness evidence already exists.' }
    $observer = [ConsoleMonsterHelpLeaseObserver]::new(
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput')), [ConsoleMonsterTestConsoleFrame], $readyPath)
    try {
        & {
            help Get-HelpFormattingValue -Full
            $attempt.AfterMarker = $true
        } | Out-Default
    } catch {
        $attempt.ExceptionType = $_.Exception.GetType().FullName
        $attempt.ErrorId = $_.FullyQualifiedErrorId
        throw
    } finally {
        $attempt.AfterMode = [ConsoleMonsterTestConsoleFrame]::InputMode()
        $attempt.IsAcquiredAfter = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired
        $attempt.FinallyRan = $true
        $observer.Dispose()
        $attempt.ObserverError = $observer.Error
        $attempt.PipelineStoppedCount = $observer.PipelineStoppedCount
    }
    'HELP_KEYBOARD_PHASE_RETURNED'
    return
}

if ($Phase -eq 'VerifyCancellation' -or $Phase -eq 'Finish') {
    $name = if ($Phase -eq 'Finish') { 'Quit' } else { 'Cancel' }
    $attempt = $state[$name]
    $ready = Get-Content -LiteralPath ($ResultPath + '.' + $name.ToLowerInvariant() + '.ready.json') -Raw | ConvertFrom-Json
    if (-not $attempt.FinallyRan -or $attempt.ObserverError) { throw 'The keyboard phase did not finish cleanly.' }
    if (-not $ready.IsAcquired -or -not $ready.OverrideIsNull -or ($ready.InputMode -band 7) -ne 0) {
        throw 'The keyboard was not owned in raw mode with processed/line/echo input disabled.'
    }
    if ($attempt.AfterMode -ne $attempt.BeforeMode -or $attempt.IsAcquiredAfter -or ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) {
        throw 'The keyboard phase did not restore the exact mode and release the lease.'
    }
    if ($attempt.AfterMarker -ne ($name -eq 'Quit')) { throw 'Ctrl+C must stop the pipeline; q must return normally.' }
    if (($name -eq 'Cancel' -and $attempt.PipelineStoppedCount -lt 1) -or
        ($name -eq 'Quit' -and $attempt.PipelineStoppedCount -ne 0)) {
        throw 'Ctrl+C must throw PipelineStoppedException; q must not throw it.'
    }
    $attempt.RawMode = $ready.InputMode
    $attempt.Verified = $true
    $state.Checks.Add("$name real keyboard, raw lease, AFTER marker, exact mode restoration")

    # A long ordinary Get-Help in this NEW prompt pipeline must not request even one pager key.
    $sentinel = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('q Space')
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $sentinel
    try {
        & { Get-Help Get-HelpFormattingValue -Full } | Out-Default
        if ($sentinel.Remaining -ne 2 -or ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) {
            throw 'Paging leaked into ordinary Get-Help in the fresh pipeline.'
        }
        $attempt.FreshPipelineRemainingKeys = $sentinel.Remaining
        $attempt.FreshPipelineMode = [ConsoleMonsterTestConsoleFrame]::InputMode()
        if ($attempt.FreshPipelineMode -ne $attempt.BeforeMode) { throw 'Fresh-pipeline help changed the input mode.' }
        $state.Checks.Add("$name fresh-pipeline ordinary Get-Help consumes no pager input")
    } finally {
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $state.SavedInput
    }
    [pscustomobject]@{
        Runtime = $state.Runtime
        Host = $state.Host
        Pid = $state.Pid
        InputRedirected = [Console]::IsInputRedirected
        OutputRedirected = [Console]::IsOutputRedirected
        AssemblyPath = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting')).Assembly.Location
        AssemblySha256 = (Get-FileHash (((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting')).Assembly.Location)).Hash
        Cancel = $state.Cancel
        Quit = $state.Quit
        Checks = @($state.Checks)
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    if ($Phase -ne 'Finish') {
        'HELP_CANCELLATION_VERIFIED'
        return
    }
}

Disable-ConsoleFormatting
[ConsoleMonster.Terminal]::Profile = $state.SavedProfile
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $state.SavedInput
if ((Get-Command help).Definition -cne $state.NativeHelp) { throw 'The native help function was not restored.' }
Remove-Variable ConsoleMonsterHelpCancellation -Scope Global
'HELP_CANCELLATION_CLEANUP_OK'
