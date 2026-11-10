Describe 'Default view summaries and recursive file trees' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A redirected ASCII/Words child with en-US culture; `od` sends objects through the console view.
        function Invoke-SummaryChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "[Environment]::SetEnvironmentVariable('CONSOLEMONSTER_GLYPHS', 'Ascii')"
                "[Environment]::SetEnvironmentVariable('CONSOLEMONSTER_STATUS_MARKERS', 'Words')"
                "[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "Import-Module '$($script:ModulePath)' -Force"
                'Enable-ConsoleFormatting'
                '$out = [Management.Automation.CmdletInfo]::new(''Out-ConsoleDefault'', (Get-Command Show-Console).ImplementingType.Assembly.GetType(''ConsoleMonster.PowerShell.CmdletOutConsoleDefault''))'
                'function od { $input | & $out -Force }'
                'function New-Fake([string] $Type, [hashtable] $Data) { $o = [pscustomobject] $Data; $o.PSObject.TypeNames.Insert(0, $Type); $o }'
                $Script
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
    }

    It 'shows a registered summary under the console view only, and the view without it when the summary fails' {
        $text = Invoke-SummaryChild -Script @'
Register-ConsoleView -TypeName Contoso.Server -Property Name, State -Summary {
    param($servers)
    @{ Value = $servers.Count; Label = 'servers' }
    @{ Value = @($servers | Where-Object State -ne 'Running').Count; Label = 'down'; Role = 'Fail' }
    @{ Caption = 'disk'; Value = 3GB; Presenter = 'Bytes' }
}
$servers = 'web1', 'web2', 'db1' | ForEach-Object { [pscustomobject] @{ PSTypeName = 'Contoso.Server'; Name = $_; State = $(if ($_ -eq 'db1') { 'Stopped' } else { 'Running' }) } }
'== console'
$servers | od
'== table'
$servers | Show-ConsoleTable
Register-ConsoleView -TypeName Contoso.Broken -Property Name -Summary { throw 'no summary today' }
Register-ConsoleView -TypeName Contoso.Odd -Property Name -Summary { @{ Value = 1; Presenter = 'Nope' }; @{ Value = 2; Label = 'kept' } }
'== broken'
[pscustomobject] @{ PSTypeName = 'Contoso.Broken'; Name = 'still shown' } | od
[pscustomobject] @{ PSTypeName = 'Contoso.Odd'; Name = 'odd one' } | od
'== end'
'@
        $console, $rest = ($text -split '== console')[1] -split '== table'
        $table, $rest = $rest -split '== broken'
        $broken = ($rest -split '== end')[0]
        $console | Should -Match '3 servers \| 1 down \| disk 3 GB'
        $table | Should -Match 'db1'
        $table | Should -Not -Match 'servers \|'
        $broken | Should -Match 'still shown'
        $broken | Should -Not -Match 'no summary today'
        $broken | Should -Match 'odd one'
        $broken | Should -Match '(?m)^ 2 kept$'
    }

    It 'lists a recursive folder as one tree with listed folder sizes and keeps one folder as a table' {
        $root = Join-Path ([System.IO.Path]::GetTempPath()) ('cm-tree-' + [guid]::NewGuid().ToString('N'))
        try {
            $null = New-Item -ItemType Directory -Path (Join-Path $root 'logs/old') -Force
            [System.IO.File]::WriteAllBytes((Join-Path $root 'logs/today.log'), [byte[]]::new(100))
            [System.IO.File]::WriteAllBytes((Join-Path $root 'logs/old/june.log'), [byte[]]::new(50))
            [System.IO.File]::WriteAllBytes((Join-Path $root 'readme.txt'), [byte[]]::new(10))
            $text = Invoke-SummaryChild -Script @"
'== tree'
Get-ChildItem -LiteralPath '$root' -Recurse | od
'== files'
Get-ChildItem -LiteralPath '$root' -Recurse -File | od
'== folder'
Get-ChildItem -LiteralPath '$root' | od
"@
            $tree, $rest = ($text -split '== tree')[1] -split '== files'
            $files, $folder = $rest -split '== folder'
            $tree | Should -Match 'Directory: '
            $tree | Should -Match ([regex]::Escape((Split-Path $root -Leaf) + '/') + '\s+160 B')
            $tree | Should -Match 'logs/\s+150 B'
            $tree | Should -Match 'old/\s+50 B'
            $tree | Should -Match 'june\.log\s+50 B'
            $tree | Should -Not -Match '[┌╭]'
            $tree | Should -Match '2 folders \| 3 files \| 160 B \| largest today\.log'
            # Folders that were not listed themselves (-File) still hold their files.
            $files | Should -Match 'old/\s+50 B'
            $files | Should -Match '3 files \| 160 B'
            $folder | Should -Match '(?m)^\+-[-+]+\+$'
            $folder | Should -Match 'readme\.txt'
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'sums up everyday results: history, pings per destination, services and events' {
        $text = Invoke-SummaryChild -Script @'
$start = [datetime] '2026-11-08T21:00:00'
'== history'
1..4 | ForEach-Object { New-Fake 'Microsoft.PowerShell.Commands.HistoryInfo' @{
    Id = $_; StartExecutionTime = $start; EndExecutionTime = $start.AddSeconds($_ * 10)
    ExecutionStatus = $(if ($_ -eq 2) { 'Failed' } else { 'Completed' }); CommandLine = 'Get-Thing' } } | od
'== pings'
$(foreach ($destination in 'alpha.test', 'beta.test') { 1..3 | ForEach-Object { New-Fake 'Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus' @{
    Destination = $destination; Ping = $_; Address = '192.0.2.1'; Latency = [long] (10 * $_)
    Status = $(if ($destination -eq 'beta.test' -and $_ -eq 2) { 'TimedOut' } else { 'Success' }) } } }) | od
'== services'
$(foreach ($service in @(@('Running', 'Automatic'), @('Stopped', 'Automatic'), @('Stopped', 'Manual'), @('Running', 'Automatic'), @('Running', 'Manual'))) {
    New-Fake 'System.ServiceProcess.ServiceController' @{ Status = $service[0]; StartType = $service[1]; Name = 'svc'; DisplayName = 'Service'
        ServicesDependedOn = @(); DependentServices = @(); CanPauseAndContinue = $false } }) | od
'== events'
$(foreach ($level in 'Error', 'Warning', 'Warning', 'Information') { New-Fake 'System.Diagnostics.Eventing.Reader.EventLogRecord' @{
    TimeCreated = $start.AddHours(-$level.Length); Id = 1; LevelDisplayName = $level; ProviderName = 'Test'; Message = 'Something happened' } }) | od
'== end'
'@
        $history, $rest = ($text -split '== history')[1] -split '== pings'
        $pings, $rest = $rest -split '== services'
        $services, $rest = $rest -split '== events'
        $events = ($rest -split '== end')[0]
        $history | Should -Match '4 commands \| 1 failed \| took 1 m 40 s \| slowest 40 s \(#4\)'
        $pings | Should -Match '(?m)^ alpha\.test 3/3 replies \| min 10 ms \| avg 20 ms \| max 30 ms'
        $pings | Should -Match '(?m)^ beta\.test 2/3 replies \| min 10 ms \| avg 20 ms \| max 30 ms'
        $services | Should -Match '3 running \| 2 stopped \| 1 automatic not running'
        $events | Should -Match '1 error \| 2 warnings \| 1 information \| over 6 h'
    }
}
