Describe 'Unix everyday host formatting' {
    BeforeAll {
        $script:Module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        # Linux kernels with dev.tty.legacy_tiocsti=0 can't take typed keys back, so static output doesn't ask there.
        $script:ProbesStaticOutput = -not (Test-Path '/proc/sys/dev/tty/legacy_tiocsti') -or (Get-Content '/proc/sys/dev/tty/legacy_tiocsti' -Raw).Trim() -eq '1'
    }

    It 'uses Unix host views with directory size placeholders, link sizes and explicit property overrides' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Unix) {
        $files = Join-Path $TestDrive 'files'
        $directory = New-Item -ItemType Directory -Path (Join-Path $files 'directory-row') -Force
        $dataFile = Join-Path $files 'file-row.json'
        [IO.File]::WriteAllText($dataFile, ('x' * 12345))
        New-Item -ItemType SymbolicLink -Path (Join-Path $files 'link-row') -Target $dataFile | Out-Null
        New-Item -ItemType SymbolicLink -Path (Join-Path $files 'link-directory-row') -Target $directory.FullName | Out-Null
        $source = @'
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'en-US'
[Threading.Thread]::CurrentThread.CurrentUICulture = [cultureinfo]'en-US'
Import-Module '__MODULE__'
Enable-ConsoleFormatting
'--- registered'
Get-ChildItem '__FILES__' | Sort-Object Name
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
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') $script:Runtime $file $capture 30 120 --submissions
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
            ($directoryRow[0] -split '│')[-3].Trim() | Should -BeExactly '—'
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

    It 'asks a light terminal for its background before static output and leaves no reply in the input' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Unix) {
        $source = @'
Import-Module '__MODULE__'
[pscustomobject]@{ Check = 'SPF'; Status = 'Pass' } | Show-ConsoleTable -Property Check, @{ Name = 'Status'; Presenter = 'Status' }
Start-Sleep -Milliseconds 300
"theme=$([ConsoleMonster.Terminal]::Theme.Name) pending=$([Console]::KeyAvailable)"
'@
        $file = Join-Path $TestDrive 'light-table.ps1'
        $capture = Join-Path $TestDrive 'light-table.ansi'
        [IO.File]::WriteAllText($file, $source.Replace('__MODULE__', $script:Module.Replace("'", "''")))
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') $script:Runtime $file $capture 30 120 --light-background
        $LASTEXITCODE | Should -Be 0
        $text = [IO.File]::ReadAllText($capture)
        $text | Should -Match 'pending=False' -Because $text
        if ($script:ProbesStaticOutput) { $text | Should -Match 'theme=Light pending' -Because $text }
    }

    It 'hands a key typed during the background query to the next reader' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Unix) {
        $source = @'
Import-Module '__MODULE__'
[pscustomobject]@{ Check = 'SPF'; Status = 'Pass' } | Show-ConsoleTable -Property Check, @{ Name = 'Status'; Presenter = 'Status' }
Start-Sleep -Milliseconds 300
$typed = if ([Console]::KeyAvailable) { [Console]::ReadKey($true).KeyChar } else { 'none' }
"typed=$typed pending=$([Console]::KeyAvailable)"
'@
        $file = Join-Path $TestDrive 'typed-table.ps1'
        $capture = Join-Path $TestDrive 'typed-table.ansi'
        [IO.File]::WriteAllText($file, $source.Replace('__MODULE__', $script:Module.Replace("'", "''")))
        & python3 (Join-Path $PSScriptRoot 'Support/formatting_pty.py') $script:Runtime $file $capture 30 120 --light-background --type-during-probe
        $LASTEXITCODE | Should -Be 0
        $text = [IO.File]::ReadAllText($capture)
        $text | Should -Match 'typed=q pending=False' -Because $text
    }
}
