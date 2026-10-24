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
Get-EverydayViewSample json-nested | Out-Default
Enable-ConsoleFormatting -Feature Output
Write-Host '### after ###'
Get-EverydayViewSample json-nested | Out-Default
Disable-ConsoleFormatting -Feature Output
Write-Host '### disabled ###'
Get-EverydayViewSample json-nested | Out-Default
Enable-ConsoleFormatting -Feature Output -NoBuiltIn
Write-Host '### no-built-in ###'
Get-EverydayViewSample json-nested | Out-Default
Disable-ConsoleFormatting
Write-Host '### end ###'
'@
        $source = $source.Replace('__MODULE__', $modulePath.Replace("'", "''"))
        $source = $source.Replace('__SAMPLE__', $samplePath.Replace("'", "''"))
        $file = Join-Path $TestDrive 'everyday-host.ps1'
        $capture = Join-Path $TestDrive 'everyday-host.ansi'
        [IO.File]::WriteAllText($file, $source)
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') ([Diagnostics.Process]::GetCurrentProcess().Path) $file $capture
        $LASTEXITCODE | Should -Be 0
        $text = [IO.File]::ReadAllText($capture) -replace '\x1b\[[0-?]*[ -/]*[@-~]', ''
        $before = (($text -split '### before ###')[1] -split '### after ###')[0].Trim()
        $after = (($text -split '### after ###')[1] -split '### disabled ###')[0].Trim()
        $disabled = (($text -split '### disabled ###')[1] -split '### no-built-in ###')[0].Trim()
        $noBuiltIn = (($text -split '### no-built-in ###')[1] -split '### end ###')[0].Trim()
        $after | Should -Match 'Settings\s+\{…\}'
        $after | Should -Match 'Tags\s+2 items'
        $after | Should -Not -Match '@\{'
        $before | Should -Match '@\{'
        $disabled | Should -BeExactly $before
        $noBuiltIn | Should -BeExactly $before
    }
}
