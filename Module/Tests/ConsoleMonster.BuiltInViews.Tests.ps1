Describe 'Everyday built-in view contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Convert-TestNativeView.ps1')
        $script:members = @('synthetic' | Get-Member -Name Length, Substring)
        $script:demoModule = New-Module -Name Demo.Inventory -ScriptBlock {
            function Get-DemoItem { 'synthetic' }
            function Test-DemoItem { $true }
            Set-Alias gdi Get-DemoItem
            Export-ModuleMember -Function Get-DemoItem, Test-DemoItem -Alias gdi
        }
        $script:commands = @($script:demoModule.ExportedCommands.Values)
        $script:modules = @($script:demoModule)
        $script:history = @(
            foreach ($id in 101, 102) {
                $value = [pscustomobject]@{
                    Id = $id
                    StartExecutionTime = [datetime]'2026-10-22T10:00:00'
                    EndExecutionTime = [datetime]'2026-10-22T10:00:02'
                    ExecutionStatus = if ($id -eq 101) { 'Completed' } else { 'Failed' }
                    CommandLine = 'Get-DemoItem -Name "synthetic"'
                }
                $value.PSObject.TypeNames.Insert(0, 'Microsoft.PowerShell.Commands.HistoryInfo')
                $value
            }
        )
    }

    It 'groups members and preserves complete overloaded signatures' {
        $text = $script:members | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 160
        $text | Should -Match 'Method'
        $text | Should -Match 'Property'
        $text | Should -Match 'string Substring\(int startIndex\), string Substring\(int startIndex, int length\)'
        $text | Should -Match 'int Length \{get;\}'
        $worker = [System.ComponentModel.BackgroundWorker]::new()
        try {
            $worker | Get-Member -Name DoWork | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 160 | Should -Match 'Event.*1'
        } finally {
            $worker.Dispose()
        }
    }

    It 'keeps member type captions and full-width section rows at both widths' {
        $other = @([datetime]'2026-10-22' | Get-Member -Name Day, AddDays)
        $rows = @($script:members) + $other
        foreach ($width in 60, 120) {
            $text = Use-ConsoleTarget -Width $width -Glyphs Full { $rows | Show-ConsoleTable }
            ([regex]::Matches($text, 'TypeName:')).Count | Should -Be 2
            $text | Should -Match 'TypeName: System.String'
            $text | Should -Match 'TypeName: System.DateTime'
            $groups = @($text -split "`n" | Where-Object { $_ -match '(Method|Property) · [0-9]+' })
            $groups.Count | Should -Be 4
            foreach ($group in $groups) {
                ([regex]::Matches($group, '│')).Count | Should -Be 2
                $group | Should -Not -Match '┼|─'
            }
            $text | Should -Not -Match '┼'
            $native = Convert-TestNativeView -Rows $script:members
            $nativeText = $native | Export-ConsoleFrame -Format Text -Width $width
            $nativeText | Should -Match 'TypeName: System.String'
            # Format-Table keeps the selected native view's TypeName group, while Show-ConsoleTable above uses sections.
            $native.GroupHeadersAsRows | Should -BeFalse
            $nativeText | Should -Match 'Substring'
            $nativeText | Should -Match 'Length'
        }
    }

    It 'shows small command results without module groups and keeps literal type chips' {
        $text = $script:commands | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 120
        $text | Should -Not -Match 'Demo.Inventory ·'
        $text | Should -Match '\[Function\]'
        $text | Should -Match '\[Alias\]'
        $text | Should -Match 'Get-DemoItem'
    }

    It 'shows module versions shortened paths and exported command counts' {
        $value = [psobject]::AsPSObject($script:demoModule)
        $value | Add-Member -NotePropertyName Path -NotePropertyValue 'C:\Demo\Inventory\Demo.Inventory.psm1' -Force
        $widget = $value | New-ConsoleTable
        $text = $widget | Export-ConsoleFrame -Format Text -Width 160
        $text | Should -Match 'Version'
        $text | Should -Match 'Inventory\\Demo.Inventory.psm1'
        $text | Should -Match 'Commands'
        $text | Should -Match '\s3\s'
        $widget.Columns[4].Overflow | Should -Be 'TruncateMiddle'
    }

    It 'shows history identifiers durations statuses and literal command text' {
        $text = $script:history | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 120
        $text | Should -Match '101'
        $text | Should -Match '2[.,]0 s'
        $text | Should -Match 'Completed'
        $text | Should -Match 'Failed'
        $text | Should -Match 'Get-DemoItem -Name "synthetic"'
    }

    It 'honours explicit properties and calculated expressions for every built-in family' {
        foreach ($rows in @($script:members), @($script:commands), @($script:modules), @($script:history)) {
            $script:reads = 0
            $text = $rows | New-ConsoleTable -Property @{ Label = 'Chosen'; Expression = { $script:reads++; 'literal [pass]value[/]' } }  | Export-ConsoleFrame -Format Text
            $text | Should -Match 'Chosen'
            $text | Should -Match '\[pass\]value\[/\]'
            $script:reads | Should -Be $rows.Count
            $text | Should -Not -Match 'Definition|Commands|Duration|ModuleName'
        }
    }

    It 'keeps native column labels and expressions for Table and List without adding registered columns' {
        foreach ($rows in @($script:members), @($script:commands), @($script:modules), @($script:history)) {
            foreach ($kind in 'Table', 'List') {
                $widget = Convert-TestNativeView -Rows $rows -Kind $kind
                $text = $widget | Export-ConsoleFrame -Format Text -Width 160
                $native = @($rows | & ('Microsoft.PowerShell.Utility\Format-' + $kind))
                if ($kind -eq 'Table') {
                    $shape = $native | Where-Object { $_.GetType().Name -eq 'FormatStartData' } | Select-Object -First 1
                    foreach ($column in $shape.shapeInfo.tableColumnInfoList) {
                        $label = if ($column.label) { $column.label } else { $column.propertyName }
                        $text | Should -Match ([regex]::Escape($label))
                    }
                }
                if ($rows[0] -is [Management.Automation.PSModuleInfo]) { $text | Should -Not -Match '\bVariables\b' }
                $script:reads = 0
                $explicit = Convert-TestNativeView -Rows $rows -Kind $kind -Arguments @{ Property = @(@{ Label = 'OnlyChosen'; Expression = { $script:reads++; 12.5 }; FormatString = 'F1' }) }
                $rendered = $explicit | Export-ConsoleFrame -Format Text -Width 120
                $rendered | Should -Match 'OnlyChosen'
                $rendered | Should -Match '12.5'
                $script:reads | Should -Be $rows.Count
                $rendered | Should -Not -Match '\[Function\]|MemberType|Definition|Duration|Commands'
            }
        }
    }

    It 'lets user registrations override built-ins and restores them after removal' {
        try {
            Register-ConsoleView -TypeName Microsoft.PowerShell.Commands.MemberDefinition -View Table -Property @{ Name = 'OwnerChoice'; Expression = { 'custom' } }
            $text = $script:members | New-ConsoleTable  | Export-ConsoleFrame -Format Text
            $text | Should -Match 'OwnerChoice'
            $text | Should -Not -Match 'Definition'
        } finally {
            Unregister-ConsoleView -TypeName Microsoft.PowerShell.Commands.MemberDefinition
        }
        $script:members | New-ConsoleTable  | Export-ConsoleFrame -Format Text | Should -Match 'Definition'
    }

    It 'preserves the native empty shape when an explicit wildcard matches no history properties' {
        $records = @($script:history | Microsoft.PowerShell.Utility\Format-Table N*)
        $widget = @($records | & ([Management.Automation.CmdletInfo]::new('Out-ConsoleFormatCommand', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleFormatCommand'))) -AsWidget)
        $widget.Count | Should -Be 0
    }
}

Describe 'Everyday command views' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        $script:culture = [Threading.Thread]::CurrentThread.CurrentCulture
        [Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'en-US'
        function script:New-TestCertificate([string] $Name, [int] $Days) {
            $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new("CN=$Name", [Security.Cryptography.RSA]::Create(2048),
                [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
            $request.CreateSelfSigned([datetime]::Today.AddDays(-365), [datetime]::Today.AddDays($Days))
        }
    }
    AfterAll { [Threading.Thread]::CurrentThread.CurrentCulture = $script:culture }

    It 'groups Select-String rows by file with context lines and literal highlighted matches' {
        $first = Join-Path $TestDrive 'first.txt'
        $second = Join-Path $TestDrive 'second.txt'
        [IO.File]::WriteAllLines($first, [string[]] @('intro', 'api-01 [ready]', 'outro'))
        [IO.File]::WriteAllLines($second, [string[]] @('api-02 waiting'))
        $found = @(Select-String -LiteralPath $first, $second -Pattern 'api-\d+' -Context 1)
        $text = $found | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'first\.txt'
        $text | Should -Match 'second\.txt'
        $text | Should -Match '1 │ intro'
        $text | Should -Match '2 │ api-01 \[ready\]'
        $text | Should -Match '3 │ outro'
        $html = $found[0] | New-ConsoleTable | Export-ConsoleFrame -Format Html -Width 100
        $html | Should -Match '<span class="\w+">api-01</span> \[ready\]'
        $found[0].Line | Should -BeExactly 'api-01 [ready]'
        $list = Use-ConsoleTarget -Width 100 { $found[0] | Show-Console -View List }
        $list | Should -Match 'Text\s+api-01 \[ready\]'
    }

    It 'shows an ACL owner and its access rules with allow and deny markers, readable rights and inheritance' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        $acl = [Security.AccessControl.DirectorySecurity]::new()
        $acl.SetOwner([Security.Principal.SecurityIdentifier]::new('S-1-5-32-544'))
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new('S-1-5-32-545'),
            'ReadAndExecute, Synchronize', 'ContainerInherit, ObjectInherit', 'None', 'Allow'))
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new('S-1-1-0'),
            'Delete', 'None', 'None', 'Deny'))
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new([Security.Principal.SecurityIdentifier]::new('S-1-5-11'),
            'Modify', 'ContainerInherit, ObjectInherit', 'NoPropagateInherit', 'Allow'))
        $text = $acl | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 160
        $text | Should -Match 'Owner: BUILTIN\\Administrators'
        $text | Should -Match '✓ Allow\s+│ BUILTIN\\Users\s+│ Read and execute\s+│ This folder, subfolders and files\s+│ Explicit'
        $text | Should -Match '✗ Deny\s+│ Everyone\s+│ Delete\s+│ This folder\s+│ Explicit'
        $text | Should -Match 'Authenticated Users\s+│ Modify\s+│ This folder, subfolders and files \(one level\)'
        $acl.GetType().Name | Should -Be 'DirectorySecurity'
        @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])).Count | Should -Be 3
        # An ACL without rules keeps PowerShell's own fields (from the module Get-Acl loads) instead of an empty table.
        Import-Module Microsoft.PowerShell.Security
        $empty = [Security.AccessControl.DirectorySecurity]::new()
        $empty.SetOwner([Security.Principal.SecurityIdentifier]::new('S-1-5-32-544'))
        $empty | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 120 | Should -Match 'BUILTIN\\Administrators'
    }

    It 'marks certificates as expired, expiring within 30 days or valid' {
        $certificates = @((New-TestCertificate 'old.example.test' -3), (New-TestCertificate 'soon.example.test' 9), (New-TestCertificate 'new.example.test' 120))
        try {
            $text = $certificates | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 140
            $text | Should -Match '✗ Expired\s+│ old\.example\.test.*│\s+-3 │'
            $text | Should -Match '▴ Expiring\s+│ soon\.example\.test.*│\s+9 │'
            $text | Should -Match '✓ Valid\s+│ new\.example\.test.*│\s+120 │'
            $text | Should -Match ([regex]::Escape($certificates[0].Thumbprint.Substring(0, 6)))
            # The view reads only the fields it shows: certificate script properties that need
            # Microsoft.PowerShell.Security (DnsNameList, EnhancedKeyUsageList) add nothing to $Error before it loads.
            $bytes = [Convert]::ToBase64String($certificates[0].RawData)
            $errors = & ([Diagnostics.Process]::GetCurrentProcess().Path) -NoProfile -NonInteractive -Command {
                param($Module, $Text)
                Import-Module $Module
                $Error.Clear()
                $certificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList (, [Convert]::FromBase64String($Text))
                $null = $certificate | New-ConsoleTable | Export-ConsoleFrame -Format Text
                [string] (Get-Module Microsoft.PowerShell.Security).Name + '|' + $Error.Count
            } -args (Join-Path $PSScriptRoot '../ConsoleMonster.psd1'), $bytes
            $errors | Should -Be '|0'
            # Status and days follow the pinned presentation clock, as relative dates do in recordings.
            $env:CONSOLEMONSTER_NOW = [datetime]::Today.AddDays(200).ToString('yyyy-MM-dd') + 'T12:00:00Z'
            $pinned = $certificates | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 140
            $pinned | Should -Match '✗ Expired\s+│ new\.example\.test.*│\s+-80 │'
            $pinned | Should -Not -Match 'Valid|Expiring'
        } finally {
            Remove-Item Env:\CONSOLEMONSTER_NOW -ErrorAction SilentlyContinue
            $certificates | ForEach-Object { $_.Dispose() }
        }
    }

    It 'shows group counts as bars, only the requested measurements and readable durations' {
        $groups = 'Info', 'Warning', 'Info', 'Info' | Group-Object | Sort-Object Count -Descending
        $text = $groups | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 80
        $info = ($text -split "`n" | Where-Object { $_ -match 'Info' })
        $warning = ($text -split "`n" | Where-Object { $_ -match 'Warning' })
        ([regex]::Matches($info, '▆')).Count | Should -BeGreaterThan ([regex]::Matches($warning, '▆')).Count
        $info | Should -Match '3 │'
        $measure = 12, 48, 7, 30 | Measure-Object -Sum -Maximum | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 80
        $measure | Should -Match 'Count\s+4'
        $measure | Should -Match 'Sum\s+97'
        $measure | Should -Match 'Maximum\s+48'
        $measure | Should -Not -Match 'Average|Minimum|Deviation|│'
        # A lone record keeps a long property name readable as a wrapped field, not only in its heading.
        $long = [pscustomobject]@{ ResponseTimeInMillisecondsForTheSlowestRegion = 12 } |
            Measure-Object -Property ResponseTimeInMillisecondsForTheSlowestRegion -Sum -Maximum -Minimum |
            New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 40
        ($long -replace '\s', '') | Should -Match 'PropertyResponseTimeInMillisecondsForTheSlowestRegion'
        $duration = New-TimeSpan -Hours 1 -Minutes 42 -Seconds 5 | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 80
        $duration | Should -Match '1 h 42 m'
        $duration | Should -Match '01:42:05'
    }
}
