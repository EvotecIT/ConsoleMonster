BeforeDiscovery {
    # One case per input type. Every case runs through every everyday command in one child process (see BeforeAll); the
    # assertions are the same on Windows PowerShell 5.1 and PowerShell 7.
    . (Join-Path $PSScriptRoot 'Support\TypeCases.ps1')
    $script:TypeCases = $TypeCases
}

Describe 'Every input type through every command' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $modulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $casesPath = Join-Path $PSScriptRoot 'Support\TypeCases.ps1'
        $sweepPath = Join-Path $PSScriptRoot 'Support\Invoke-TypeSweep.ps1'
        $runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:Commands = @('Show-ConsoleTable', 'Show-ConsoleList', 'Show-ConsoleTree', 'Show-ConsoleJson', 'Show-Console', 'Out-ConsoleDefault', 'Compare-ConsoleObject')
        $script = @(
            "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
            "[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
            "Import-Module '$modulePath' -Force"
            ". '$casesPath'"
            "& '$sweepPath' -TypeCases `$TypeCases"
        ) -join "`n"
        $output = Invoke-TestChild -Runtime $runtime -Script $script -TimeoutSeconds 600
        $script:Cells = @{}
        $current = $null
        foreach ($line in $output) {
            $text = "$line"
            if ($text -match '^@@CELL (.+)$') {
                $current = @{ Text = [System.Collections.Generic.List[string]]::new(); Error = $null; Ms = 0 }
                $script:Cells[$Matches[1]] = $current
            } elseif ($null -ne $current -and $text -match '^@@ERR (.*)$') {
                $current.Error = $Matches[1]
            } elseif ($null -ne $current -and $text -match '^@@MS (\d+)$') {
                $current.Ms = [int] $Matches[1]
            } elseif ($null -ne $current -and $text -ne '@@END') {
                $current.Text.Add($text)
            }
        }
    }

    It 'shows <Name> in every command without errors, .NET plumbing, type names or secrets' -ForEach $script:TypeCases {
        foreach ($command in $script:Commands) {
            $cell = $script:Cells["$Name|$command"]
            $cell | Should -Not -BeNullOrEmpty -Because "$Name through $command ran"
            $cell.Error | Should -BeNullOrEmpty -Because "$Name through $command"
            $text = $cell.Text -join "`n"
            $text | Should -Not -Match 'System\.Object\[\]|System\.Byte\[\]|SyncRoot|IsFixedSize|CimClass|CimInstanceProperties|System\.Collections\.[\w.+`]*(KeyCollection|ValueCollection|Hashtable|ArrayList|List`1|Dictionary`2)(?![\w.]*\.dll)' -Because "$Name through $command"
            $text | Should -Not -Match 'NotARealSecret' -Because "$Name through $command"
            # Completion is guarded by the child-process timeout. Fixture construction and runner scheduling are not
            # rendering contracts; keep the dedicated stalled-getter budget below rather than timing ordinary cells.
        }
    }

    It 'never reveals the length of a secure string' {
        foreach ($command in 'Show-ConsoleTable', 'Show-ConsoleList', 'Show-ConsoleTree', 'Show-ConsoleJson', 'Show-Console') {
            ($script:Cells["SecureString|$command"].Text -join "`n") | Should -Not -Match 'Length' -Because $command
        }
    }

    It 'shows dictionaries as their keys and values' {
        ($script:Cells['Hashtable|Show-ConsoleTable'].Text -join "`n") | Should -Match 'Count\s+3'
        ($script:Cells['ListOfHashtables|Show-ConsoleTable'].Text -join "`n") | Should -Match 'Name\W+Size'
        ($script:Cells['NestedDictionary|Show-ConsoleTable'].Text -join "`n") | Should -Match 'Port=8080, Tls=yes'
        ($script:Cells['NonStringKeys|Show-ConsoleJson'].Text -join "`n") | Should -Match '"Monday": "start"'
        ($script:Cells['Ordered|Show-ConsoleList'].Text -join "`n") | Should -Match '(?s)Zulu.*Alpha.*Mike'
    }

    It 'shows collections as their items and nested records by their title' {
        ($script:Cells['PSCustomObject|Show-ConsoleTable'].Text -join "`n") | Should -Match 'a, b'
        ($script:Cells['PSCustomObject|Show-ConsoleTable'].Text -join "`n") | Should -Match 'Jan \(7\)'
        ($script:Cells['ListAsOneObject|Show-ConsoleTable'].Text -join "`n") | Should -Match '(?s)1.*2.*3'
        ($script:Cells['ArrayOfArrays|Show-ConsoleTable'].Text -join "`n") | Should -Match '1, 2'
        ($script:Cells['DataRows|Show-ConsoleTable'].Text -join "`n") | Should -Not -Match 'RowState|ItemArray'
        ($script:Cells['XmlDocument|Show-ConsoleTable'].Text -join "`n") | Should -Match 'web01, web02'
    }

    It 'marks a failing getter instead of losing the record' {
        ($script:Cells['ThrowingGetter|Show-ConsoleList'].Text -join "`n") | Should -Match 'script getter failed'
        ($script:Cells['ThrowingGetter|Show-ConsoleTable'].Text -join "`n") | Should -Match 'script getter failed'
    }

    It 'stops reading getters that stall instead of waiting for all of them' {
        $cell = $script:Cells['SlowGetters|Show-ConsoleTable']
        $cell.Error | Should -BeNullOrEmpty
        $cell.Ms | Should -BeLessThan 5000
        ($cell.Text -join "`n") | Should -Match 'slow'
    }
}
