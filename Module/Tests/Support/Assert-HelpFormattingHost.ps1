<#
.SYNOPSIS
Proves opt-in Help routing on an actual local console, including native captures and paged keyboard input.
.PARAMETER ResultPath
JSON receipt for the completed checks. Output must remain attached to a console.
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string] $ResultPath)

$ErrorActionPreference = 'Stop'
if ([Console]::IsOutputRedirected -or $Host.Name -ne 'ConsoleHost') { throw 'An actual local ConsoleHost is required.' }
if (-not (Get-Module ConsoleMonster)) {
    Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
}
# Use the loaded module's root so PS5.1 sees one path to the fixture's about topic,
# including when the native site runner imports through its module junction.
Import-Module (Join-Path (Get-Module ConsoleMonster).ModuleBase 'Tests/Fixtures/HelpFormatting/HelpFormatting.psd1')
. (Join-Path $PSScriptRoot 'Get-HelpFormattingCapture.ps1')
. (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
$directory = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $directory
$savedPager = $env:PAGER
$savedProfile = [ConsoleMonster.Terminal]::Profile
$savedInput = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override
$nativeHelp = (Get-Command help).Definition
$nativeGetHelp = Get-Command Get-Help
$checks = [Collections.Generic.List[string]]::new()

function Assert-HelpFrame {
    param([string] $Expected, [string] $Case)
    $expected = ($Expected -replace "`r", '').TrimEnd("`n") + "`n"
    $lines = @($expected.TrimEnd("`n") -split "`n").Count
    $width = @($expected.TrimEnd("`n") -split "`n" | ForEach-Object Length | Measure-Object -Maximum).Maximum
    $actual = [ConsoleMonsterTestConsoleFrame]::LastFrame($lines, $width)
    if ($actual -cne $expected) { throw "Help frame differs for $Case.`nExpected:`n$expected`nActual:`n$actual" }
}

try {
    function global:Out-HelpFormattingTestPager { param($PagerArguments) process { $_ } }
    $env:PAGER = 'Out-HelpFormattingTestPager'
    $cases = @(
        'Get-Help Get-HelpFormattingValue -Full'
        'Get-Help Get-HelpFormattingValue -Detailed'
        'Get-Help Get-HelpFormattingValue -Examples'
        'Get-Help Get-HelpFormattingValue -Parameter *'
        'Get-HelpFormattingValue -?'
        'help Get-HelpFormattingValue -Examples'
    )
    $baseline = @{}
    foreach ($case in $cases) { $baseline[$case] = Get-HelpFormattingCapture $case $directory }
    Enable-ConsoleFormatting -Feature Help
    foreach ($case in $cases) {
        $actual = Get-HelpFormattingCapture $case $directory
        foreach ($name in $actual.PSObject.Properties.Name) {
            if ($actual.$name -cne $baseline[$case].$name) { throw "Changed native $name for $case with a console attached" }
        }
    }
    $checks.Add('Console-attached native record/text/file/redirect/selection/out-variable parity')

    # A script's outer Out-Default was resolved before import. Explicitly invoke the installed shim as its host sink.
    # The nested producer has no following command; this is the same pipe connection as the interactive prompt.
    $width = [Console]::BufferWidth
    [ConsoleMonster.Terminal]::Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($width, 200, 'None')
    $native = Microsoft.PowerShell.Core\Get-Help Get-HelpFormattingShort
    $factory = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.ConsoleViewRegistration')).Assembly.GetType('ConsoleMonster.PowerShell.PSHelp').GetMethod('CreateView', [Reflection.BindingFlags]'Static,NonPublic')
    $view = $factory.Invoke($null, @($native, (Get-Command Get-HelpFormattingShort).PSObject.BaseObject, $false, $false, $true))
    $expected = $view | Export-ConsoleFrame -Format Text -Width $width
    & { Get-Help Get-HelpFormattingShort } | Out-Default
    Assert-HelpFrame $expected 'Get-Help default'
    & { Get-HelpFormattingShort -? } | Out-Default
    Assert-HelpFrame $expected '-? default'
    $checks.Add('Default Get-Help and -? actual console cells equal the selected native help view')

    foreach ($paged in $false, $true) {
        Clear-Host
        & {
            [pscustomobject]@{ Order='BEFORE_HELP' }
            if ($paged) { help Get-HelpFormattingShort -Full } else { Get-Help Get-HelpFormattingShort -Full }
            [pscustomobject]@{ Order='AFTER_HELP' }
        } | Out-Default
        $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(22, $width)
        $beforeIndex = $frame.IndexOf('BEFORE_HELP', [StringComparison]::Ordinal)
        $helpIndex = $frame.IndexOf('Get-HelpFormattingShort', [StringComparison]::Ordinal)
        $afterIndex = $frame.IndexOf('AFTER_HELP', [StringComparison]::Ordinal)
        if ($beforeIndex -lt 0 -or $helpIndex -le $beforeIndex -or $afterIndex -le $helpIndex) { throw "Buffered output was reordered: $frame" }
    }
    $checks.Add('Buffered native objects retain their order around Get-Help and help')

    Clear-Host
    & { Get-Help Get-HelpFormattingValue -Examples } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(22, $width)
    if ($frame -notmatch 'HELP_SECOND_EXAMPLE' -or $frame -match 'HELP_DESCRIPTION|HELP_NAME_DESCRIPTION') { throw 'Examples did not select the example sections' }
    Clear-Host
    & { Get-Help Get-HelpFormattingValue -Parameter Name } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(22, $width)
    if ($frame -notmatch '-Name\s+\S\s+String' -or $frame -match '-Count|HELP_DESCRIPTION') { throw 'Parameter did not select the requested parameter' }
    Clear-Host
    & { Get-Help Get-HelpFormattingValue -Detailed } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(22, $width)
    if ($frame -notmatch 'HELP_SECOND_EXAMPLE' -or $frame -match 'HELP_NOTES') { throw 'Detailed did not retain examples and omit full-only notes' }
    Clear-Host
    & { Get-Help Get-HelpFormattingValue -Full } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(22, $width)
    if ($frame -notmatch 'HELP_NOTES' -or $frame -notmatch 'example.invalid') { throw 'Full did not retain notes and links' }
    $checks.Add('Full/Detailed/Examples/Parameter select their native sections on the actual host')

    Clear-Host
    & { Get-Help about_ConsoleMonsterHelpFormatting } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(18, $width)
    if ($frame -cnotmatch '\[brackets\], <angle brackets>' -or $frame -notmatch 'HELP_ABOUT_LITERAL') { throw "About topic did not stay literal: $frame" }
    $checks.Add('About topic stays literal on the actual host')

    Clear-Host
    & { Get-Help about_ConsoleMonsterMarkdown } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(18, $width)
    if ($frame -notmatch 'Markdown topic' -or $frame -match '# Markdown topic' -or $frame -notmatch 'web-01' -or $frame -notmatch '\[literal brackets\]') {
        throw "Markdown about topic did not use the shared reader: $frame"
    }
    $checks.Add('Markdown about topics use the shared reader on the actual host')

    # Captured results, identity pipelines and job results have no connected native producer.
    $saved = Get-Help Get-HelpFormattingShort -Full
    & { $saved } | Out-Default
    $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame(18, $width)
    if ($frame -cnotmatch 'RELATED LINKS' -or $frame -cnotmatch 'INPUTS') { throw "Captured help lost native formatting: $frame" }
    $checks.Add('Captured help displayed later remains native')

    $transcript = Join-Path $directory 'transcript.txt'
    $null = Start-Transcript -LiteralPath $transcript
    Disable-ConsoleFormatting
    'HELP_BASELINE_BEGIN' | Microsoft.PowerShell.Core\Out-Default
    & { Get-Help Get-HelpFormattingShort -Full } | Microsoft.PowerShell.Core\Out-Default
    'HELP_BASELINE_END' | Microsoft.PowerShell.Core\Out-Default
    Enable-ConsoleFormatting -Feature Help
    'HELP_ENABLED_BEGIN' | Microsoft.PowerShell.Core\Out-Default
    & { Get-Help Get-HelpFormattingShort -Full } | Out-Default
    'HELP_ENABLED_END' | Microsoft.PowerShell.Core\Out-Default
    $null = Stop-Transcript
    $recorded = Get-Content -LiteralPath $transcript -Raw
    $before = [regex]::Match($recorded, '(?s)HELP_BASELINE_BEGIN\r?\n(.*?)HELP_BASELINE_END').Groups[1].Value
    $after = [regex]::Match($recorded, '(?s)HELP_ENABLED_BEGIN\r?\n(.*?)HELP_ENABLED_END').Groups[1].Value
    if (-not $before -or $before -cne $after) { throw 'Transcript help body differs from native bytes' }
    $checks.Add('Transcript help body equals native text exactly')

    # A short help topic needs no input or footer.
    # PowerShell's help function selects Full for its default AllUsersView.
    $full = Microsoft.PowerShell.Core\Get-Help Get-HelpFormattingShort -Full
    $pagedView = $factory.Invoke($null, @($full, (Get-Command Get-HelpFormattingShort).PSObject.BaseObject, $false, $false, $true))
    $pagedExpected = $pagedView | Export-ConsoleFrame -Format Text -Width $width
    $input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('q')
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $input
    & { help Get-HelpFormattingShort } | Out-Default
    Assert-HelpFrame $pagedExpected 'short help pager'
    if ($input.Remaining -ne 1 -or ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) { throw 'Short help consumed input' }
    $checks.Add('Short help skips pager and leaves input alone')

    # Space advances, b returns, q exits. A remaining key proves early quit.
    [ConsoleMonster.Terminal]::Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($width, 10, 'None')
    $input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Space b q Space')
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $input
    & { help Get-HelpFormattingValue -Full } | Out-Default
    if ($input.Remaining -ne 1 -or ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) { throw 'Pager did not advance/quit/release input' }
    $checks.Add('Paged help advances with Space, returns with b, quits with q, releases input')

    $input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('q Space')
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $input
    & { help Get-HelpFormattingValue -Parameter * } | Out-Default
    if ($input.Remaining -ne 1 -or ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) { throw 'q did not stop all parameter help results' }
    & { Get-Help Get-HelpFormattingShort -Full } | Out-Default
    if ($input.Remaining -ne 1) { throw 'Paging state leaked into the next Get-Help command' }
    $checks.Add('q stops remaining parameter results and paging state does not leak')

    # A failed producer must release its marker before another command reuses this same active output pipe.
    $input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('q Space')
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $input
    $script:HelpFailureCaught = $false
    & {
        try { help Get-HelpFormattingValue -Parameter NoSuchParameter -ErrorAction Stop }
        catch { $script:HelpFailureCaught = $true }
        Get-Help Get-HelpFormattingValue -Full
    } | Out-Default
    if (-not $script:HelpFailureCaught) { throw 'The invalid parameter request did not exercise a terminating producer error' }
    if ($input.Remaining -ne 2 -or ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) { throw 'A failed help producer leaked paging into the same output sink' }
    $checks.Add('Terminating help failure releases paging before the same output sink continues')

    Disable-ConsoleFormatting
    if ((Get-Command help).Definition -cne $nativeHelp -or -not [object]::ReferenceEquals((Get-Command Get-Help), $nativeGetHelp)) { throw 'Commands were not restored' }
    $checks.Add('Exact restoration')
    [pscustomobject]@{ Runtime=$PSVersionTable.PSVersion.ToString(); Host=$Host.Name; Redirected=[Console]::IsOutputRedirected; Checks=@($checks) } |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    'HELP_HOST_CONTRACTS_OK'
} finally {
    try { Stop-Transcript -ErrorAction Stop | Out-Null } catch { }
    Disable-ConsoleFormatting
    [ConsoleMonster.Terminal]::Profile = $savedProfile
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $savedInput
    $env:PAGER = $savedPager
    Remove-Item Function:Out-HelpFormattingTestPager -ErrorAction Ignore
    Remove-Item -LiteralPath $directory -Recurse
}
