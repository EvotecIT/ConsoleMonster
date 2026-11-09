Describe 'Console view drafts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        $script:Rows = @(
            [pscustomobject][ordered]@{
                PSTypeName = 'Contoso.Backup'; Server = 'alpha'; State = 'Completed'
                Size = 1024L; Took = [timespan]::FromSeconds(5); Empty = $null
                PSComputerName = 'remote'; RunspaceId = 'id'; PSShowComputerName = $true
            }
            [pscustomobject][ordered]@{
                PSTypeName = 'Contoso.Backup'; Server = 'bravo'; State = 'Failed'
                Size = 32768L; Took = [timespan]::FromSeconds(10); Empty = ''
                PSComputerName = 'remote'; RunspaceId = 'id'; PSShowComputerName = $true
            }
        )
    }

    AfterEach {
        Unregister-ConsoleView -TypeName 'Contoso.Backup', 'Contoso.Preferred', 'Contoso.SampleLimit', 'Contoso.Values', 'Selected.System.Management.Automation.PSCustomObject' -Confirm:$false
    }

    It 'writes a pasteable script with shared presenters and an optional status summary' {
        $draft = $script:Rows | ConvertTo-ConsoleView
        $draft | Should -BeExactly @'
# Type: Contoso.Backup
# Keep this script in your profile, or use -AsDataFile for ModuleName.consoleviews.psd1.
$column1 = @{
    Name = 'Server'
    Expression = 'Server'
}
$column2 = @{
    Name = 'State'
    Expression = 'State'
    Presenter = 'StatusWord'
}
$column3 = @{
    Name = 'Size'
    Expression = 'Size'
    Presenter = 'BytesBar'
}
$column4 = @{
    Name = 'Took'
    Expression = 'Took'
    Presenter = 'Duration'
}

Register-ConsoleView `
    -TypeName 'Contoso.Backup' `
    -Property $column1, $column2, $column3, $column4

# To add the summary, append this block to the registration above with a backtick.
# -Summary {
#     param($items)
#     @{ Value = @($items).Count; Label = 'items' }
#     $statusProperty = 'State'
#     $items | Group-Object -Property $statusProperty | ForEach-Object {
#         @{ Value = $_.Count; Label = $_.Name }
#     }
# }
'@
        & ([scriptblock]::Create($draft))
        $text = $script:Rows | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'Server.*State.*Size.*Took'
        $text | Should -Match 'alpha'
        $text | Should -Match 'bravo'
        $text | Should -Not -Match 'PSComputerName|RunspaceId|PSShowComputerName|Empty'
        @($draft -split "`n" | Where-Object Length -GE 100).Count | Should -Be 0
        # The suggested block also runs when appended, including the count for each current status.
        $parts = $draft -split '# To add the summary,', 2
        $summary = (($parts[1] -split "`n" | Select-Object -Skip 1) -replace '^# ?', '') -join "`n"
        & ([scriptblock]::Create($parts[0].TrimEnd() + ' ' + $summary))
    }

    It 'imports the data-only draft through the public view-file command' {
        $path = Join-Path $TestDrive 'Backups.consoleviews.psd1'
        $data = $script:Rows | ConvertTo-ConsoleView -AsDataFile
        $data | Set-Content -LiteralPath $path -Encoding UTF8
        $view = (Import-PowerShellDataFile -LiteralPath $path).Views[0]
        $view.TypeName | Should -BeExactly 'Contoso.Backup'
        @($view.Property.Name) -join ',' | Should -BeExactly 'Server,State,Size,Took'
        @($view.Property.Expression | Where-Object { $_ -is [scriptblock] }).Count | Should -Be 0
        Register-ConsoleView -Path $path
        $text = $script:Rows | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'Server.*State.*Size.*Took'
        $text | Should -Match 'bravo'
        $text | Should -Not -Match 'PSComputerName|Empty'
    }

    It 'registers the same columns immediately without executing its suggested summary' {
        $draft = $script:Rows | ConvertTo-ConsoleView -Register
        $draft | Should -BeExactly ($script:Rows | ConvertTo-ConsoleView)
        $text = $script:Rows | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'Server.*State.*Size.*Took'
        $text | Should -Not -Match 'Empty|PSComputerName'
    }

    It 'warns for <Prefix> input and drafts for the intended type' -ForEach @(
        @{ Prefix = 'Deserialized.'; TypeName = 'Contoso.Backup'; Message = 'will not apply to job or remote output' }
        @{ Prefix = 'Selected.'; TypeName = 'Selected.System.Management.Automation.PSCustomObject'; Message = 'selected shape, not the original type' }
    ) {
        $original = [pscustomobject]@{ PSTypeName = 'Contoso.Backup'; Name = 'alpha' }
        $row = if ($Prefix -eq 'Deserialized.') {
            [Management.Automation.PSSerializer]::Deserialize([Management.Automation.PSSerializer]::Serialize($original))
        } else { $original | Select-Object Name }
        $draft = $row | ConvertTo-ConsoleView -WarningVariable warnings -WarningAction SilentlyContinue
        @($warnings).Count | Should -Be 1
        $warnings[0].Message | Should -Match ([regex]::Escape($Message))
        $draft | Should -Match ([regex]::Escape("-TypeName '$TypeName'"))
        & ([scriptblock]::Create($draft))
        if ($Prefix -eq 'Deserialized.') {
            $row.PSObject.TypeNames[0] | Should -BeExactly 'Deserialized.Contoso.Backup'
        }
    }

    It 'keeps default display order, drops empty members and caps the draft at six columns' {
        $row = [pscustomobject][ordered]@{
            PSTypeName = 'Contoso.Preferred'; Noise = 'hidden'; Name = 'alpha'
            A = 1; B = 2; C = 3; D = 4; E = 5; F = 6; Empty = [DBNull]::Value
        }
        $display = [Management.Automation.PSPropertySet]::new('DefaultDisplayPropertySet', [string[]]@('Empty', 'B', 'A', 'Name', 'C', 'D', 'E', 'F'))
        $row | Add-Member -MemberType MemberSet -Name PSStandardMembers -Value ([Management.Automation.PSMemberInfo[]]@($display))
        $path = Join-Path $TestDrive 'Preferred.consoleviews.psd1'
        $row | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path
        $names = (Import-PowerShellDataFile $path).Views[0].Property.Name
        $names -join ',' | Should -BeExactly 'B,A,Name,C,D,E'
    }

    It 'samples only the first fifty rows and prioritizes the identity before taking six columns' {
        $rows = @(1..51 | ForEach-Object {
            [pscustomobject][ordered]@{
                PSTypeName = 'Contoso.SampleLimit'; First = 1; Empty = $(if ($_ -eq 51) { 'late' })
                A = 2; B = 3; C = 4; D = 5; E = 6; Name = 'alpha'
                Size = $(if ($_ -eq 51) { 1000000L } else { 1024L })
            }
        })
        $path = Join-Path $TestDrive 'Bounded.consoleviews.psd1'
        $rows | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path
        (Import-PowerShellDataFile $path).Views[0].Property.Name -join ',' | Should -BeExactly 'Name,First,A,B,C,D'
        $rows | Select-Object Name, Size | ConvertTo-ConsoleView -WarningAction SilentlyContinue |
            Should -Not -Match 'BytesBar'
    }

    It 'does not restore excluded metadata when no draft columns remain' {
        $row = [pscustomobject]@{ PSTypeName = 'Contoso.Metadata'; PSComputerName = 'host'; RunspaceId = 'id'; PSShowComputerName = $true }
        $draft = $row | ConvertTo-ConsoleView -WarningVariable warnings -WarningAction SilentlyContinue
        $draft | Should -BeNullOrEmpty
        @($warnings).Count | Should -Be 1
        $warnings[0].Message | Should -Match 'No nonempty properties'
    }

    It 'uses shared type inference and refines statuses, byte ranges and recent dates' {
        $rows = @(
            [pscustomobject]@{ PSTypeName = 'Contoso.Values'; Id = 1234; Ready = $true; Size = 10L; Modified = [datetime]::UtcNow.AddDays(-1); Decision = 'Passed'; Took = [timespan]::FromSeconds(2) }
            [pscustomobject]@{ PSTypeName = 'Contoso.Values'; Id = 1235; Ready = $false; Size = 100L; Modified = [datetime]::UtcNow.AddDays(-2); Decision = 'Warning'; Took = [timespan]::FromSeconds(3) }
        )
        $path = Join-Path $TestDrive 'Values.consoleviews.psd1'
        $rows | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path
        $columns = (Import-PowerShellDataFile $path).Views[0].Property
        $columns.Presenter -join ',' | Should -BeExactly 'Identifier,Boolean,Bytes,DateRelative,StatusWord,Duration'
        $rows[1].Size = 101L
        $rows[1].Modified = [datetime]::UtcNow.AddDays(-31)
        $rows[1].Decision = 'ordinary text'
        $rows | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path
        $columns = (Import-PowerShellDataFile $path).Views[0].Property
        ($columns | Where-Object Name -EQ Size).Presenter | Should -BeExactly 'BytesBar'
        ($columns | Where-Object Name -EQ Modified).Presenter | Should -BeExactly 'Date'
        ($columns | Where-Object Name -EQ Decision).ContainsKey('Presenter') | Should -BeFalse
    }

    It 'preserves <Kind> literal names as data in scripts and data files' -ForEach @(
        @{ Kind = 'ASCII'; Type = "Contoso.O'Brien.`nType"; Member = "odd'[*]`$`nfield" }
        @{ Kind = 'smart single quote'; Type = 'Contoso.' + (-join (0x2018..0x201b | ForEach-Object { [char]$_ })); Member = 'Owner' + [char]0x2019 + "; throw 'draft injection'; #" }
        @{ Kind = 'smart double quote'; Type = 'Contoso.Double'; Member = (-join (0x201c..0x201e | ForEach-Object { [char]$_ })) + "`n`$(throw 'draft injection')" }
    ) {
        $type = $Type
        $name = $Member
        $row = [pscustomobject][ordered]@{ PSTypeName = $type; $name = 'literal-value' }
        try {
            $script = $row | ConvertTo-ConsoleView
            @($script -split "`n" | Where-Object Length -GE 100).Count | Should -Be 0
            & ([scriptblock]::Create($script))
            $text = $row | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
            $text | Should -Match 'literal-value'
            $path = Join-Path $TestDrive 'Literal.consoleviews.psd1'
            $data = $row | ConvertTo-ConsoleView -AsDataFile
            @($data -split "`n" | Where-Object Length -GE 100).Count | Should -Be 0
            $data | Set-Content -LiteralPath $path
            $view = (Import-PowerShellDataFile -LiteralPath $path).Views[0]
            $view.TypeName | Should -BeExactly $type
            $view.Property[0].Expression | Should -BeExactly $name
            Register-ConsoleView -Path $path
        } finally { Unregister-ConsoleView -TypeName $type -Confirm:$false }
    }

    It 'wraps long script literals and warns when data-file literals cannot fit' {
        $type = 'Contoso.' + ('LongType' * 20)
        $name = 'LongProperty' * 12
        $row = [pscustomobject][ordered]@{ PSTypeName = $type; $name = 'literal-value' }
        try {
            $draft = $row | ConvertTo-ConsoleView
            @($draft -split "`n" | Where-Object Length -GE 100).Count | Should -Be 0
            & ([scriptblock]::Create($draft))
            $row | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100 |
                Should -Match 'literal-value'
            $data = $row | ConvertTo-ConsoleView -AsDataFile -WarningVariable warnings -WarningAction SilentlyContinue
            $data | Should -BeNullOrEmpty
            @($warnings).Count | Should -Be 1
            $warnings[0].Message | Should -Match 'omit -AsDataFile'
        } finally { Unregister-ConsoleView -TypeName $type -Confirm:$false }
    }
}
