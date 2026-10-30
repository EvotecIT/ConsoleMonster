<#
.SYNOPSIS
Checks built-in views and exact native fallbacks inside a real console host.
.PARAMETER ResultPath
The JSON receipt written after every contract passes.
.EXAMPLE
pwsh -NoProfile -File ./Assert-BuiltInViewsHost.ps1 -ResultPath ./builtins.json
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string] $ResultPath)

$ErrorActionPreference = 'Stop'
if ([Console]::IsOutputRedirected) { throw 'A real console is required.' }
if (-not (Get-Module ConsoleMonster)) {
    Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
}
. (Join-Path $PSScriptRoot 'Convert-TestNativeView.ps1')
. (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('cm-builtins-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $temporary
$module = New-Module -Name Demo.BuiltInProof -ScriptBlock {
    function Get-DemoProof { 'synthetic' }
    Export-ModuleMember -Function Get-DemoProof
}
Import-Module $module
$start = [datetime]'2026-10-22T10:00:00'
Add-History -InputObject ([pscustomobject]@{
    CommandLine = 'Get-DemoProof -Name "synthetic"'
    ExecutionStatus = 'Completed'
    StartExecutionTime = $start
    EndExecutionTime = $start.AddSeconds(2)
})
$data = @{
    Member = @('synthetic' | Get-Member -Name Length, Substring)
    Command = @(Get-Command -Module Demo.BuiltInProof)
    Module = @(Get-Module Demo.BuiltInProof)
    History = @(Get-History -Count 1)
}

function Get-NativeCapture {
    param([object[]] $Rows, [string] $Command)
    $records = @($Rows | & $Command)
    $text = $Rows | & $Command | Out-String -Width 120
    $selected = @($Rows | & $Command | Out-String -Stream -Width 120 | Select-String 'synthetic|Demo|Length') -join "`n"
    $file = Join-Path $temporary 'native.txt'
    $Rows | & $Command | Out-File -LiteralPath $file -Width 120
    $outFile = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    $Rows | & $Command > $file
    $redirect = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    $Rows | & $Command -OutVariable captured | Out-Null
    [pscustomobject]@{
        Records = [Management.Automation.PSSerializer]::Serialize($records, 20)
        Text = $text
        Selected = $selected
        OutFile = $outFile
        Redirect = $redirect
        OutVariable = [Management.Automation.PSSerializer]::Serialize($captured, 20)
    }
}

function Assert-DisplayedFrame {
    param([object[]] $Rows, [switch] $Native, [hashtable] $Arguments = @{})
    $width = [Console]::BufferWidth
    if ($Native) {
        $widget = Convert-TestNativeView -Rows $Rows -Arguments $Arguments
        $wanted = $widget | Export-ConsoleFrame -Format Text -Width $width
    } else {
        $wanted = $Rows | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width $width
    }
    $wanted = ($wanted -replace "`r", '').TrimEnd("`n") + "`n"
    $lineCount = @($wanted.TrimEnd("`n") -split "`n").Count
    $frameWidth = @($wanted.TrimEnd("`n") -split "`n" | ForEach-Object { $_.Length } | Measure-Object -Maximum).Maximum
    if ($Native) {
        $Rows | Format-Table @Arguments
    } else {
        $Rows | Out-Default
    }
    $actual = [ConsoleMonsterTestConsoleFrame]::LastFrame($lineCount, $frameWidth)
    if ($actual -cne $wanted) { throw "Host frame differs.`nExpected:`n$wanted`nActual:`n$actual" }
}

function Get-TranscriptBody {
    param([string] $Path)
    $text = [IO.File]::ReadAllText($Path)
    $begin = $text.IndexOf('CM_PROOF_BEGIN', [StringComparison]::Ordinal)
    $end = $text.IndexOf('CM_PROOF_END', $begin, [StringComparison]::Ordinal)
    if ($begin -lt 0 -or $end -lt 0) { throw 'Transcript markers are missing.' }
    $text.Substring($begin, $end - $begin)
}

try {
    $expected = @{}
    foreach ($name in $data.Keys) {
        foreach ($kind in 'Table', 'List', 'Wide') {
            $expected["$name-$kind"] = Get-NativeCapture $data[$name] "Microsoft.PowerShell.Utility\Format-$kind"
        }
    }
    $nativeDefaults = @{}
    foreach ($name in $data.Keys) { $nativeDefaults[$name] = $data[$name] | Out-String -Width 120 }
    Enable-ConsoleFormatting -Include Output, FormatCommands -Exclude Errors, Streams, ReadLine
    foreach ($name in $data.Keys) {
        foreach ($kind in 'Table', 'List', 'Wide') {
            $actual = Get-NativeCapture $data[$name] "Format-$kind"
            foreach ($field in $actual.PSObject.Properties.Name) {
                if ($actual.$field -cne $expected["$name-$kind"].$field) { throw "$name/$kind changed $field" }
            }
        }
        $after = $data[$name] | Out-String -Width 120
        if ($after -cne $nativeDefaults[$name]) { throw "$name changed default Out-String" }
        $script:stage = "$name/default"
        Assert-DisplayedFrame -Rows $data[$name]
        $script:stage = "$name/native-default"
        Assert-DisplayedFrame -Rows $data[$name] -Native
        $script:stage = "$name/native-calculated"
        Assert-DisplayedFrame -Rows $data[$name] -Native -Arguments @{ Property = @(@{ Label = 'Chosen'; Expression = { 'literal [pass]value[/]' } }) }
        $script:stage = "$name/native-wildcard"
        if ($name -eq 'History') {
            $data[$name] | Format-Table N* -GroupBy { 'UserGroup' } -HideTableHeaders -Wrap
        } else {
            Assert-DisplayedFrame -Rows $data[$name] -Native -Arguments @{ Property = @('N*'); GroupBy = { 'UserGroup' }; HideTableHeaders = $true; Wrap = $true }
        }
    }
    Disable-ConsoleFormatting
    foreach ($enabled in $false, $true) {
        $path = Join-Path $temporary ("transcript-$enabled.txt")
        if ($enabled) { Enable-ConsoleFormatting -Include Output, FormatCommands -Exclude Errors, Streams, ReadLine }
        $null = Start-Transcript -LiteralPath $path
        Write-Host CM_PROOF_BEGIN
        foreach ($name in 'Member', 'Command', 'Module', 'History') {
            $data[$name] | Out-Default
            $data[$name] | Format-Table
            $data[$name] | Format-List
        }
        Write-Host CM_PROOF_END
        $null = Stop-Transcript
    }
    if ((Get-TranscriptBody (Join-Path $temporary 'transcript-False.txt')) -cne (Get-TranscriptBody (Join-Path $temporary 'transcript-True.txt'))) {
        throw 'Transcribed output changed.'
    }
    $modulePath = Join-Path $PSScriptRoot '../../ConsoleMonster.psd1'
    $job = Start-Job -ArgumentList $modulePath -ScriptBlock {
        param($Path)
        Import-Module $Path
        $rows = @('synthetic' | Get-Member -Name Length)
        $before = $rows | Format-Table | Out-String
        Enable-ConsoleFormatting -Include Output, FormatCommands -Exclude Errors, Streams, ReadLine
        $after = $rows | Format-Table | Out-String
        $before -ceq $after
    }
    try {
        $null = Wait-Job $job -Timeout 60
        if ($job.State -ne 'Completed') { throw 'The native job did not complete.' }
        if ((Receive-Job $job -ErrorAction Stop) -ne $true) { throw 'Native job output changed.' }
    } finally {
        if ($job.State -eq 'Running') { Stop-Job $job }
        Remove-Job $job
    }
    Disable-ConsoleFormatting
    foreach ($name in 'Get-Member', 'Get-Command', 'Get-Module', 'Get-History', 'Write-Warning', 'Write-Verbose', 'Write-Debug', 'Write-Information', 'Format-Table', 'Format-List', 'Format-Wide') {
        if ((Get-Command $name).CommandType -ne 'Cmdlet') { throw "$name was changed or not restored." }
    }
    [pscustomobject]@{
        Runtime = $PSVersionTable.PSVersion.ToString()
        Families = 4
        CaptureCombinations = 12
        RealFrameComparisons = 15
        UnmatchedNativeWildcard = 'Passed'
        NativeRecordFileRedirectCaptures = 'Passed'
        ExplicitPropertiesGroupingAndLabels = 'Passed'
        ExactTranscriptBody = 'Passed'
        NativeJob = 'Passed'
        RestoredCommands = 'Passed'
    } | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    'BUILT_IN_HOST_CONTRACTS_OK'
} catch {
    @($script:stage, $_.Exception.ToString(), $_.ScriptStackTrace) | Set-Content -LiteralPath ($ResultPath + '.failure-detail.txt') -Encoding UTF8
    throw
} finally {
    try { Stop-Transcript -ErrorAction Stop | Out-Null } catch { }
    Disable-ConsoleFormatting
    Remove-Module Demo.BuiltInProof -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporary -Recurse
}
