$everydayCases = foreach ($view in 'json-flat', 'json-nested', 'json-envelope', 'json-envelopes', 'json-budgets', 'json-records', 'json-empty-records', 'json-large', 'csv', 'csv-wide', 'command', 'command-grouped', 'module', 'history', 'ping', 'ip', 'dns-host') {
    foreach ($width in 96, 48) { @{ View = $view; Width = $width } }
}
$windowsNetworkCases = foreach ($view in 'dns-record', 'net-ip', 'net-adapter', 'net-connection') {
    foreach ($width in 96, 48) { @{ View = $view; Width = $width } }
}
BeforeAll {
    function Write-EverydaySetupFailure {
        param([System.Management.Automation.ErrorRecord] $SetupError)
        # Keep the original setup error visible even when Pester wraps a failed BeforeAll.
        Write-Host 'Everyday view setup failed:'
        Write-Host "ScriptStackTrace: $($setupError.ScriptStackTrace)"
        Write-Host "InvocationInfo.PositionMessage: $($setupError.InvocationInfo.PositionMessage)"
        $exception = $setupError.Exception
        $depth = 0
        while ($null -ne $exception) {
            Write-Host "Exception[$depth] type: $($exception.GetType().FullName)"
            Write-Host "Exception[$depth] message: $($exception.Message)"
            Write-Host "Exception[$depth] StackTrace: $($exception.StackTrace)"
            if ($exception -is [System.Management.Automation.RuntimeException] -and $null -ne $exception.ErrorRecord) {
                Write-Host "Exception[$depth] ScriptStackTrace: $($exception.ErrorRecord.ScriptStackTrace)"
                Write-Host "Exception[$depth] InvocationInfo.PositionMessage: $($exception.ErrorRecord.InvocationInfo.PositionMessage)"
            }
            $exception = $exception.InnerException
            $depth++
        }
    }
    try {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Get-EverydayViewSample.ps1')
        . (Join-Path $PSScriptRoot 'Support/ConvertTo-TestEverydayText.ps1')
    } catch {
        Write-EverydaySetupFailure -SetupError $_
        throw
    }
}
Describe 'Everyday structured data and discovery views' {
    BeforeAll {
        try {
            $script:SavedProfile = [ConsoleMonster.Terminal]::Profile
            $script:SavedCulture = [Threading.Thread]::CurrentThread.CurrentCulture
            $script:SavedTheme = [ConsoleMonster.Terminal]::Theme
            $english = [cultureinfo]'en-US'
            [Threading.Thread]::CurrentThread.CurrentCulture = $english
            [ConsoleMonster.Terminal]::Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(96, 200).WithCulture($english).WithLanguage($english)
            [ConsoleMonster.Terminal]::Theme = [ConsoleMonster.Styling.Theme]::Dark
        } catch {
            Write-EverydaySetupFailure -SetupError $_
            throw
        }
    }
    AfterAll {
        Disable-ConsoleFormatting
        if ($null -ne $script:SavedProfile) {
            [ConsoleMonster.Terminal]::Profile = $script:SavedProfile
            [ConsoleMonster.Terminal]::Theme = $script:SavedTheme
            [Threading.Thread]::CurrentThread.CurrentCulture = $script:SavedCulture
        }
    }

    It 'renders <View> at <Width> columns like its golden frame' -ForEach $everydayCases {
        $text = Get-EverydayViewSample $View | ConvertTo-TestEverydayText -Width $Width
        $expected = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "Golden/everyday/$View-$Width.txt"))
        $text.Replace("`r", '').TrimEnd() | Should -BeExactly $expected.Replace("`r", '').TrimEnd()
        @($text -split "`n" | Where-Object { ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.DisplayWidth'))::Measure($_) -gt $Width }).Count | Should -Be 0
    }

    It 'renders Windows <View> at <Width> columns like its golden frame' -ForEach $windowsNetworkCases -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        $text = Get-EverydayViewSample $View | ConvertTo-TestEverydayText -Width $Width
        $expected = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "Golden/everyday/windows-$View-$Width.txt"))
        $text.Replace("`r", '').TrimEnd() | Should -BeExactly $expected.Replace("`r", '').TrimEnd()
    }

    It 'keeps CSV values strings, preserves header order and aligns numeric and date text without converting it' {
        $rows = @(Get-EverydayViewSample csv)
        $text = $rows | ConvertTo-TestEverydayText -Width 96
        $text | Should -Match 'Name.*Count.*Cost.*When.*Notes'
        $text | Should -Match '0012'
        $text | Should -Match '12.50'
        $rows[0].Count | Should -BeOfType ([string])
        $rows[0].Count | Should -BeExactly '0012'
        $rows | ConvertTo-Csv -NoTypeInformation | Select-Object -First 1 | Should -BeExactly '"Name","Count","Cost","When","Notes"'
    }

    It 'bounds nested data without visiting nested getters or changing object identity' {
        $nested = [pscustomobject]@{ Name = 'api-01' }
        $nested | Add-Member ScriptProperty Dangerous { throw 'Nested getter must not run' }
        $row = [pscustomobject]@{ Name = 'payload'; Child = $nested; Array = 1..100 }
        $row | ConvertTo-TestEverydayText | Should -Match '\+94 more'
        $row | ConvertTo-TestEverydayText | Should -Match 'Name=api-01'
        [object]::ReferenceEquals($row.Child, $nested) | Should -BeTrue
        ($row | ConvertTo-TestEverydayText) | Should -Not -Match 'Dangerous|@\{'
    }

    It 'reports omitted rows and fields and retains the first columns in source order' {
        Get-EverydayViewSample json-large | ConvertTo-TestEverydayText | Should -Match '\+3 more rows'
        $row = [pscustomobject]@{}
        1..27 | ForEach-Object { $row | Add-Member NoteProperty "Field$_" $_ }
        $text = $row | ConvertTo-TestEverydayText
        $text | Should -Match '\+3 more fields'
        $text | Should -Not -Match 'Field25'
    }

    It 'lets a user view override the structured default and restores the default after removal' {
        try {
            Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Property @{ Label = 'Custom'; Expression = { 'chosen' } }
            Get-EverydayViewSample json-flat | ConvertTo-TestEverydayText | Should -Match 'Custom'
        } finally {
            Unregister-ConsoleView -TypeName System.Management.Automation.PSCustomObject
        }
        Get-EverydayViewSample json-flat | ConvertTo-TestEverydayText | Should -Match 'Count'
    }

    It 'keeps a sparse record visible when only its key has a value' {
        $row = [pscustomobject]@{ Name = 'api-01'; Owner = $null; Note = $null }
        foreach ($width in 96, 48) {
            $row | ConvertTo-TestEverydayText -Width $width | Should -Match 'api-01'
        }
    }

    It 'honours custom view properties even when its group matches the structured built-in' {
        try {
            Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Group StructuredData -Property @{ Label = 'Chosen'; Expression = 'Name' }
            $text = Get-EverydayViewSample json-large | ConvertTo-TestEverydayText -Width 96
            $text | Should -Match 'Chosen'
            $text | Should -Match 'api-53'
            $text | Should -Not -Match '\+3 more rows'
        } finally {
            Unregister-ConsoleView -TypeName System.Management.Automation.PSCustomObject
        }
    }

    It 'keeps unnamed envelope labels and chooses a name field without reordering columns' {
        $text = Get-EverydayViewSample json-envelope | ConvertTo-TestEverydayText -Width 96
        $text | Should -Match 'Total\s+3'
        $text | Should -Match 'Items'
        $text | Should -Match 'api-01'
        $text | Should -Match 'worker-01'
        $text | Should -Not -Match '3 items'
        $row = [pscustomobject]@{ Count = '0012'; Name = 'api-01'; Region = 'west' }
        $record = $row | ConvertTo-TestEverydayText -Width 48
        $record | Should -Match 'api-01'
        $record | Should -Match 'Count\s+0012'
        $row.PSObject.Properties.Name -join ',' | Should -BeExactly 'Count,Name,Region'
    }

    It 'shows small nested values and bounds deep objects and large arrays without losing omission hints' {
        $text = Get-EverydayViewSample json-nested | ConvertTo-TestEverydayText -Width 96
        $text | Should -Match 'Region=west'
        $text | Should -Match 'Count=3'
        $text | Should -Match 'blue, ready'
        $text | Should -Match 'dns'
        $text | Should -Match 'http'
        $text | Should -Match '\(none\)'
        $limited = Get-EverydayViewSample json-budgets | ConvertTo-TestEverydayText -Width 96
        $limited | Should -Match 'tag-1, tag-2'
        $limited | Should -Match '\+4 more'
        $limited | Should -Match '6 keys'
        $limited | Should -Match 'check-5'
        $limited | Should -Match '\+3 more rows'
        $limited | Should -Not -Match 'tag-7|check-6|A=1'
        [pscustomobject]@{} | ConvertTo-TestEverydayText | Should -Match '\(none\)'
    }

    It 'handles cycles and several envelope tables within one display budget without mutating them' {
        $cycle = [pscustomobject]@{ Name = 'loop' }
        $cycle | Add-Member NoteProperty Self $cycle
        $text = [pscustomobject]@{ Name = 'payload'; Cycle = $cycle } | ConvertTo-TestEverydayText
        $text | Should -Match 'keys'
        [object]::ReferenceEquals($cycle, $cycle.Self) | Should -BeTrue
        $payload = [pscustomobject]@{ Total = 36 }
        foreach ($group in 1..6) {
            $rows = @(1..6 | ForEach-Object { [pscustomobject]@{ Name = "group-$group-row-$_"; Ready = $true } })
            $payload | Add-Member NoteProperty "Group$group" $rows
        }
        $text = $payload | ConvertTo-TestEverydayText -Width 96
        $text | Should -Match 'group-5-row-4'
        $text | Should -Not -Match 'group-5-row-5|group-6-row-1'
        $text | Should -Match '6 items'
        $payload.Group6.Count | Should -Be 6
    }

    It 'reads selected columns even when a later root or nested row declares them beyond its first fields' {
        $one = [pscustomobject]@{ Name = 'one' }
        $two = [pscustomobject]@{}
        1..24 | ForEach-Object { $two | Add-Member NoteProperty "p$_" $_ }
        $two | Add-Member NoteProperty Name 'two'
        $text = @($one, $two) | ConvertTo-TestEverydayText -Width 96
        $text | Should -Match '\btwo\b'
        $text | Should -Match '\+1 more field'
        $text | Should -Not -Match '\+2 more fields'
        $nested = [pscustomobject]@{ Total = 2; Items = @($one, $two) } | ConvertTo-TestEverydayText -Width 96
        $nested | Should -Match '\btwo\b'
        $two.Name | Should -BeExactly 'two'
        $dictionary = [ordered]@{}
        1..24 | ForEach-Object { $dictionary["p$_"] = $_ }
        $dictionary['Name'] = 'dictionary-row'
        $envelope = [pscustomobject]@{ Items = @([ordered]@{ Name = 'one' }, $dictionary) }
        $envelope | ConvertTo-TestEverydayText -Width 96 | Should -Match 'dictionary-row'
    }

    It 'bounds large multiline text in expanded records and keeps the complete original value' {
        $message = 'body line' + ("`ncontinued body" * 10000)
        $payload = [pscustomobject]@{ Name = 'payload'; Message = $message; Settings = [pscustomobject]@{ Region = 'west' } }
        foreach ($width in 96, 48) {
            $text = $payload | ConvertTo-TestEverydayText -Width $width
            $text | Should -Match 'body line'
            $text | Should -Match '…'
            $text.Length | Should -BeLessThan 2000
            ($text -split "`n").Count | Should -BeLessThan 30
        }
        $payload.Message | Should -BeExactly $message
    }

    It 'shows empty root and nested record rows explicitly instead of creating a zero-column table' {
        foreach ($json in '[{},{}]', '[{}, {"Name":"api-01"}]', '{"Items":[{}]}', '{"Items":[{}, {"Name":"api-01"}]}') {
            $text = $json | ConvertFrom-Json | ConvertTo-TestEverydayText -Width 48
            $text | Should -Match '\(none\)'
            $text | Should -Not -Match 'outside the bounds|error:'
        }
    }

    It 'shows readable network labels while preserving the failure marks and omitting failure latency' {
        $text = Get-EverydayViewSample ping | ConvertTo-TestEverydayText -Width 120
        $text | Should -Match 'Timed out'
        $text | Should -Match 'Host unreachable'
        $text | Should -Not -Match 'TimedOut|DestinationHostUnreachable'
        $failed = ($text -split "`n" | Where-Object { $_ -match 'Timed out' }) -join "`n"
        $failed | Should -Not -Match '\b12\b'
        $addresses = Get-EverydayViewSample ip | ConvertTo-TestEverydayText
        $addresses | Should -Match 'IPv4'
        $addresses | Should -Match 'IPv6'
        $addresses | Should -Not -Match 'InterNetwork'
    }

    It 'puts a repeated module name only in its group header and preserves user and system path origins' {
        $text = Get-EverydayViewSample module | ConvertTo-TestEverydayText -Width 160
        $text | Should -Not -Match 'Name\s+\S\s+Version'
        $text | Should -Match 'Demo.Inventory'
        $text | Should -Match 'System /opt/'
        $text | Should -Match 'User ~/'
        $text | Should -Match '2.1.0'
        $module = [pscustomobject]@{ Name = 'Local.Tools'; Version = [version]'1.0.0'; ModuleType = 'Script'; ExportedCommands = @{}; Path = (Join-Path $HOME 'Documents/PowerShell/Modules/Local.Tools/Local.Tools.psm1') }
        $module.PSObject.TypeNames.Insert(0, 'System.Management.Automation.PSModuleInfo')
        $module | ConvertTo-TestEverydayText -Width 160 | Should -Match 'User ~[/\\]Documents'
    }

    It 'keeps capture, Select-Object, Export-Csv and native format records unchanged when enabled' {
        $rows = @(Get-EverydayViewSample csv)
        $native = $rows | Out-String -Width 96
        try {
            Enable-ConsoleFormatting -Feature Output
            $captured = @($rows | Select-Object -First 2)
            [object]::ReferenceEquals($rows[0], $captured[0]) | Should -BeTrue
            $rows | Select-Object Name, Count | Export-Csv (Join-Path $TestDrive 'rows.csv') -NoTypeInformation
            $imported = Import-Csv (Join-Path $TestDrive 'rows.csv')
            $imported[0].Count | Should -BeExactly '0012'
            ($rows | Out-String -Width 96) | Should -BeExactly $native
            @($rows | Microsoft.PowerShell.Utility\Format-Table)[0].GetType().Name | Should -Be 'FormatStartData'
        } finally { Disable-ConsoleFormatting }
    }

    It 'uses actual command and module objects through their registrations' {
        $command = Get-Command Get-Date | ConvertTo-TestEverydayText
        $command | Should -Match '\[Cmdlet\]'
        $command | Should -Match 'Get-Date'
        Get-Module Microsoft.PowerShell.Utility | ConvertTo-TestEverydayText | Should -Match 'Commands'
    }

    It 'reads only fields supplied by DNS and ICMP result variants under strict mode' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        $dns = [pscustomobject]@{ Name = 'example.test'; Type = 'MX'; TTL = 300; Preference = 10; NameExchange = 'mail.example.test' }
        $dns.PSObject.TypeNames.Insert(0, 'Microsoft.DnsClient.Commands.DnsRecord_MX')
        $connection = [pscustomobject]@{ ComputerName = 'api.example.test'; RemoteAddress = '192.0.2.10'; InterfaceAlias = 'Ethernet'; PingSucceeded = $true }
        $connection.PSObject.TypeNames.Insert(0, 'NetConnectionResults')
        $text = & {
            Set-StrictMode -Version Latest
            $dns | ConvertTo-TestEverydayText -Width 96
            $connection | ConvertTo-TestEverydayText -Width 96
        }
        ($text -join "`n") | Should -Match '10 mail.example.test'
        ($text -join "`n") | Should -Match 'Success'
        ($text -join "`n") | Should -Not -Match 'cannot be found|error:'
    }

    It 'does not register Windows network views on Unix' -Skip:([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        $text = Get-EverydayViewSample net-ip | Show-ConsoleTable -AsString
        $text | Should -Match 'PrefixLength'
        $text | Should -Not -Match '(?m)^\s*Prefix\s'
    }
}
