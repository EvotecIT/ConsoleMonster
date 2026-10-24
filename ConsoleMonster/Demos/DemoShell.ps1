# PowerShell's parser and execution engine remain authoritative. The demo driver
# sends printable input, Ctrl-U to discard it and RS to execute the complete step.
$ErrorActionPreference = 'Continue'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Remove-Module PSReadLine -ErrorAction SilentlyContinue
if ($env:CM_DEMO_MODULE) { Import-Module $env:CM_DEMO_MODULE }
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSStyle.OutputRendering = 'Ansi'
    $PSStyle.Progress.View = 'Classic'
}
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Typed demos require PowerShell 7.' }
if (-not ('ConsoleMonster.DemoChild.TerminalInputRelay' -as [type])) {
    $__cmSource = [IO.File]::ReadAllText($env:CM_DEMO_INPUT_SOURCE)
    Add-Type -TypeDefinition $__cmSource -ErrorAction Stop
}
function global:__ConsoleMonsterPrompt {
    [Console]::Write("$([char]27)]133;A$([char]7)")
    [Console]::Write([string](prompt))
    [Console]::Write("$([char]27)]133;B$([char]7)")
}
$__cmreader = [ConsoleMonster.DemoChild.TerminalInputRelay]::new($true)
$__cmCharacters = [Collections.Generic.Queue[char]]::new()
$__cmcommand = [Text.StringBuilder]::new()
try {
    __ConsoleMonsterPrompt
    while ($true) {
        while ($__cmCharacters.Count -eq 0) {
            $__cmchunk = $__cmreader.ReadAvailable()
            foreach ($__cmchar in $__cmchunk.ToCharArray()) { $__cmCharacters.Enqueue($__cmchar) }
            if ($__cmCharacters.Count -eq 0) { Start-Sleep -Milliseconds 5 }
        }
        $__cmvalue = [int]$__cmCharacters.Dequeue()
        if ($__cmvalue -eq 29) {
            [Console]::Write("$([char]27)[0m$([char]27)]133;A$([char]7)$([char]27)]133;B$([char]7)")
            continue
        }
        if ($__cmvalue -eq 21) {
            $null = $__cmcommand.Clear()
            [Console]::Write("`r$([char]27)[2K")
            __ConsoleMonsterPrompt
            continue
        }
        if ($__cmvalue -ne 30) {
            if ($__cmvalue -eq 13 -or $__cmvalue -eq 10) {
                $null = $__cmcommand.Append("`n")
                [Console]::Write("`r`n")
            } else {
                $null = $__cmcommand.Append([char]$__cmvalue)
                [Console]::Write([char]$__cmvalue)
            }
            continue
        }
        [Console]::Write("`r`n$([char]27)]133;C$([char]7)")
        try {
            if ($env:CM_DEMO_SCRIPT_PATH) {
                $__cmTokens = $null
                $__cmErrors = $null
                $__cmAst = [Management.Automation.Language.Parser]::ParseInput($__cmcommand.ToString(), $env:CM_DEMO_SCRIPT_PATH, [ref]$__cmTokens, [ref]$__cmErrors)
                $__cmBlock = $__cmAst.GetScriptBlock()
            } else {
                $__cmBlock = [scriptblock]::Create($__cmcommand.ToString())
            }
            . $__cmBlock | Out-Default
            $__cmstatus = if ($?) { 0 } else { 1 }
        } catch {
            $_ | Out-Default
            $__cmstatus = 1
        }
        [Console]::Write("$([char]27)]133;D;$__cmstatus$([char]7)")
        $null = $__cmcommand.Clear()
        __ConsoleMonsterPrompt
    }
} finally { $__cmreader.Dispose() }
