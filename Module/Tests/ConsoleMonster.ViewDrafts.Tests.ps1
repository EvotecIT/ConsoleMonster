Describe 'Console view drafts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        $assembly = (Get-Command Show-Console).ImplementingType.Assembly
        $script:SinkType = $assembly.GetType('ConsoleMonster.PowerShell.ConsoleDefaultOutput')
        $script:SinkOptionsType = $assembly.GetType('ConsoleMonster.PowerShell.ConsoleDefaultOutputOptions')
        function Out-DraftedView {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline)] $InputObject)
            begin {
                $options = $script:SinkOptionsType::new()
                $options.Force = $true
                $output = $script:SinkType::new($PSCmdlet, $options)
            }
            process { $output.Write([psobject] $InputObject) }
            end { try { $output.Complete() } finally { $output.Dispose() } }
        }
        function Get-DraftNames($Columns) {
            @($Columns | ForEach-Object { if ($_ -is [string]) { $_ } else { $_.Name } }) -join ','
        }
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

    It 'writes compact named columns and a working default summary' {
        $draft = $script:Rows | ConvertTo-ConsoleView
        $draft | Should -BeExactly @'
# Contoso.Backup: keep these lines in your profile; ConvertTo-ConsoleView -AsDataFile makes the module form.
$state = @{ Name = 'State'; Expression = 'State'; Presenter = 'StatusWord' }
$size = @{ Name = 'Size'; Expression = 'Size'; Presenter = 'BytesBar' }
$took = @{ Name = 'Took'; Expression = 'Took'; Presenter = 'Duration' }

$summary = {
    param($items)
    @{ Value = @($items).Count; Label = 'backups' }
    $roles = @{
        failed = 'Fail'; error = 'Fail'; stopped = 'Fail'
        warning = 'Warn'; degraded = 'Warn'
        completed = 'Pass'; passed = 'Pass'; success = 'Pass'; ok = 'Pass'
    }
    $items | Group-Object -Property 'State' | ForEach-Object {
        $label = $_.Name.ToLowerInvariant()
        $fact = @{ Value = $_.Count; Label = $label }
        if ($roles.ContainsKey($label)) { $fact.Role = $roles[$label] }
        $fact
    }
}
# Drop -Summary $summary to leave the line out.
$view = @{
    TypeName = 'Contoso.Backup'
    Property = @('Server', $state, $size, $took)
    Summary = $summary
}
Register-ConsoleView @view
'@
        & ([scriptblock]::Create($draft))
        $text = Use-ConsoleTarget -Width 100 { $script:Rows | Out-DraftedView }
        $text | Should -Match 'Server.*State.*Size.*Took'
        $text | Should -Match 'alpha'
        $text | Should -Match 'bravo'
        $text | Should -Match '2 backups.*1 completed.*1 failed'
        $text | Should -Not -Match 'PSComputerName|RunspaceId|PSShowComputerName|Empty'
        @($draft -split "`n" | Select-Object -Skip 1 | Where-Object Length -GT 100).Count | Should -Be 0
    }

    It 'keeps a short registration on one line without backtick continuations' {
        $row = [pscustomobject]@{ PSTypeName = 'Contoso.Backup'; Server = 'alpha' }
        $draft = $row | ConvertTo-ConsoleView
        $draft | Should -BeExactly @'
# Contoso.Backup: keep these lines in your profile; ConvertTo-ConsoleView -AsDataFile makes the module form.

$summary = {
    param($items)
    @{ Value = @($items).Count; Label = 'backups' }
}
# Drop -Summary $summary to leave the line out.
Register-ConsoleView -TypeName Contoso.Backup -Property Server -Summary $summary
'@
        & ([scriptblock]::Create($draft))
        Use-ConsoleTarget -Width 100 { $row | Out-DraftedView } | Should -Match '1 backups'
    }

    It 'imports the data-only draft through the public view-file command' {
        $path = Join-Path $TestDrive 'Backups.consoleviews.psd1'
        $data = $script:Rows | ConvertTo-ConsoleView -AsDataFile
        $data | Should -BeExactly @'
# Contoso.Backup: keep as ModuleName.consoleviews.psd1 beside the module manifest.
@{
    SchemaVersion = 1
    Views = @(
        @{
            TypeName = 'Contoso.Backup'
            Property = @(
                'Server'
                @{ Name = 'State'; Expression = 'State'; Presenter = 'StatusWord' }
                @{ Name = 'Size'; Expression = 'Size'; Presenter = 'BytesBar' }
                @{ Name = 'Took'; Expression = 'Took'; Presenter = 'Duration' }
            )
        }
    )
}
'@
        $data | Set-Content -LiteralPath $path -Encoding UTF8
        $view = (Import-PowerShellDataFile -LiteralPath $path).Views[0]
        $view.TypeName | Should -BeExactly 'Contoso.Backup'
        Get-DraftNames $view.Property | Should -BeExactly 'Server,State,Size,Took'
        @($view.Property.Expression | Where-Object { $_ -is [scriptblock] }).Count | Should -Be 0
        Register-ConsoleView -Path $path
        $text = $script:Rows | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'Server.*State.*Size.*Took'
        $text | Should -Match 'bravo'
        $text | Should -Not -Match 'PSComputerName|Empty'
    }

    It 'registers the same columns and default summary immediately' {
        $draft = $script:Rows | ConvertTo-ConsoleView -Register
        $draft | Should -BeExactly ($script:Rows | ConvertTo-ConsoleView)
        $text = Use-ConsoleTarget -Width 100 { $script:Rows | Out-DraftedView }
        $text | Should -Match 'Server.*State.*Size.*Took'
        $text | Should -Not -Match 'Empty|PSComputerName'
        $text | Should -Match '2 backups.*1 completed.*1 failed'
    }

    It 'uses lowercase labels and requested summary roles for <StatusProperty>' -ForEach @(
        @{ StatusProperty = 'State' }
        @{ StatusProperty = 'State*' }
        @{ StatusProperty = 'State[0]' }
    ) {
        $expected = @{
            completed = 'Pass'; passed = 'Pass'; success = 'Pass'; ok = 'Pass'
            failed = 'Fail'; error = 'Fail'; stopped = 'Fail'
            warning = 'Warn'; degraded = 'Warn'; running = $null
        }
        $rows = @($expected.Keys | ForEach-Object {
            [pscustomobject]@{ PSTypeName = 'Contoso.Backup'; Server = 'alpha'; $StatusProperty = $_.ToUpperInvariant(); StateOther = 'detail'; State0 = 'Failed' }
        })
        . ([scriptblock]::Create(($rows | ConvertTo-ConsoleView)))
        $facts = @(& $summary $rows)
        $facts[0].Value | Should -Be 10
        $facts[0].Label | Should -BeExactly 'backups'
        $facts.Count | Should -Be 11
        foreach ($fact in $facts | Select-Object -Skip 1) {
            $expected.Keys | Should -Contain $fact.Label
            $fact.Label | Should -BeExactly $fact.Label.ToLowerInvariant()
            $fact.Role | Should -BeExactly $expected[$fact.Label]
        }
    }

    It 'drafts the Get-Process default set with Name, Id and CPU before ancillary counters' {
        try {
            $path = Join-Path $TestDrive 'Processes.consoleviews.psd1'
            Get-Process -Id $PID | ConvertTo-ConsoleView -AsDataFile | Set-Content $path -Encoding UTF8
            Get-DraftNames (Import-PowerShellDataFile $path).Views[0].Property |
                Should -BeExactly 'Name,Id,CPU,Handles,SI'
            $draft = Get-Process -Id $PID | ConvertTo-ConsoleView
            . ([scriptblock]::Create($draft))
            @(& $summary @(Get-Process -Id $PID))[0].Label | Should -BeExactly 'processes'
        } finally { Unregister-ConsoleView -TypeName System.Diagnostics.Process -Confirm:$false }
    }

    It 'uses items for an unrecognizable type-name noun' {
        $type = 'Contoso.Backup`1'
        $row = [pscustomobject]@{ PSTypeName = $type; Name = 'alpha' }
        try {
            . ([scriptblock]::Create(($row | ConvertTo-ConsoleView)))
            @(& $summary @($row))[0].Label | Should -BeExactly 'items'
        } finally { Unregister-ConsoleView -TypeName $type -Confirm:$false }
    }

    It 'keeps presenter variables distinct from each other and automatic <Automatic> variables' -ForEach @(
        @{ Automatic = 'IsWindows' }
        @{ Automatic = 'IsLinux' }
        @{ Automatic = 'IsMacOS' }
        @{ Automatic = 'IsCoreCLR' }
        @{ Automatic = 'PSStyle' }
        @{ Automatic = 'EnabledExperimentalFeatures' }
        @{ Automatic = 'ErrorActionPreference' }
    ) {
        $row = [pscustomobject][ordered]@{
            PSTypeName = 'Contoso.Backup'; Server = 'alpha'; PID = 1234
            'State Word' = 'Running'; StateWord = 'Completed'; Summary = 'Passed'
            $Automatic = $true
        }
        $before = $PID
        $draft = $row | ConvertTo-ConsoleView
        & ([scriptblock]::Create($draft))
        $PID | Should -Be $before
        $row | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100 |
            Should -Match '(?s)alpha.*1234.*Running.*Completed.*Passed'
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
        $draft | Should -Match ("(?:-TypeName\s+|TypeName = ')" + [regex]::Escape($TypeName))
        & ([scriptblock]::Create($draft))
        if ($Prefix -eq 'Deserialized.') {
            $row.PSObject.TypeNames[0] | Should -BeExactly 'Deserialized.Contoso.Backup'
        }
    }

    It 'puts the name first, preserves remaining default display order and caps six nonempty columns' {
        $row = [pscustomobject][ordered]@{
            PSTypeName = 'Contoso.Preferred'; Noise = 'hidden'; Name = 'alpha'
            A = 1; B = 2; C = 3; D = 4; E = 5; F = 6; Empty = [DBNull]::Value
        }
        $display = [Management.Automation.PSPropertySet]::new('DefaultDisplayPropertySet', [string[]]@('Empty', 'B', 'A', 'Name', 'C', 'D', 'E', 'F'))
        $row | Add-Member -MemberType MemberSet -Name PSStandardMembers -Value ([Management.Automation.PSMemberInfo[]]@($display))
        $path = Join-Path $TestDrive 'Preferred.consoleviews.psd1'
        $row | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path -Encoding UTF8
        $names = Get-DraftNames (Import-PowerShellDataFile $path).Views[0].Property
        $names | Should -BeExactly 'Name,B,A,C,D,E'
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
        $rows | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path -Encoding UTF8
        Get-DraftNames (Import-PowerShellDataFile $path).Views[0].Property | Should -BeExactly 'Name,First,A,B,C,D'
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
        $rows | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path -Encoding UTF8
        $columns = (Import-PowerShellDataFile $path).Views[0].Property
        $columns.Presenter -join ',' | Should -BeExactly 'Identifier,Boolean,Bytes,DateRelative,StatusWord,Duration'
        $rows[1].Size = 101L
        $rows[1].Modified = [datetime]::UtcNow.AddDays(-31)
        $rows[1].Decision = 'ordinary text'
        $rows | ConvertTo-ConsoleView -AsDataFile | Set-Content -LiteralPath $path -Encoding UTF8
        $columns = (Import-PowerShellDataFile $path).Views[0].Property
        ($columns | Where-Object Name -EQ Size).Presenter | Should -BeExactly 'BytesBar'
        ($columns | Where-Object Name -EQ Modified).Presenter | Should -BeExactly 'Date'
        $columns | Should -Contain 'Decision'
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
            @($script -split "`n" | Select-Object -Skip 1 | Where-Object Length -GT 100).Count | Should -Be 0
            & ([scriptblock]::Create($script))
            $text = $row | New-ConsoleTable | Export-ConsoleFrame -Format Text -Width 100
            $text | Should -Match 'literal-value'
            $path = Join-Path $TestDrive 'Literal.consoleviews.psd1'
            $data = $row | ConvertTo-ConsoleView -AsDataFile
            @($data -split "`n" | Where-Object Length -GT 100).Count | Should -Be 0
            $data | Set-Content -LiteralPath $path -Encoding UTF8
            $view = (Import-PowerShellDataFile -LiteralPath $path).Views[0]
            $view.TypeName | Should -BeExactly $type
            $(if ($view.Property[0] -is [string]) { $view.Property[0] } else { $view.Property[0].Expression }) | Should -BeExactly $name
            Register-ConsoleView -Path $path
        } finally { Unregister-ConsoleView -TypeName $type -Confirm:$false }
    }

    It 'wraps long script literals and warns when data-file literals cannot fit' {
        $type = 'Contoso.' + ('LongType' * 20)
        $name = 'LongProperty' * 12
        $row = [pscustomobject][ordered]@{ PSTypeName = $type; $name = 'literal-value' }
        try {
            $draft = $row | ConvertTo-ConsoleView
            @($draft -split "`n" | Select-Object -Skip 1 | Where-Object Length -GT 100).Count | Should -Be 0
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
