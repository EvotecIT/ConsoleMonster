# Fleet health check: menus, selections, review, live work and actionable results.
# Synthetic data and timings; reports and notifications stay in memory. No remote effects.
[CmdletBinding()]
param([switch] $StaticSnapshot, [ValidateRange(0, 5)] [int] $Step = 0)
Import-Module ConsoleMonster
$ErrorActionPreference = 'Stop'

# Data: replace these inventories, operations and findings with your own sources.
$inventory = @'
Name|Domain|Site|Role|Detail|Offline
DC01|corp.example.test|Warsaw|Domain controller|10.20.1.11|
DC02|corp.example.test|Warsaw|Domain controller|10.20.1.12|
FS01|corp.example.test|Dublin|File|10.30.1.21|
SQL01|corp.example.test|Dublin|Database|10.30.1.31|
WEB01|corp.example.test|Berlin|Web|10.40.1.41|
FS22|corp.example.test|Berlin|File|10.40.1.22|offline since 07:12
LAB01|lab.example.test|Lab|Web|192.168.50.40|
LAB02|lab.example.test|Lab|Database|192.168.50.30|
'@ | ConvertFrom-Csv -Delimiter '|' | ForEach-Object {
    $_ | Add-Member -NotePropertyName Online -NotePropertyValue (-not $_.Offline) -PassThru
}
$catalog = @'
Id|Name|Category|Description|Expected|Seconds|Changes
SecureChannel|Secure channel|Identity|Tests the computer account trust with the domain.|trusted|1.2|False
TimeOffset|Time offset|Identity|Compares the clock with the domain time source.|under 1 s|0.8|False
ForwardRecord|Forward record|DNS|Compares the A record with the server address.|matches IP|0.6|False
CertExpiry|Cert expiry|Certificates|Finds machine certificates expiring within 30 days.|30+ days|1.8|False
CertChain|Cert chain|Certificates|Builds each certificate chain to a trusted root.|trusted root|3.5|False
MissingUpdates|Missing updates|Updates|Lists missing security updates older than 14 days.|0|6.5|False
FreeSpace|Free space|Storage|Measures free space on every fixed volume.|15% free|1.3|False
AutoServices|Auto services|Services|Checks that automatic services are running.|all running|2.2|False
'@ | ConvertFrom-Csv -Delimiter '|' | ForEach-Object {
    $_.Seconds = [double]::Parse($_.Seconds, [cultureinfo]::InvariantCulture)
    $_.Changes = $_.Changes -eq 'True'; $_
}
$findings = @{
    'DC02/TimeOffset' = @('Fail', '4.7 s')
    'FS01/MissingUpdates' = @('Fail', '3 updates')
    'SQL01/FreeSpace' = @('Fail', '6% on data volume')
    'WEB01/CertExpiry' = @('Fail', '9 days')
    'WEB01/CertChain' = @('Warn', 'intermediate ends in 40 days')
    'SQL01/AutoServices' = @('Warn', '1 restarted')
}
$history = @(
    [pscustomobject] @{ When = 'Mon 19 Oct 21:04'; Servers = '3'; Checks = '4'; Result = '10 passed · 1 warn · 1 failed'; Duration = '00:06:12' }
    [pscustomobject] @{ When = 'Tue 20 Oct 07:30'; Servers = '2'; Checks = '3'; Result = '6 passed · 0 warn · 0 failed'; Duration = '00:01:06' }
)
$reportRoot = if ($IsWindows) { 'C:\Reports\Fleet' } else { '/tmp/reports/fleet' }
$plan = @{
    Domain = 'corp.example.test'; Targets = @($inventory | Where-Object { $_.Domain -eq 'corp.example.test' -and -not $_.Offline } | Select-Object -First 5)
    Checks = @($catalog | Where-Object Id -In @('SecureChannel', 'TimeOffset', 'ForwardRecord', 'CertExpiry', 'CertChain', 'MissingUpdates', 'FreeSpace', 'AutoServices'))
    History = $history; CompletedWork = @(); Parallel = 3; Report = $reportRoot; Format = 'Html'; Notify = $false; Email = 'ops@example.test'; DryRun = $false
}
$StaticSnapshot = $StaticSnapshot -or [Console]::IsInputRedirected

function Get-Work {
    for ($ci = 0; $ci -lt $plan.Checks.Count; $ci++) {
        for ($ni = 0; $ni -lt $plan.Targets.Count; $ni++) {
            $node = $plan.Targets[$ni]; $check = $plan.Checks[$ci]
            $outcome = $findings["$($node.Name)/$($check.Id)"]
            [pscustomobject] @{
                Order = $ci * $plan.Targets.Count + $ni
                Node = $node; Check = $check; Cell = $ni * $plan.Checks.Count + $ci
                Status = $(if ($outcome) { $outcome[0] } else { 'Pass' })
                Actual = $(if ($outcome) { $outcome[1] } else { $check.Expected })
                Seconds = [math]::Round($check.Seconds * (1 + $ni * .17), 1)
            }
        }
    }
}
function Get-Verdict($Work) {
    if (@($Work | Where-Object Status -EQ Fail).Count) { 'Fail' }
    elseif (@($Work | Where-Object Status -EQ Warn).Count) { 'Warn' } else { 'Pass' }
}
function Format-Seconds($Value) { $Value.ToString('0.0', [cultureinfo]::InvariantCulture) + 's' }
function Get-Findings($Work) {
    $rows = @($Work | Where-Object Status -NE Pass | ForEach-Object {
        [pscustomobject] @{ Server = $_.Node.Name; Check = $_.Check.Name; Expected = $_.Check.Expected
            Actual = New-ConsoleText $_.Actual -Role $_.Status }
    })
    # Explicit text presenters keep the automatic PowerShell value shaping aligned with C#.
    $columns = @('Server', 'Check', 'Expected' | ForEach-Object { @{ Name = $_; Presenter = 'Text' } })
    $columns += @{ Name = 'Actual'; Flexible = $true }
    if ($rows.Count) { $rows | Format-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget }
    else { New-ConsoleLiveTable -Name findings -Column $columns -Border Minimal }
}
function Get-History {
    $rows = @(for ($index = $plan.History.Count - 1; $index -ge 0; $index--) { $plan.History[$index] })
    $columns = @('When', 'Servers', 'Checks', 'Result', 'Duration' | ForEach-Object { @{ Name = $_; Presenter = 'Text' } })
    $rows | Format-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget
}
function Get-Preview($Check) {
    New-ConsoleRows -Gap 1 {
        New-ConsoleText $Check.Description
        New-ConsoleKeyValue -Markup -Data ([ordered] @{
            Group = $Check.Category; Expected = $Check.Expected; Duration = "about $(Format-Seconds $Check.Seconds)"
            Changes = $(if ($Check.Changes) { '[warn]yes[/]' } else { '[pass]no[/]' }); Parameter = $Check.Id
        })
    }
}

# Layout: snapshots and live work use this same widget tree.
function Get-RunBody($Work, [int] $Done, [int] $Phase) {
    $finished = @($Work | Select-Object -First $Done)
    $active = if ($Phase -eq 2) { @($Work | Select-Object -Skip $Done | Where-Object { $_.Check.Id -eq $Work[$Done].Check.Id } | Select-Object -First $plan.Parallel) } else { @() }
    $counter = [ordered] @{
        passed = @($finished | Where-Object Status -EQ Pass).Count
        warnings = @($finished | Where-Object Status -EQ Warn).Count
        failed = @($finished | Where-Object Status -EQ Fail).Count
    }
    New-ConsoleRows {
        New-ConsoleSummaryBar -Done $Done -Total $Work.Count -Counter $counter -Elapsed ([timespan]::FromSeconds($Done * .24 / $plan.Parallel))
        New-ConsolePhases {
            $names = @('Discover', 'Connect', 'Check', 'Report')
            for ($i = 0; $i -lt $names.Count; $i++) {
                $status = if ($i -lt $Phase) { if ($i -eq 2) { Get-Verdict $finished } else { 'Pass' } }
                    elseif ($i -eq $Phase) { 'Running' } else { 'Pending' }
                New-ConsoleStatusItem $names[$i] -Status $status
            }
        }
        $lanes = New-ConsoleLanes -Name lanes {
            foreach ($category in @($Work.Check.Category | Select-Object -Unique)) {
                $planned = @($Work | Where-Object { $_.Check.Category -eq $category })
                $ready = @($finished | Where-Object { $_.Check.Category -eq $category })
                $status = if ($ready.Count -eq $planned.Count) { Get-Verdict $ready }
                    elseif (@($active | Where-Object { $_.Check.Category -eq $category }).Count) { 'Running' } else { 'Pending' }
                New-ConsoleLane $category -Total $planned.Count -Done $ready.Count -Status $status `
                    -Passed @($ready | Where-Object Status -EQ Pass).Count -Warned @($ready | Where-Object Status -EQ Warn).Count -Failed @($ready | Where-Object Status -EQ Fail).Count
            }
        }
        New-ConsoleColumns -CollapseBelow 100 {
            New-ConsolePanel -Title 'Check map' {
                $states = @('Pending') * ($plan.Targets.Count * $plan.Checks.Count)
                for ($i = 0; $i -lt $states.Count; $i++) { if ($i -notin $Work.Cell) { $states[$i] = 'Skip' } }
                foreach ($w in $finished) { $states[$w.Cell] = $w.Status }
                foreach ($w in $active) { $states[$w.Cell] = 'Running' }
                New-ConsoleCellMap -Status $states -Columns $plan.Checks.Count -RowLabels $plan.Targets.Name -LegendLines 2
            }
            New-ConsolePanel -Title Categories -Weight 1.15 {
                $lanes; New-ConsoleRule
                New-ConsoleKeyValue -Data ([ordered] @{
                    Domain = $plan.Domain; Servers = "$($plan.Targets.Count) · $(@($plan.Targets.Site | Select-Object -Unique).Count) sites"
                    Checks = "$($plan.Checks.Count) · $($Work.Count) in all"; Report = $plan.Format.ToUpperInvariant()
                })
            }
            New-ConsolePanel -Title Running -Weight 0.95 {
                New-ConsoleStatusList {
                    foreach ($w in $active) { New-ConsoleStatusItem "$($w.Node.Name) · $($w.Check.Name)" -Status Running }
                    if (-not @($active).Count) {
                        $text = if ($Phase -ge 3) { 'Checks complete' } else { 'Waiting for connections' }
                        New-ConsoleStatusItem $text -Status $(if ($Phase -ge 3) { Get-Verdict $finished } else { 'Pending' })
                    }
                }
            }
        }
        New-ConsoleColumns -FixedWidth 0, 0, 20 -CollapseBelow 100 {
            New-ConsolePanel -Title Findings -Weight 1.5 { Get-Findings $finished }
            New-ConsolePanel -Title 'Slowest checks' {
                $rows = @($finished | Sort-Object @{ Expression = 'Seconds'; Descending = $true }, Order | Select-Object -First 5 | ForEach-Object {
                    [pscustomobject] @{ Check = $_.Check.Name; Server = $_.Node.Name; Took = Format-Seconds $_.Seconds }
                })
                $columns = @('Check', 'Server', 'Took' | ForEach-Object { @{ Name = $_; Presenter = 'Text' } })
                if ($rows.Count) { $rows | Format-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget }
                else { New-ConsoleLiveTable -Name slowest -Column $columns -Border Minimal }
            }
            New-ConsolePanel -Title Durations { New-ConsoleHistogram -Name durations -Edge 0, 2, 4, 8, 16 -Height 5 -Value @($finished | ForEach-Object Seconds) }
        }
        New-ConsolePanel -Title Log {
            New-ConsoleLog -NoTime -Height 7 {
                New-ConsoleLogEntry "Inventory: $($plan.Domain) · $($plan.Targets.Count) servers · $($Work.Count) checks"
                if ($Phase -ge 1) { New-ConsoleLogEntry "Connecting up to $($plan.Parallel) servers at a time" }
                foreach ($w in @($finished | Select-Object -Last 5)) {
                    $level = if ($w.Status -eq 'Fail') { 'err' } elseif ($w.Status -eq 'Warn') { 'warn' } else { 'info' }
                    New-ConsoleLogEntry "$($w.Node.Name) · $($w.Check.Name): $($w.Actual)" -Level $level
                }
                if ($Phase -ge 3) { New-ConsoleLogEntry "Preparing $($plan.Format.ToUpperInvariant()) report: $($plan.Report)" }
                if ($Phase -ge 4) {
                    $text = if ($plan.Notify) { "Report ready · recipient $($plan.Email)" }
                        else { "Report ready · $(@($finished | Where-Object Status -NE Pass).Count) findings to review" }
                    New-ConsoleLogEntry $text
                }
            }
        }
    }
}
function Get-Dashboard($Work, [int] $Done, [int] $Phase) {
    New-ConsoleDashboard -Title '[bold]Fleet health check[/]' `
        -Subtitle "$($plan.Domain) · $($plan.Targets.Count) servers · $($plan.Checks.Count) checks" `
        -Footer 'Ctrl+C cancel · results keep the findings' { Get-RunBody $Work $Done $Phase }
}

# Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
function Invoke-Work($Context, $Work) {
    $plan.CompletedWork = @($Work)
    $dashboard = New-ConsoleDashboard -Title '[bold]Fleet health check[/]' -Subtitle $plan.Domain `
        -Footer 'Ctrl+C cancel · results keep the findings' {
        New-ConsoleRegion -Name body { Get-RunBody $Work 0 0 }
    }
    $runState = @{ Finished = $false }
    $outcome = $Context.Run($dashboard, {
        param($Run)
        $phase = 0; $done = 0
        foreach ($phase in 0, 1) {
            Set-ConsoleRegion -Name body { Get-RunBody $Work 0 $phase }
            Start-Sleep -Milliseconds 240
            if ($Run.CancellationToken.IsCancellationRequested) {
                return New-ConsoleRunResult 'Run cancelled' -Status Warn -Summary "$done of $($Work.Count) operations completed."
            }
        }
        foreach ($group in @($Work | Group-Object { $_.Check.Id })) {
            for ($offset = 0; $offset -lt $group.Count; $offset += $plan.Parallel) {
                Set-ConsoleRegion -Name body { Get-RunBody $Work $done 2 }
                Start-Sleep -Milliseconds 240
                if ($Run.CancellationToken.IsCancellationRequested) {
                    return New-ConsoleRunResult 'Run cancelled' -Status Warn -Summary "$done of $($Work.Count) operations completed."
                }
                $done += [math]::Min($plan.Parallel, $group.Count - $offset)
            }
        }
        foreach ($phase in 3, 4) {
            Set-ConsoleRegion -Name body { Get-RunBody $Work $done $phase }
            Start-Sleep -Milliseconds 240
            if ($Run.CancellationToken.IsCancellationRequested) {
                return New-ConsoleRunResult 'Run cancelled' -Status Warn -Summary "$done of $($Work.Count) operations completed."
            }
        }
        $result = Get-Result $Work
        $runState.Finished = $true
        $result
    })
    if (-not $runState.Finished) { return }
    $plan.History += [pscustomobject] @{
        When = 'Tue 20 Oct 09:30'; Duration = '{0:hh\:mm\:ss}' -f $outcome.Elapsed; Servers = [string] @($Work.Node.Name | Select-Object -Unique).Count
        Checks = [string] @($Work.Check.Id | Select-Object -Unique).Count
        Result = "$(@($Work | Where-Object Status -EQ Pass).Count) passed · $(@($Work | Where-Object Status -EQ Warn).Count) warn · $(@($Work | Where-Object Status -EQ Fail).Count) failed"
    }
}
function Get-Result($Work) {
    $summary = "$($Work.Count) checks on $(@($Work.Node.Name | Select-Object -Unique).Count) servers. Review the findings and choose the next action."
    # Retain the current run for next actions on the menu's owning thread.
    $next = [ordered] @{
        'View report' = {
            $MenuContext.ShowResult((New-ConsoleRunResult "Report · $($plan.Format.ToUpperInvariant())" `
                -Status (Get-Verdict $plan.CompletedWork) -Summary $plan.Report -Details (Get-Findings $plan.CompletedWork)))
        }
    }
    if (@($Work | Where-Object Status -EQ Fail).Count) {
        $next['Rerun failed checks'] = { Invoke-Work $MenuContext @($plan.CompletedWork | Where-Object Status -EQ Fail) }
    }
    $next['View history'] = { $MenuContext.ShowResult((New-ConsoleRunResult 'Run history' -Details (Get-History))) }
    New-ConsoleRunResult 'Fleet check finished' -Status (Get-Verdict $Work) -Summary $summary -Details (Get-Findings $Work) `
        -Figure ([ordered] @{ passed = @($Work | Where-Object Status -EQ Pass).Count
            warnings = @($Work | Where-Object Status -EQ Warn).Count; failed = @($Work | Where-Object Status -EQ Fail).Count }) `
        -FigureStatus @{ passed = 'Pass'; warnings = $(if (@($Work | Where-Object Status -EQ Warn).Count) { 'Warn' } else { 'Pass' })
            failed = $(if (@($Work | Where-Object Status -EQ Fail).Count) { 'Fail' } else { 'Pass' }) } -Next $next
}
function Get-OptionFields([string] $Page) {
    $pageOption = @{}
    if ($Page) { $pageOption.Page = $Page }
    New-ConsoleField @pageOption Parallel 'Servers at a time' -Type Number -Integer -Minimum 1 -Maximum 16 -Default 3 -Parameter ThrottleLimit
    New-ConsoleField @pageOption Report 'Report folder' -Type Path -Kind Folder -Default $reportRoot -Parameter ReportPath
    New-ConsoleField @pageOption Format 'Report format' -Type Select -Choices @(
        [pscustomobject] @{ Id = 'Html'; Name = 'HTML' }; [pscustomobject] @{ Id = 'Csv'; Name = 'CSV' }; [pscustomobject] @{ Id = 'Json'; Name = 'JSON' }
    ) -Display Name -CommandValue Id -Default Html -Parameter Format
    New-ConsoleField @pageOption Notify 'Email the report' -Type Toggle -Parameter Notify
    New-ConsoleField @pageOption Email Recipient -Default 'ops@example.test' -Parameter To -Required `
        -VisibleWhen { $Values.Notify } -Validate { if ($_ -notlike '*@*') { 'Enter an email address' } }

}
function Set-Options($Values) {
    foreach ($name in 'Parallel', 'Report', 'Format', 'Notify', 'Email') { $plan[$name] = $Values[$name] }
}
$checkForm = New-ConsoleForm 'New check' -Command Invoke-FleetCheck -SubmitText 'Run checks' `
    -Description 'Choose the scope, checks and run options.' {
    New-ConsoleField Domain Domain -Page Scope -Type Select -Choices @($inventory.Domain | Select-Object -Unique) -Default $plan.Domain -Parameter Domain
    New-ConsoleField Servers Servers -Page Scope -Type MultiSelect -DependsOn Domain -Minimum 1 -Parameter ComputerName `
        -Display Name -CommandValue Name -GroupBy Site -Badge Role -Disabled Offline -Selected Online `
        -Describe { "$($_.Role) · $($_.Detail)" } -Default $plan.Targets.Name -ArgumentList (, $inventory) -LoadChoices {
            param($Values, $Inventory)
            Start-Sleep -Milliseconds 350
            $Inventory | Where-Object Domain -EQ $Values.Domain
        }
    New-ConsoleField Checks Checks -Page Checks -Type MultiSelect -Choices $catalog -Display Name -CommandValue Id `
        -GroupBy Category -Describe Description -Badge { "~$(Format-Seconds $_.Seconds)" } -Preview { Get-Preview $_ } `
        -Default $plan.Checks.Id -Minimum 1 -Parameter Check
    Get-OptionFields -Page Options
}
$menu = New-ConsoleMenu 'Fleet health check' -Layout Shell -Subtitle 'Tue 20 Oct 2026 · 09:30' {
    New-ConsoleMenuPreset 'Morning · identity & DNS' -Form $checkForm -Values @{
        Servers = @($inventory | Where-Object Role -EQ 'Domain controller' | ForEach-Object Name)
        Checks = @('SecureChannel', 'TimeOffset', 'ForwardRecord'); Parallel = 2
    } -Preview { New-ConsoleKeyValue -Data ([ordered] @{ Scope = 'Domain controllers'; Checks = 'Identity and DNS' }) }
    New-ConsoleMenuPreset 'Nightly · all sites' -Form $checkForm -Values @{
        Servers = @($inventory | Where-Object { $_.Domain -eq $plan.Domain -and -not $_.Offline } | ForEach-Object Name)
        Checks = $catalog.Id; Parallel = 3; Notify = $true
    } -Preview { New-ConsoleKeyValue -Data ([ordered] @{ Scope = 'Every online server'; Checks = 'All categories' }) }
    New-ConsoleMenuPreset 'Patch Tuesday · updates' -Form $checkForm -Values @{ Checks = @('MissingUpdates', 'AutoServices'); Format = 'Csv' }
    New-ConsoleMenuPreset 'Certificates · web tier' -Form $checkForm -Values @{ Servers = @('WEB01'); Checks = @('CertExpiry', 'CertChain', 'ForwardRecord') }
    New-ConsoleMenuItem Presets -Hotkey p -IconName Favorite -Presets
    New-ConsoleMenuItem 'New check' -Hotkey n -IconName Run -Description 'Choose a domain, servers and checks; review the command and run.' -Form $checkForm -OnSubmit {
        $plan.Domain = [string] $Values.Domain
        $plan.Targets = @($inventory | Where-Object { $_.Domain -eq $plan.Domain -and -not $_.Offline -and $_.Name -in $Values.Servers })
        $plan.Checks = @($catalog | Where-Object Id -In $Values.Checks)
        Set-Options $Values
        Invoke-Work $MenuContext @(Get-Work)
    }
    New-ConsoleMenuItem History -Hotkey h -IconName List -State { "$($plan.History.Count) runs" } -Page { Get-History }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem Quit -Exit
}
if ($StaticSnapshot) {
    $work = @(Get-Work)
    $done = if ($Step -lt 2) { 0 } elseif ($Step -eq 2) { [math]::Min($work.Count, $plan.Targets.Count + 2) }
        elseif ($Step -eq 3) { [int] [math]::Floor($work.Count * 2 / 3) } else { $work.Count }
    $phase = if ($Step -lt 2) { $Step } elseif ($Step -lt 4) { 2 } elseif ($Step -eq 4) { 3 } else { 4 }
    Get-Dashboard $work $done $phase | Write-ConsoleWidget
} else { Show-ConsoleMenu $menu -Layout Shell -AppName 'Fleet health check gallery' }
