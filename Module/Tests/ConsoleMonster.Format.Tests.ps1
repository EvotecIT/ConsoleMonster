Describe 'Format-Console cmdlets' {
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

    It 'exports the Format cmdlets' {
        Import-Module $script:ModulePath -Force
        (Get-Command -Module ConsoleMonster -Name Format-Console*).Name | Sort-Object | Should -Be @('Format-ConsoleCode', 'Format-ConsoleError', 'Format-ConsoleJson', 'Format-ConsoleList', 'Format-ConsoleTable', 'Format-ConsoleTree')
    }

    It 'formats a table with presenters and a totals row as plain text' {
        $text = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -Total -Width 100 -Property Name, Kind, @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes' }, @{ Name = 'Time'; Presenter = 'Duration' }"
        $text | Should -Match 'Size'
        $text | Should -Match '1\.41 MB'
        $text | Should -Match 'Total\s+\|\s+\|\s+4\.23 MB\s+\|\s+1 m 01 s'
        $text.IndexOf([char] 27) | Should -Be -1
    }

    It 'formats numbers with -Culture and CONSOLEMONSTER_CULTURE instead of the current culture' {
        $props = "Name, @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes' }"
        $polish = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -Culture pl-PL -Width 100 -Property $props"
        $polish | Should -Match '1,41 MB'
        $env:CONSOLEMONSTER_CULTURE = 'pl-PL'
        try {
            $fromEnvironment = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -Width 100 -Property $props"
            $explicit = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -Culture en-US -Width 100 -Property $props"
        } finally {
            $env:CONSOLEMONSTER_CULTURE = $null
        }
        $fromEnvironment | Should -Match '1,41 MB'
        $explicit | Should -Match '1\.41 MB'
    }

    It 'hides low-priority columns with a footer and switches to cards when narrow' {
        $props = "Name, Kind, @{ Name = 'Length'; Presenter = 'Bytes'; Priority = 5 }, @{ Name = 'Path'; Overflow = 'TruncateMiddle'; Priority = 0; MinWidth = 24 }"
        $hidden = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -Width 60 -CardsBelow 0 -Property $props"
        $hidden | Should -Match '\+1 column \(Path\)'
        $cards = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -Width 40 -Property $props"
        $cards | Should -Match '3 records'
        $cards | Should -Match 'Docs\.zip'
        $cards | Should -Match 'Path\s+C:\\Build'
    }

    It 'evaluates calculated properties, groups with subtotals and limits to the top rows' {
        $text = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -GroupBy Kind -Total -Top 2 -Width 90 -Property Name, @{ Name = 'KB'; Expression = { [math]::Round(`$_.Length / 1KB) }; Aggregate = 'Sum' }"
        # Groups are sorted by key: the two Module rows come first and -Top 2 shows only them.
        $text | Should -Match 'Module\D{1,8}2'
        $text | Should -Not -Match 'nupkg'
        $text | Should -Match 'Subtotal\s+\|\s+2,886'
        $text | Should -Match 'Showing 2 of 3'
    }

    It 'keeps the input order of groups with -NoSort' {
        $text = Invoke-FormatChild -Script "`$artifacts | Format-ConsoleTable -GroupBy Kind -NoSort -Total -Top 2 -Width 90 -Property Name, @{ Name = 'KB'; Expression = { [math]::Round(`$_.Length / 1KB) }; Aggregate = 'Sum' }"
        $text | Should -Match 'NuGet\D{1,8}1'
        $text | Should -Match 'Subtotal\s+\|\s+1,447'
        $text | Should -Match 'Showing 2 of 3'
    }

    It 'hides the column that repeats the group key, compared by where its values come from' {
        # The process view's ProcessName column is an alias of Name; a calculated { $_.Kind } column reads Kind.
        $text = Invoke-FormatChild -Script @'
$p = Get-Process -Id $PID
'== byname'; @($p, $p) | Format-ConsoleTable -GroupBy Name -Width 100
'== byalias'; @($p, $p) | Format-ConsoleTable -GroupBy ProcessName -Width 100
'== shown'; @($p, $p) | Format-ConsoleTable -GroupBy Name -ShowGroupColumn -Width 100
'== single'; $p | Format-ConsoleTable -GroupBy Name -Width 100
'== calculated'; $artifacts | Format-ConsoleTable -GroupBy Kind -Width 100 -Property @{ Name = 'Label'; Expression = { $_.Kind } }, Name
'@
        $parts = $text -split '== \w+'
        $parts[1] | Should -Not -Match 'ProcessName'
        $parts[1] | Should -Match '(pwsh|powershell)\D{1,4}2'
        $parts[2] | Should -Not -Match 'ProcessName'
        $parts[3] | Should -Match 'ProcessName'
        # A single row is a record: the group heading stays above it.
        $parts[4] | Should -Match '(pwsh|powershell)\s+1\s*\n'
        $parts[5] | Should -Not -Match 'Label'
        $parts[5] | Should -Match 'Module\D{1,4}2'
    }

    It 'shows the numbers of a column with the decimals the column needs' {
        $text = Invoke-FormatChild -Script "@([pscustomobject] @{ Name = 'a'; Cpu = 7.0 }, [pscustomobject] @{ Name = 'b'; Cpu = 11.75 }, [pscustomobject] @{ Name = 'c'; Cpu = 3 }) | Format-ConsoleTable -Width 60"
        $text | Should -Match '\|\s+7\.00 \|'
        $text | Should -Match '\|\s+11\.75 \|'
        $text | Should -Match '\|\s+3\.00 \|'
    }

    It 'wraps long text onto three lines unless -Wrap is given, and keeps short values whole' {
        $script = "`$rows = @([pscustomobject] @{ Name = 'a'; Enabled = `$true; Notes = ('word ' * 60) }, [pscustomobject] @{ Name = 'b'; Enabled = `$false; Notes = 'short' })"
        $default = Invoke-FormatChild -Script "$script; `$rows | Format-ConsoleTable -Width 50"
        $all = Invoke-FormatChild -Script "$script; `$rows | Format-ConsoleTable -Width 50 -Wrap"
        @($default -split "`n" | Where-Object { $_ -match '^\|' }).Count | Should -Be 5
        $default | Should -Match '\.\.\. \|'
        @($all -split "`n" | Where-Object { $_ -match '^\|' }).Count | Should -BeGreaterThan 5
        $default | Should -Match '\| yes\s+\|'
    }

    It 'passes objects through with -PassThru' {
        $text = Invoke-FormatChild -Script "`$out = `$artifacts | Format-ConsoleTable -Width 80 -PassThru; 'COUNT=' + @(`$out).Count"
        $text | Should -Match 'COUNT=3'
    }

    It 'respects DefaultDisplayPropertySet when no properties are given' {
        $script = @'
$o = [pscustomobject] @{ Name = 'svc1'; Status = 'Running'; Secret = 'x'; Path = 'C:\svc1.exe' }
$set = [System.Management.Automation.PSPropertySet]::new('DefaultDisplayPropertySet', [string[]] @('Name', 'Status'))
$o | Add-Member -MemberType MemberSet -Name PSStandardMembers -Value ([System.Management.Automation.PSMemberInfo[]] @($set))
@($o, $o) | Format-ConsoleTable -Width 60
'@
        $text = Invoke-FormatChild -Script $script
        $text | Should -Match 'Name\s+\|\s+Status'
        $text | Should -Not -Match 'Secret'
    }

    It 'formats a record list and hides empty values' {
        $text = Invoke-FormatChild -Script "`$artifacts[0] | Format-ConsoleList -Width 80 -Property Name, Kind, @{ Name = 'Length'; Presenter = 'Bytes' }, Notes"
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
$cert | Format-ConsoleTree -MaxItems 2 -Title 'mail.evotec.xyz' -Width 80
'=====SPLIT====='
$cert | Format-ConsoleTree -AsInspector -Title 'mail.evotec.xyz' -Width 80
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
$h | Format-ConsoleTree -Depth 3 -Width 80
'@
        $text = Invoke-FormatChild -Script $script
        $text | Should -Match 'Name\s+root'
        $text | Should -Match 'A\s+1'
        $text | Should -Match 'Self\s+\(cycle: \$\)'
        $text | Should -Not -Match 'SyncRoot'
    }
}
