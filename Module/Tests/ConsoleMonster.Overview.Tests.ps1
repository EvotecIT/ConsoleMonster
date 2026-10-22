Describe 'ConsoleMonster host overview' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:OverviewModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:OverviewRuntime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        Import-Module $script:OverviewModulePath -Force -ErrorAction Stop

        # The default registry has no unregister operation. Register controlled providers only in an owned child.
        function Invoke-OverviewProviderChild {
            param([Parameter(Mandatory)][ValidateSet('Completed', 'Pending')][string] $Mode)
            $body = @'
$name = if ($mode -eq 'Completed') { 'PesterComplete' } else { 'PesterHeld' }
$held = [System.Threading.Tasks.TaskCompletionSource[ConsoleMonster.Overview.OverviewSection]]::new()
$resources = [ConsoleMonster.Overview.OverviewSection]::new($name, 'Controlled resources', [ConsoleMonster.Overview.OverviewItem[]]@(
    [ConsoleMonster.Overview.OverviewItem]::new('CPU', '8 logical CPUs', 24),
    [ConsoleMonster.Overview.OverviewItem]::new('Memory', '12 / 32 GiB', 37.5)
))
try {
    if ($mode -eq 'Completed') { $held.SetResult($resources) }
    # A compiled expression delegate does not need a PowerShell runspace on the probe's pool thread.
    $providerType = [Func[System.Threading.CancellationToken, System.Threading.Tasks.Task[ConsoleMonster.Overview.OverviewSection]]]
    $token = [System.Linq.Expressions.Expression]::Parameter([System.Threading.CancellationToken], 'token')
    $body = [System.Linq.Expressions.Expression]::Constant($held.Task, [System.Threading.Tasks.Task[ConsoleMonster.Overview.OverviewSection]])
    $provider = [System.Linq.Expressions.Expression]::Lambda($providerType, $body, [System.Linq.Expressions.ParameterExpression[]]@($token)).Compile()
    [void][ConsoleMonster.Overview.OverviewSectionRegistry]::Default.Register($name, 'Controlled resources', $provider)
    $budget = if ($mode -eq 'Completed') { 750 } else { 1 }
    $snapshot = Show-ConsoleOverview -Section $name -BudgetMilliseconds $budget -AsObject
    [pscustomobject]@{
        SectionCount = $snapshot.Sections.Count
        Name = $snapshot.Sections[0].Name
        IsPending = $snapshot.Sections[0].IsPending
        ItemCount = $snapshot.Sections[0].Items.Count
        Cpu = @($snapshot.Sections[0].Items | Where-Object Label -eq CPU)[0].Value
        Memory = @($snapshot.Sections[0].Items | Where-Object Label -eq Memory)[0].Value
        ProviderCompleted = $held.Task.IsCompleted
    } | ConvertTo-Json -Compress
} finally {
    # Release the owned task on success and failure before the child exits.
    [void]$held.TrySetResult($resources)
}
'@
            $lines = @(
                '$ErrorActionPreference = ''Stop''; try {'
                "Import-Module '$($script:OverviewModulePath.Replace("'", "''"))' -Force -ErrorAction Stop"
                "`$mode = '$Mode'"
                $body
                '} catch { [Console]::Error.WriteLine($_.Exception.ToString()); exit 1 }'
            )
            $output = @(Invoke-TestChild -Runtime $script:OverviewRuntime -Script ($lines -join "`n"))
            if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
            ($output -join "`n") | ConvertFrom-Json
        }
    }

    It 'exports the compiled command through the source module manifest' {
        $command = Get-Command Show-ConsoleOverview -Module ConsoleMonster -ErrorAction Stop
        $command.CommandType | Should -Be 'Cmdlet'
        $command.Parameters.Keys | Should -Contain 'AsObject'
        $command.Parameters.Keys | Should -Contain 'Section'
    }

    It 'returns native resource observations as one snapshot' {
        $snapshot = Show-ConsoleOverview -Section Resources -AsObject
        $snapshot.GetType().FullName | Should -Be 'ConsoleMonster.Overview.OverviewSnapshot'
        $snapshot.Sections.Count | Should -Be 1
        $snapshot.Sections[0].Name | Should -Be 'Resources'
        if ($snapshot.Sections[0].IsPending) {
            $snapshot.Sections[0].Items.Count | Should -Be 0
        } else {
            $cpu = @($snapshot.Sections[0].Items | Where-Object Label -eq CPU)
            if ($PSVersionTable.PSEdition -eq 'Core' -and $IsMacOS) {
                # Darwin can return cached counters, which have no interval from which to calculate usage.
                $cpu.Count | Should -BeLessOrEqual 1
            } else {
                $cpu.Count | Should -Be 1
            }
            foreach ($item in $cpu) {
                $item.Percentage | Should -BeGreaterOrEqual 0
                $item.Percentage | Should -BeLessOrEqual 100
            }
            @($snapshot.Sections[0].Items | Where-Object Label -eq Memory).Count | Should -Be 1
        }
    }

    It 'returns completed observations from a controlled provider' {
        $result = Invoke-OverviewProviderChild -Mode Completed
        $result.SectionCount | Should -Be 1
        $result.Name | Should -Be 'PesterComplete'
        $result.IsPending | Should -BeFalse
        $result.ItemCount | Should -Be 2
        $result.Cpu | Should -Be '8 logical CPUs'
        $result.Memory | Should -Be '12 / 32 GiB'
    }

    It 'returns a pending marker while a controlled provider remains incomplete' {
        $result = Invoke-OverviewProviderChild -Mode Pending
        $result.SectionCount | Should -Be 1
        $result.Name | Should -Be 'PesterHeld'
        $result.IsPending | Should -BeTrue
        $result.ItemCount | Should -Be 0
        $result.ProviderCompleted | Should -BeFalse
    }

    It 'captures plain text within the requested narrow width' {
        $text = Show-ConsoleOverview -Section Resources,Disks,Network -Width 60 -AsString
        $text.GetType().FullName | Should -Be 'System.String'
        $text.IndexOf([char]27) | Should -Be -1
        foreach ($line in $text.Split([char]10)) {
            [ConsoleMonster.Text.DisplayWidth]::Measure($line) | Should -BeLessOrEqual 60
        }
    }

    It 'rejects unknown section names' {
        { Show-ConsoleOverview -Section NotRegistered -AsObject -ErrorAction Stop } | Should -Throw
    }

    It 'rejects an out of range collection budget' {
        { Show-ConsoleOverview -BudgetMilliseconds 901 -AsObject -ErrorAction Stop } | Should -Throw
    }

    It 'keeps snapshot and text output parameter sets separate' {
        { Show-ConsoleOverview -AsObject -AsString -ErrorAction Stop } | Should -Throw
    }

    It 'loads generated help for every public parameter and the examples' {
        $help = Get-Help Show-ConsoleOverview -Full
        [string]$help.Synopsis | Should -Match 'read-only summary'
        @($help.examples.example).Count | Should -Be 3
        foreach ($name in 'Section', 'AsObject', 'Width', 'AsString', 'BudgetMilliseconds') {
            $parameter = @($help.parameters.parameter | Where-Object Name -eq $name)
            $parameter.Count | Should -Be 1
            [string]($parameter[0].description.Text -join ' ') | Should -Not -BeNullOrEmpty
        }
    }
}
