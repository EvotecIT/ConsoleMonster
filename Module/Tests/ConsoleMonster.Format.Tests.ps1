Describe 'Structured Show-Console cmdlets' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # Runs a script in a child process of the same runtime with redirected output, plain mode and en-US culture, so the
        # output is deterministic text without escape codes.
        function Invoke-FormatChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'COLORTERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            $lines.Add("`$env:CONSOLEMONSTER_MODE = 'plain'")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add('$artifacts = @(')
            $lines.Add("    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.nupkg'; Kind = 'NuGet'; Length = 1482113; Time = [timespan]::FromSeconds(48); Path = 'C:\Build\Artefacts\Packed\ConsoleMonster.2.0.0.nupkg'; Notes = '' }")
            $lines.Add("    [pscustomobject] @{ Name = 'ConsoleMonster-2.0.0.zip'; Kind = 'Module'; Length = 2904576; Time = [timespan]::FromSeconds(12); Path = 'C:\Build\Artefacts\Packed\ConsoleMonster-2.0.0.zip'; Notes = 'signed' }")
            $lines.Add("    [pscustomobject] @{ Name = 'Docs.zip'; Kind = 'Module'; Length = 51302; Time = [timespan]::FromSeconds(1); Path = 'C:\Build\Artefacts\Packed\Docs.zip'; Notes = `$null }")
            $lines.Add(')')
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'exports the structured Show cmdlets' {
        Import-Module $script:ModulePath -Force
        (Get-Command -Module ConsoleMonster -Name Show-ConsoleCode,Show-ConsoleError,Show-ConsoleJson,Show-ConsoleList,Show-ConsoleTable,Show-ConsoleTree).Name | Sort-Object | Should -Be @('Show-ConsoleCode', 'Show-ConsoleError', 'Show-ConsoleJson', 'Show-ConsoleList', 'Show-ConsoleTable', 'Show-ConsoleTree')
    }

    It 'formats a table with presenters and a totals row as plain text' {
        $text = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -Total -Width 100 -Property Name, Kind, @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes' }, @{ Name = 'Time'; Presenter = 'Duration' }"
        $text | Should -Match 'Size'
        $text | Should -Match '1\.41 MB'
        $text | Should -Match 'Total\s+\|\s+\|\s+4\.23 MB\s+\|\s+1 m 01 s'
        $text.IndexOf([char] 27) | Should -Be -1
    }

    It 'formats numbers with -Culture and CONSOLEMONSTER_CULTURE instead of the current culture' {
        $props = "Name, @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes' }"
        $polish = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -Culture pl-PL -Width 100 -Property $props"
        $polish | Should -Match '1,41 MB'
        $env:CONSOLEMONSTER_CULTURE = 'pl-PL'
        try {
            $fromEnvironment = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -Width 100 -Property $props"
            $explicit = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -Culture en-US -Width 100 -Property $props"
        } finally {
            $env:CONSOLEMONSTER_CULTURE = $null
        }
        $fromEnvironment | Should -Match '1,41 MB'
        $explicit | Should -Match '1\.41 MB'
    }

    It 'hides low-priority columns with a footer and switches to cards when narrow' {
        $props = "Name, Kind, @{ Name = 'Length'; Presenter = 'Bytes'; Priority = 5 }, @{ Name = 'Path'; Overflow = 'TruncateMiddle'; Priority = 0; MinWidth = 24 }"
        $hidden = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -Width 60 -CardsBelow 0 -Property $props"
        $hidden | Should -Match '\+1 column \(Path\)'
        $cards = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -Width 40 -Property $props"
        $cards | Should -Match '3 records'
        $cards | Should -Match 'Docs\.zip'
        $cards | Should -Match 'Path\s+C:\\Build'
    }

    It 'evaluates calculated properties, groups with subtotals and limits to the top rows' {
        $text = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -GroupBy Kind -Total -Top 2 -Width 90 -Property Name, @{ Name = 'KB'; Expression = { [math]::Round(`$_.Length / 1KB) }; Aggregate = 'Sum' }"
        # Groups are sorted by key: the two Module rows come first and -Top 2 shows only them.
        $text | Should -Match 'Module\D{1,8}2'
        $text | Should -Not -Match 'nupkg'
        $text | Should -Match 'Subtotal\s+\|\s+2,886'
        $text | Should -Match 'Showing 2 of 3'
    }

    It 'keeps the input order of groups with -NoSort' {
        $text = Invoke-FormatChild -Script "`$artifacts | Show-ConsoleTable -GroupBy Kind -NoSort -Total -Top 2 -Width 90 -Property Name, @{ Name = 'KB'; Expression = { [math]::Round(`$_.Length / 1KB) }; Aggregate = 'Sum' }"
        $text | Should -Match 'NuGet\D{1,8}1'
        $text | Should -Match 'Subtotal\s+\|\s+1,447'
        $text | Should -Match 'Showing 2 of 3'
    }

    It 'hides the column that repeats the group key, compared by where its values come from' {
        # The process view's ProcessName column is an alias of Name; a calculated { $_.Kind } column reads Kind.
        $text = Invoke-FormatChild -Script @'
$p = Get-Process -Id $PID
'== byname'; @($p, $p) | Show-ConsoleTable -GroupBy Name -Width 100
'== byalias'; @($p, $p) | Show-ConsoleTable -GroupBy ProcessName -Width 100
'== shown'; @($p, $p) | Show-ConsoleTable -GroupBy Name -ShowGroupColumn -Width 100
'== single'; $p | Show-ConsoleTable -GroupBy Name -Width 100
'== calculated'; $artifacts | Show-ConsoleTable -GroupBy Kind -Width 100 -Property @{ Name = 'Label'; Expression = { $_.Kind } }, Name
'@
        $parts = $text -split '== \w+'
        $parts[1] | Should -Not -Match 'ProcessName'
        $parts[1] | Should -Match '(pwsh|powershell)\D{1,4}2'
        $parts[2] | Should -Not -Match 'ProcessName'
        $parts[3] | Should -Match 'ProcessName'
        # A single row stays a table, under its group heading.
        $parts[4] | Should -Match '(pwsh|powershell)\D{1,4}1'
        $parts[4] | Should -Not -Match 'ProcessName'
        $parts[5] | Should -Not -Match 'Label'
        $parts[5] | Should -Match 'Module\D{1,4}2'
    }

    It 'shows the numbers of a column with the decimals the column needs' {
        $text = Invoke-FormatChild -Script "@([pscustomobject] @{ Name = 'a'; Cpu = 7.0 }, [pscustomobject] @{ Name = 'b'; Cpu = 11.75 }, [pscustomobject] @{ Name = 'c'; Cpu = 3 }) | Show-ConsoleTable -Width 60"
        $text | Should -Match '\|\s+7\.00 \|'
        $text | Should -Match '\|\s+11\.75 \|'
        $text | Should -Match '\|\s+3\.00 \|'
    }

    It 'wraps long text onto three lines unless -Wrap is given, and keeps short values whole' {
        $script = "`$rows = @([pscustomobject] @{ Name = 'a'; Enabled = `$true; Notes = ('word ' * 60) }, [pscustomobject] @{ Name = 'b'; Enabled = `$false; Notes = 'short' })"
        $default = Invoke-FormatChild -Script "$script; `$rows | Show-ConsoleTable -Width 50"
        $all = Invoke-FormatChild -Script "$script; `$rows | Show-ConsoleTable -Width 50 -Wrap"
        @($default -split "`n" | Where-Object { $_ -match '^\|' }).Count | Should -Be 5
        $default | Should -Match '\.\.\. \|'
        @($all -split "`n" | Where-Object { $_ -match '^\|' }).Count | Should -BeGreaterThan 5
        $default | Should -Match '\| yes\s+\|'
    }

    It 'marks status fields in Show-ConsoleList records too' {
        $text = Invoke-FormatChild -Script "[pscustomobject] @{ Name = 'a'; Status = 'Failed'; Health = 'Healthy' } | Show-ConsoleList -Width 60"
        $text | Should -Match 'Status\s+\[FAIL\] Failed'
        $text | Should -Match 'Health\s+\[PASS\] Healthy'
    }

    It 'marks status columns with PowerShell''s own words in Show-ConsoleTable' {
        $text = Invoke-FormatChild -Script "@([pscustomobject] @{ Name = 'a'; Status = 'Success' }, [pscustomobject] @{ Name = 'b'; Status = 'Warning' }, [pscustomobject] @{ Name = 'c'; Status = 'Error' }) | Show-ConsoleTable -Width 60"
        $text | Should -Match '\[PASS\] Success'
        $text | Should -Match '\[WARN\] Warning'
        $text | Should -Match '\[FAIL\] Error'
    }

    It 'uses a table or record command explicitly and puts object content in a panel' {
        $text = Invoke-FormatChild -Script @'
'== one'; [pscustomobject] @{ Name = 'alpha'; Port = 8080; Size = 3 } | Show-ConsoleTable -Width 60
'== record'; [pscustomobject] @{ Name = 'alpha'; Port = 8080; Size = 3 } | Show-ConsoleList -Width 60
'== panel'; New-ConsolePanel -Title 'Server' -Content ([pscustomobject] @{ Name = 'alpha'; Port = 8080 }) | Show-Console -Width 60
'@
        $parts = $text -split '== \w+'
        $parts[1] | Should -Match '\| Name\s+\| Port \| Size \|'
        $parts[1] | Should -Match '\| alpha \|\s+8080 \|'
        $parts[2] | Should -Match '(?m)^alpha\s*$'
        $parts[2] | Should -Match 'Port\s+8080'
        $parts[3] | Should -Match 'Server'
        $parts[3] | Should -Match 'Port\s+8080'
    }

    It 'opens one level of a wide object in Show-ConsoleTree unless -Depth is given' {
        $fixture = (Join-Path $PSScriptRoot 'Support/WideTreeFixture.cs').Replace("'", "''")
        $text = Invoke-FormatChild -Script (@'
Add-Type -Path '__FIXTURE__'
$wide = [ConsoleMonsterWideTreeFixture]::new()
"DEFAULT=$(($wide | Show-Console -AsString -View Tree).Split("`n").Count)"
"DEEP=$(($wide | Show-Console -Depth 3 -AsString -View Tree).Split("`n").Count)"
"SMALL=$(([pscustomobject] @{ a = 1; b = @{ c = @{ d = 1 } } } | Show-Console -AsString -View Tree).Split("`n").Count)"
'@.Replace('__FIXTURE__', $fixture))
        [int] ($text -replace '(?s).*DEFAULT=(\d+).*', '$1') | Should -BeLessThan 300
        [int] ($text -replace '(?s).*DEEP=(\d+).*', '$1') | Should -BeGreaterThan 300
        [int] ($text -replace '(?s).*SMALL=(\d+).*', '$1') | Should -Be 5
    }

    It 'passes objects through with -PassThru' {
        $text = Invoke-FormatChild -Script "`$out = `$artifacts | Show-ConsoleTable -Width 80 -PassThru; 'COUNT=' + @(`$out).Count"
        $text | Should -Match 'COUNT=3'
    }

    It 'respects DefaultDisplayPropertySet when no properties are given' {
        $script = @'
$o = [pscustomobject] @{ Name = 'svc1'; Status = 'Running'; Secret = 'x'; Path = 'C:\svc1.exe' }
$set = [System.Management.Automation.PSPropertySet]::new('DefaultDisplayPropertySet', [string[]] @('Name', 'Status'))
$o | Add-Member -MemberType MemberSet -Name PSStandardMembers -Value ([System.Management.Automation.PSMemberInfo[]] @($set))
@($o, $o) | Show-ConsoleTable -Width 60
'@
        $text = Invoke-FormatChild -Script $script
        $text | Should -Match 'Name\s+\|\s+Status'
        $text | Should -Not -Match 'Secret'
    }

    It 'formats a record list and hides empty values' {
        $text = Invoke-FormatChild -Script "`$artifacts[0] | Show-ConsoleList -Width 80 -Property Name, Kind, @{ Name = 'Length'; Presenter = 'Bytes' }, Notes"
        $text | Should -Match 'ConsoleMonster\.2\.0\.0\.nupkg'
        $text | Should -Match 'Length\s+1\.41 MB'
        $text | Should -Not -Match 'Notes'
    }

    It 'shows nested objects as a tree with +N more and as an inspector with a nested table' {
        $script = @'
$cert = [pscustomobject] @{
    Subject = 'CN=mail.evotec.xyz'
    San = @('a.evotec.xyz', 'b.evotec.xyz', 'c.evotec.xyz', 'd.evotec.xyz')
    Chain = @(
        [pscustomobject] @{ Subject = 'CN=mail.evotec.xyz'; Key = 'ECDSA P-256' }
        [pscustomobject] @{ Subject = 'CN=R11'; Key = 'RSA 2048' }
    )
}
$cert | Show-Console -MaxItems 2 -Title 'mail.evotec.xyz' -Width 80 -View Tree
'=====SPLIT====='
$cert | Show-ConsoleObject -StaticSnapshot -Title 'mail.evotec.xyz' -Width 80
'@
        $text = Invoke-FormatChild -Script $script
        $parts = $text -split '=====SPLIT====='
        $parts[0] | Should -Match 'Subject\s+CN=mail\.evotec\.xyz'
        $parts[0] | Should -Match '\+2 more'
        $parts[1] | Should -Match 'Chain\s+2 items'
        $parts[1] | Should -Match 'Subject\s+Key'
        $parts[1] | Should -Match 'CN=R11\s+RSA 2048'
    }

    It 'shows a hashtable by its keys and marks cycles' {
        $script = @'
$h = @{ Name = 'root'; Child = @{ A = 1 } }
$h.Self = $h
$h | Show-Console -Depth 3 -Width 80 -View Tree
'@
        $text = Invoke-FormatChild -Script $script
        $text | Should -Match 'Name\s+root'
        $text | Should -Match 'A\s+1'
        $text | Should -Match 'Self\s+\(cycle: \$\)'
        $text | Should -Not -Match 'SyncRoot'
    }

    It 'keeps the title and caption when a single dictionary is shown as a record' {
        $script = @'
@{ a = 1 } | Show-ConsoleTable -Title 'Config' -Caption 'from defaults' -Width 60 -AsString
'=====SPLIT====='
@(@{ a = 1 }, [pscustomobject] @{ Name = 'svc'; Port = 8080 }) | Show-ConsoleTable -Title 'Config' -Width 60 -AsString
'@
        $text = Invoke-FormatChild -Script $script
        $parts = $text -split '=====SPLIT====='
        # The record's heading line carries the title and, after it, the caption as its summary.
        $parts[0] | Should -Match '(?m)^\s*Config\b.*from defaults'
        $parts[0] | Should -Match 'a\s+1'
        $parts[0] | Should -Not -Match 'Hashtable'
        # The title heads only the first widget: the record, not the table after it.
        ([regex]::Matches($parts[1], 'Config')).Count | Should -Be 1
        $parts[1] | Should -Match 'svc'
    }

    It 'shows a lone dictionary without a heading unless the caller gives a title' {
        $script = @'
@{ Domain = 'evotec.xyz'; Records = 14 } | Show-ConsoleTable -Width 60 -AsString
'=====SPLIT====='
@{ Domain = 'evotec.xyz'; Records = 14 } | Show-ConsoleList -Width 60 -AsString
'=====SPLIT====='
@{ Domain = 'evotec.xyz'; Records = 14 } | Show-ConsoleTable -Title 'Mail' -Width 60 -AsString
'=====SPLIT====='
@{ Name = 'alpha'; Port = 8080 } | Show-ConsoleList -Width 60 -AsString
'@
        $text = Invoke-FormatChild -Script $script
        $parts = $text -split '=====SPLIT====='
        foreach ($part in $parts[0], $parts[1]) {
            $part | Should -Not -Match 'Hashtable'
            $part | Should -Match 'Domain\s+evotec\.xyz'
            $part | Should -Match 'Records\s+14'
        }
        $parts[2] | Should -Match '(?m)^\s*Mail\b'
        $parts[2] | Should -Match 'Domain\s+evotec\.xyz'
        # A Name key is a field like the others, not a heading.
        $parts[3] | Should -Match 'Name\s+alpha'
        $parts[3] | Should -Match 'Port\s+8080'
    }
}
