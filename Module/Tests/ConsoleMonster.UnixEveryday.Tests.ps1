Describe 'Unix everyday host formatting' {
    BeforeAll {
        $script:Unix = [Environment]::OSVersion.Platform -eq [PlatformID]::Unix
        $script:Module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
    }

    It 'keeps mixed script output tabular and uses Unix defaults only for implicit host views' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Unix) {
        $source = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__'
Enable-ConsoleFormatting -Feature All, FormatCommands
'--- x'
Get-ChildItem | Select-Object -First 3
'--- implicit'
Get-ChildItem | Select-Object -First 3 | Out-Default
Get-Item '__MODULE__' | Format-List
Get-Process -Id $PID | Format-Table
'--- explicit'
Get-Process -Id $PID | Format-Table -Property Id, Handles
'--- finished'
'@
        $source = $source.Replace('__MODULE__', $script:Module.Replace("'", "''"))
        $file = Join-Path $TestDrive 'mixed.ps1'
        $capture = Join-Path $TestDrive 'mixed.ansi'
        [IO.File]::WriteAllText($file, $source)
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') $script:Runtime $file $capture
        $LASTEXITCODE | Should -Be 0
        $text = [IO.File]::ReadAllText($capture) -replace '\x1b\[[0-?]*[ -/]*[@-~]', ''
        $text | Should -Not -Match 'PSPath\s*:|UnixStat\s*:|PrivateBuild|FileVersion|VersionInfo'
        $text | Should -Not -Match '\[accent(?:2)?\]|\[pass\]'
        $text | Should -Match 'drwx'
        $text | Should -Match 'UnixMode'
        $text | Should -Match 'User'
        $text | Should -Match 'Size'
        $text | Should -Match 'KB'
        $implicit = ($text -split '--- implicit')[1] -split '--- explicit' | Select-Object -First 1
        $implicit | Should -Not -Match 'Handles|NPM\(|PM\(M\)'
        $implicit | Should -Match 'Memory'
        ($text -split '--- explicit')[1] | Should -Match 'Handles'
        $text.IndexOf('--- x') | Should -BeLessThan $text.IndexOf('--- implicit')
        $text.IndexOf('--- explicit') | Should -BeLessThan $text.IndexOf('--- finished')
    }
}
