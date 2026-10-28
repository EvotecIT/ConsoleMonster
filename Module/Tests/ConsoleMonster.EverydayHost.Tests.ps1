Describe 'Everyday data at the interactive host boundary' {
    It 'changes only opted-in host display and restores native JSON output when disabled or built-ins are excluded' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Unix) {
        $modulePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $samplePath = Join-Path $PSScriptRoot 'Support/Get-EverydayViewSample.ps1'
        $source = @'
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'en-US'
Import-Module '__MODULE__'
. '__SAMPLE__'
Write-Host '### before ###'
Get-EverydayViewSample json-nested
Enable-ConsoleFormatting -Feature Output
Write-Host '### after ###'
Get-EverydayViewSample json-nested
Disable-ConsoleFormatting -Feature Output
Write-Host '### disabled ###'
Get-EverydayViewSample json-nested
Enable-ConsoleFormatting -Feature Output -NoBuiltIn
Write-Host '### no-built-in ###'
Get-EverydayViewSample json-nested
Disable-ConsoleFormatting
Enable-ConsoleFormatting -Feature Output
Write-Host '### explicit ###'
& '__EXPLICIT__'
Write-Host '### pipeline ###'
Get-EverydayViewSample json-nested | Out-String | Write-Host
Start-Transcript -LiteralPath '__TRANSCRIPT__' | Out-Null
Write-Host '### transcript ###'
Get-EverydayViewSample json-nested
Write-Host '### transcript-end ###'
Stop-Transcript | Out-Null
$env:NO_COLOR='1'
[ConsoleMonster.Terminal]::Refresh()
Write-Host '### plain ###'
Get-EverydayViewSample json-nested
Write-Host '### plain-end ###'
$env:CI='1'
[ConsoleMonster.Terminal]::Refresh()
Write-Host '### ci ###'
Get-EverydayViewSample json-nested
Disable-ConsoleFormatting
Write-Host '### end ###'
'@
        $source = $source.Replace('__MODULE__', $modulePath.Replace("'", "''"))
        $source = $source.Replace('__SAMPLE__', $samplePath.Replace("'", "''"))
        $source = $source.Replace('__TRANSCRIPT__', (Join-Path $TestDrive 'host-transcript.txt').Replace("'", "''"))
        $explicitScript = Join-Path $TestDrive 'explicit-default.ps1'
        [IO.File]::WriteAllText($explicitScript, 'Get-EverydayViewSample json-nested | Out-Default')
        $source = $source.Replace('__EXPLICIT__', $explicitScript.Replace("'", "''"))
        $file = Join-Path $TestDrive 'everyday-host.ps1'
        $capture = Join-Path $TestDrive 'everyday-host.ansi'
        [IO.File]::WriteAllText($file, $source)
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') ([Diagnostics.Process]::GetCurrentProcess().Path) $file $capture 30 120 --submissions
        $LASTEXITCODE | Should -Be 0
        $text = [IO.File]::ReadAllText($capture) -replace '\x1b\[[0-?]*[ -/]*[@-~]', ''
        $before = (($text -split '### before ###')[1] -split '### after ###')[0].Trim()
        $after = (($text -split '### after ###')[1] -split '### disabled ###')[0].Trim()
        $disabled = (($text -split '### disabled ###')[1] -split '### no-built-in ###')[0].Trim()
        $noBuiltIn = (($text -split '### no-built-in ###')[1] -split '### explicit ###')[0].Trim()
        $after | Should -Match 'Settings\s+Region=west, Retry=\{Count=3\}'
        $after | Should -Match 'Tags\s+blue, ready'
        $after | Should -Match 'dns'
        $after | Should -Match 'http'
        $after | Should -Match 'Empty\s+\(none\)'
        $after | Should -Not -Match '@\{'
        $before | Should -Match '@\{'
        $disabled | Should -BeExactly $before
        $noBuiltIn | Should -BeExactly $before
        $explicit = (($text -split '### explicit ###')[1] -split '### pipeline ###')[0].Trim()
        $pipeline = (($text -split '### pipeline ###')[1] -split '### transcript ###')[0].Trim()
        $transcript = (($text -split '### transcript ###')[1] -split '### transcript-end ###')[0].Trim()
        $plain = (($text -split '### plain ###')[1] -split '### plain-end ###')[0].Trim()
        $ci = (($text -split '### ci ###')[1] -split '### end ###')[0].Trim()
        $explicit | Should -BeExactly $before
        $pipeline | Should -BeExactly $before
        $transcript | Should -BeExactly $before
        $plain | Should -Match 'Settings\s+Region=west, Retry=\{Count=3\}'
        $plain | Should -Not -Match '@\{'
        $ci | Should -BeExactly $before
    }
}
