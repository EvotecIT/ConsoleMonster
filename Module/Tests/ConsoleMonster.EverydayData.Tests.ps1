$everydayCases = foreach ($view in 'json-flat', 'json-nested', 'json-envelope', 'json-large', 'csv', 'csv-wide', 'command', 'command-grouped', 'module', 'history', 'ping', 'ip', 'dns-host') {
    foreach ($width in 96, 48) { @{ View = $view; Width = $width } }
}
$windowsNetworkCases = foreach ($view in 'dns-record', 'net-ip', 'net-adapter', 'net-connection') {
    foreach ($width in 96, 48) { @{ View = $view; Width = $width } }
}
Describe 'Everyday structured data and discovery views' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Get-EverydayViewSample.ps1')
        $script:SavedProfile = [ConsoleMonster.Terminal]::Profile
        $script:SavedCulture = [Threading.Thread]::CurrentThread.CurrentCulture
        $script:SavedTheme = [ConsoleMonster.Terminal]::Theme
        $english = [cultureinfo]'en-US'
        [Threading.Thread]::CurrentThread.CurrentCulture = $english
        [ConsoleMonster.Terminal]::Profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed(96, 200).WithCulture($english).WithLanguage($english)
        [ConsoleMonster.Terminal]::Theme = [ConsoleMonster.Styling.Theme]::Dark
    }
    AfterAll {
        Disable-ConsoleFormatting
        [ConsoleMonster.Terminal]::Profile = $script:SavedProfile
        [ConsoleMonster.Terminal]::Theme = $script:SavedTheme
        [Threading.Thread]::CurrentThread.CurrentCulture = $script:SavedCulture
    }

    It 'renders <View> at <Width> columns like its golden frame' -ForEach $everydayCases {
        $text = Get-EverydayViewSample $View | Out-ConsoleDefault -As Text -Width $Width
        $expected = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "Golden/everyday/$View-$Width.txt"))
        $text.Replace("`r", '').TrimEnd() | Should -BeExactly $expected.Replace("`r", '').TrimEnd()
        @($text -split "`n" | Where-Object { [ConsoleMonster.Text.DisplayWidth]::Measure($_) -gt $Width }).Count | Should -Be 0
    }

    It 'renders Windows <View> at <Width> columns like its golden frame' -ForEach $windowsNetworkCases -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        $text = Get-EverydayViewSample $View | Out-ConsoleDefault -As Text -Width $Width
        $expected = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "Golden/everyday/windows-$View-$Width.txt"))
        $text.Replace("`r", '').TrimEnd() | Should -BeExactly $expected.Replace("`r", '').TrimEnd()
    }

    It 'keeps CSV values strings, preserves header order and aligns numeric and date text without converting it' {
        $rows = @(Get-EverydayViewSample csv)
        $text = $rows | Out-ConsoleDefault -As Text -Width 96
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
        $row | Out-ConsoleDefault -As Text | Should -Match '100 items'
        [object]::ReferenceEquals($row.Child, $nested) | Should -BeTrue
        ($row | Out-ConsoleDefault -As Text) | Should -Not -Match 'Dangerous|@\{'
    }

    It 'reports omitted rows and fields and retains the first columns in source order' {
        Get-EverydayViewSample json-large | Out-ConsoleDefault -As Text | Should -Match '\+3 more rows'
        $row = [pscustomobject]@{}
        1..27 | ForEach-Object { $row | Add-Member NoteProperty "Field$_" $_ }
        $text = $row | Out-ConsoleDefault -As Text
        $text | Should -Match '\+3 more fields'
        $text | Should -Not -Match 'Field25'
    }

    It 'lets a user view override the structured default and restores the default after removal' {
        try {
            Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Property @{ Label = 'Custom'; Expression = { 'chosen' } }
            Get-EverydayViewSample json-flat | Out-ConsoleDefault -As Text | Should -Match 'Custom'
        } finally {
            Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Remove
        }
        Get-EverydayViewSample json-flat | Out-ConsoleDefault -As Text | Should -Match 'Count'
    }

    It 'keeps a sparse record visible when only its key has a value' {
        $row = [pscustomobject]@{ Name = 'api-01'; Owner = $null; Note = $null }
        foreach ($width in 96, 48) {
            $row | Out-ConsoleDefault -As Text -Width $width | Should -Match 'api-01'
        }
    }

    It 'honours custom view properties even when its group matches the structured built-in' {
        try {
            Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Group StructuredData -Property @{ Label = 'Chosen'; Expression = 'Name' }
            $text = Get-EverydayViewSample json-large | Out-ConsoleDefault -As Text -Width 96
            $text | Should -Match 'Chosen'
            $text | Should -Match 'api-53'
            $text | Should -Not -Match '\+3 more rows'
        } finally {
            Register-ConsoleView -TypeName System.Management.Automation.PSCustomObject -Remove
        }
    }

    It 'keeps unnamed envelope labels and chooses a name field without reordering columns' {
        $text = Get-EverydayViewSample json-envelope | Out-ConsoleDefault -As Text -Width 96
        $text | Should -Match 'Total\s+3'
        $text | Should -Match 'Items\s+3 items'
        $row = [pscustomobject]@{ Count = '0012'; Name = 'api-01'; Region = 'west' }
        $record = $row | Out-ConsoleDefault -As Text -Width 48
        $record | Should -Match 'api-01'
        $record | Should -Match 'Count\s+0012'
        $row.PSObject.Properties.Name -join ',' | Should -BeExactly 'Count,Name,Region'
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
        $command = Get-Command Get-Date | Out-ConsoleDefault -As Text
        $command | Should -Match '\[Cmdlet\]'
        $command | Should -Match 'Get-Date'
        Get-Module Microsoft.PowerShell.Utility | Out-ConsoleDefault -As Text | Should -Match 'Commands'
    }

    It 'reads only fields supplied by DNS and ICMP result variants under strict mode' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        $dns = [pscustomobject]@{ Name = 'example.test'; Type = 'MX'; TTL = 300; Preference = 10; NameExchange = 'mail.example.test' }
        $dns.PSObject.TypeNames.Insert(0, 'Microsoft.DnsClient.Commands.DnsRecord_MX')
        $connection = [pscustomobject]@{ ComputerName = 'api.example.test'; RemoteAddress = '192.0.2.10'; InterfaceAlias = 'Ethernet'; PingSucceeded = $true }
        $connection.PSObject.TypeNames.Insert(0, 'NetConnectionResults')
        $text = & {
            Set-StrictMode -Version Latest
            $dns | Out-ConsoleDefault -As Text -Width 96
            $connection | Out-ConsoleDefault -As Text -Width 96
        }
        ($text -join "`n") | Should -Match '10 mail.example.test'
        ($text -join "`n") | Should -Match 'Success'
        ($text -join "`n") | Should -Not -Match 'cannot be found|error:'
    }

    It 'does not register Windows network views on Unix' -Skip:([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        $text = Get-EverydayViewSample net-ip | Out-ConsoleDefault -As Text
        $text | Should -Match 'PrefixLength'
        $text | Should -Not -Match '(?m)^\s*Prefix\s'
    }
}
