Describe 'Unix everyday host formatting' {
    BeforeAll {
        $script:Module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
    }

    It 'uses Unix host views with empty directory sizes, link sizes and explicit property overrides' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Unix) {
        $files = Join-Path $TestDrive 'files'
        $directory = New-Item -ItemType Directory -Path (Join-Path $files 'directory-row') -Force
        $dataFile = Join-Path $files 'file-row.json'
        [IO.File]::WriteAllText($dataFile, ('x' * 12345))
        New-Item -ItemType SymbolicLink -Path (Join-Path $files 'link-row') -Target $dataFile | Out-Null
        New-Item -ItemType SymbolicLink -Path (Join-Path $files 'link-directory-row') -Target $directory.FullName | Out-Null
        $source = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__'
Enable-ConsoleFormatting -Feature All, FormatCommands
'--- registered'
Get-ChildItem '__FILES__' | Sort-Object Name | Out-Default
'--- table'
Get-ChildItem '__FILES__' | Sort-Object Name | Format-Table
'--- list'
Get-Item '__MODULE__' | Format-List
Get-Process -Id $PID | Format-Table
'--- explicit'
Get-Process -Id $PID | Format-Table -Property Id, Handles
'@
        $source = $source.Replace('__MODULE__', $script:Module.Replace("'", "''"))
        $source = $source.Replace('__FILES__', $files.Replace("'", "''"))
        $file = Join-Path $TestDrive 'unix-views.ps1'
        $capture = Join-Path $TestDrive 'unix-views.ansi'
        [IO.File]::WriteAllText($file, $source)
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') $script:Runtime $file $capture
        $LASTEXITCODE | Should -Be 0
        $text = [IO.File]::ReadAllText($capture) -replace '\x1b\[[0-?]*[ -/]*[@-~]', ''
        $text | Should -Not -Match 'PrivateBuild|FileVersion|VersionInfo'
        $text | Should -Not -Match '\[accent(?:2)?\]|\[pass\]'
        $text | Should -Match 'drwx'
        $text | Should -Match 'UnixMode'
        $text | Should -Match 'User'
        $text | Should -Match 'Size'
        $text | Should -Match 'KB'
        foreach ($view in 'registered', 'table') {
            $listing = (($text -split "--- $view")[1] -split '---')[0]
            $rows = $listing -split '\r?\n'
            $directoryRow = @($rows | Where-Object { $_ -match '│\s+directory-row\s*│' })
            $directoryRow.Count | Should -Be 1
            ($directoryRow[0] -split '│')[-3].Trim() | Should -BeNullOrEmpty
            foreach ($name in 'file-row.json', 'link-row', 'link-directory-row') {
                $row = @($rows | Where-Object { $_ -match "│\s+$([regex]::Escape($name))\s*│" })
                $row.Count | Should -Be 1
                ($row[0] -split '│')[-3].Trim() | Should -Match '^\d+(\.\d+)?\s+(B|KB)$'
            }
        }
        $implicit = ($text -split '--- list')[1] -split '--- explicit' | Select-Object -First 1
        $implicit | Should -Not -Match 'Handles|NPM\(|PM\(M\)'
        $implicit | Should -Match 'Memory'
        ($text -split '--- explicit')[1] | Should -Match 'Handles'
    }
}
