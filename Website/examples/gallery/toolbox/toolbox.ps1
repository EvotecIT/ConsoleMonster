# Server toolbox: menus, selections, review, live work and actionable results.
# Synthetic data and timings; reports and notifications stay in memory. No remote effects.
[CmdletBinding()]
param([switch] $StaticSnapshot, [ValidateRange(0, 5)] [int] $Step = 0)
Import-Module ConsoleMonster
$ErrorActionPreference = 'Stop'

# Data: replace these inventories, operations and findings with your own sources.
$inventory = @'
Name|Domain|Site|Role|Detail|Offline
FS01|lab.example.test|Warsaw|File|Windows Server 2022|
FS02|lab.example.test|Warsaw|File|Windows Server 2022|
WEB01|lab.example.test|Warsaw|Web|Windows Server 2025|
SQL01|lab.example.test|Warsaw|Database|Windows Server 2019|
FS11|lab.example.test|Berlin|File|Windows Server 2022|
WEB11|lab.example.test|Berlin|Web|Windows Server 2025|
PRN11|lab.example.test|Berlin|Print|Windows Server 2016|offline since 09:12
BKP21|lab.example.test|Dublin|Backup|Windows Server 2022|
SQL21|lab.example.test|Dublin|Database|Windows Server 2022|
LAB01|lab.example.test|Lab|Test|Windows Server 2025|
LAB02|lab.example.test|Lab|Test|Windows Server 2025|maintenance until Friday
'@ | ConvertFrom-Csv -Delimiter '|'
$catalog = @'
Id|Name|Category|Description|Expected|Minutes|Changes
DiskCleanup|Disk cleanup|Maintenance|Removes temporary files and old update caches.|cache under 1 GB|3|True
LogRotation|Log rotation|Maintenance|Compresses logs older than 7 days.|retention 90 days|2|True
RestartPending|Pending restart check|Maintenance|Reports restarts waiting after updates.|no restart pending|1|False
PatchStatus|Patch status|Security|Lists missing security updates.|0 missing|2|False
Certificates|Certificate expiry|Security|Finds certificates expiring within 30 days.|30+ days|1|False
LocalAdmins|Local administrators audit|Security|Compares local Administrators with the approved list.|approved list|1|False
Firewall|Firewall rules review|Security|Flags inbound allow rules open to any address.|scoped rules|2|False
EventErrors|Event log errors|Health|Summarizes errors from the last 24 hours.|under 20|1|False
Services|Service health|Health|Checks and restarts stopped automatic services.|all running|1|True
DiskSpace|Disk space|Health|Warns below 15% and fails below 5% free.|15% free|1|False
Uptime|Uptime and reboots|Health|Reports unexpected reboots in the last 30 days.|0 unexpected|1|False
'@ | ConvertFrom-Csv -Delimiter '|' | ForEach-Object {
    $_.Minutes = [double]::Parse($_.Minutes, [cultureinfo]::InvariantCulture)
    $_.Changes = $_.Changes -eq 'True'; $_
}
$findings = @{
    'FS02/PatchStatus' = @('Fail', '3 updates missing for 41 days')
    'WEB01/Certificates' = @('Warn', 'expires in 24 days')
    'SQL01/DiskSpace' = @('Fail', '4% free on data volume')
    'FS11/Services' = @('Warn', '1 service restarted')
    'FS01/DiskCleanup' = @('Warn', '1.4 GB remains')
}
$history = @(
    [pscustomobject] @{ When = 'Mon 19 Oct 21:04'; Servers = '3'; Tasks = '4'; Result = '10 passed · 1 warn · 1 failed'; Duration = '00:06:12' }
    [pscustomobject] @{ When = 'Tue 20 Oct 07:30'; Servers = '2'; Tasks = '3'; Result = '6 passed · 0 warn · 0 failed'; Duration = '00:01:06' }
)
$reportRoot = if ($IsWindows) { 'C:\Reports\Toolbox' } else { '/tmp/reports/toolbox' }
$plan = @{
    Domain = 'lab.example.test'; Targets = @($inventory | Where-Object { $_.Domain -eq 'lab.example.test' -and -not $_.Offline } | Select-Object -First 4)
    Checks = @($catalog | Where-Object Id -In @('DiskCleanup', 'PatchStatus', 'Certificates', 'DiskSpace'))
    History = $history; CompletedWork = @(); Parallel = 3; Report = $reportRoot; Format = 'Html'; Notify = $false; Email = 'ops@example.test'; DryRun = $false
}
$StaticSnapshot = $StaticSnapshot -or [Console]::IsInputRedirected

function Get-Work {
    for ($ci = 0; $ci -lt $plan.Checks.Count; $ci++) {
        for ($ni = 0; $ni -lt $plan.Targets.Count; $ni++) {
            $node = $plan.Targets[$ni]; $check = $plan.Checks[$ci]
            $outcome = $findings["$($node.Name)/$($check.Id)"]
            [pscustomobject] @{
                Node = $node; Check = $check
                Status = $(if ($outcome) { $outcome[0] } else { 'Pass' })
                Actual = $(if ($outcome) { $outcome[1] } else { $check.Expected })
            }
        }
    }
}
function Get-Verdict($Work) {
    if (@($Work | Where-Object Status -EQ Fail).Count) { 'Fail' }
    elseif (@($Work | Where-Object Status -EQ Warn).Count) { 'Warn' } else { 'Pass' }
}
function Get-Findings($Work) {
    $rows = @($Work | Where-Object Status -NE Pass | ForEach-Object {
        [pscustomobject] @{ Server = $_.Node.Name; Task = $_.Check.Name; Expected = $_.Check.Expected
            Actual = New-ConsoleText $_.Actual -Role $_.Status }
    })
    # Explicit text presenters keep the automatic PowerShell value shaping aligned with C#.
    $columns = @('Server', 'Task', 'Expected' | ForEach-Object { @{ Name = $_; Presenter = 'Text' } })
    $columns += @{ Name = 'Actual'; Flexible = $true }
    if ($rows.Count) { $rows | Format-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget }
    else { New-ConsoleLiveTable -Name findings -Column $columns -Border Minimal }
}
function Get-History {
    $rows = @(for ($index = $plan.History.Count - 1; $index -ge 0; $index--) { $plan.History[$index] })
    $columns = @('When', 'Servers', 'Tasks', 'Result', 'Duration' | ForEach-Object { @{ Name = $_; Presenter = 'Text' } })
    $rows | Format-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget
}
function Get-Preview($Check) {
    New-ConsoleRows -Gap 1 {
        New-ConsoleText $Check.Description
        New-ConsoleKeyValue -Markup -Data ([ordered] @{
            Group = $Check.Category; Expected = $Check.Expected; Duration = "about $($Check.Minutes) min per server"
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
            $names = @('Discover', 'Connect', 'Tasks', 'Report')
            for ($i = 0; $i -lt $names.Count; $i++) {
                $status = if ($i -lt $Phase) { if ($i -eq 2) { Get-Verdict $finished } else { 'Pass' } }
                    elseif ($i -eq $Phase) { 'Running' } else { 'Pending' }
                New-ConsoleStatusItem $names[$i] -Status $status
            }
        }
        $lanes = New-ConsoleLanes -Name lanes {
            foreach ($node in $plan.Targets) {
                $planned = @($Work | Where-Object { $_.Node.Name -eq $node.Name })
                $ready = @($finished | Where-Object { $_.Node.Name -eq $node.Name })
                $doing = @($active | Where-Object { $_.Node.Name -eq $node.Name })
                $status = if ($ready.Count -eq $planned.Count) { Get-Verdict $ready }
                    elseif ($doing.Count) { 'Running' } else { 'Pending' }
                $detail = if ($doing.Count) { $doing[0].Check.Name } elseif ($ready.Count) { $ready[-1].Check.Name } else { $null }
                New-ConsoleLane $node.Name -Label (New-ConsoleText "$($node.Name)  $($node.Site)") -Total $planned.Count `
                    -Done $ready.Count -Status $status -Detail $detail -Passed @($ready | Where-Object Status -EQ Pass).Count `
                    -Warned @($ready | Where-Object Status -EQ Warn).Count -Failed @($ready | Where-Object Status -EQ Fail).Count
            }
        }
        New-ConsoleColumns -CollapseBelow 90 {
            New-ConsolePanel -Title Servers { $lanes }
            New-ConsolePanel -Title 'Run options' {
                New-ConsoleKeyValue -Data ([ordered] @{
                    Targets = "$($plan.Targets.Count) servers"; Tasks = "$($plan.Checks.Count) per server"
                    Parallel = [string] $plan.Parallel; Report = $plan.Format.ToUpperInvariant(); 'Dry run' = $(if ($plan.DryRun) { 'yes' } else { 'no' })
                })
            }
        }
        New-ConsolePanel -Title Findings { Get-Findings $finished }
        New-ConsolePanel -Title Log {
            New-ConsoleLog -NoTime -Height 7 {
                New-ConsoleLogEntry "Inventory: $($plan.Domain) · $($plan.Targets.Count) servers · $($Work.Count) tasks"
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
    New-ConsoleDashboard -Title '[bold]Server toolbox[/]' `
        -Subtitle "$($plan.Domain) · $($plan.Targets.Count) servers · $($plan.Checks.Count) tasks" `
        -Footer 'Ctrl+C cancel · results keep the findings' { Get-RunBody $Work $Done $Phase }
}

# Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
function Invoke-Work($Context, $Work) {
    $plan.CompletedWork = @($Work)
    $dashboard = New-ConsoleDashboard -Title '[bold]Server toolbox[/]' -Subtitle $plan.Domain `
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
        Tasks = [string] @($Work.Check.Id | Select-Object -Unique).Count
        Result = "$(@($Work | Where-Object Status -EQ Pass).Count) passed · $(@($Work | Where-Object Status -EQ Warn).Count) warn · $(@($Work | Where-Object Status -EQ Fail).Count) failed"
    }
}
function Get-Result($Work) {
    $summary = "$($Work.Count) tasks on $(@($Work.Node.Name | Select-Object -Unique).Count) servers. Review the findings and choose the next action."
    # Retain the current run for next actions on the menu's owning thread.
    $next = [ordered] @{
        'View report' = {
            $MenuContext.ShowResult((New-ConsoleRunResult "Report · $($plan.Format.ToUpperInvariant())" `
                -Status (Get-Verdict $plan.CompletedWork) -Summary $plan.Report -Details (Get-Findings $plan.CompletedWork)))
        }
    }
    if (@($Work | Where-Object Status -EQ Fail).Count) {
        $next['Rerun failed tasks'] = { Invoke-Work $MenuContext @($plan.CompletedWork | Where-Object Status -EQ Fail) }
    }
    $next['View history'] = { $MenuContext.ShowResult((New-ConsoleRunResult 'Run history' -Details (Get-History))) }
    New-ConsoleRunResult 'Run finished' -Status (Get-Verdict $Work) -Summary $summary -Details (Get-Findings $Work) `
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
    New-ConsoleField @pageOption DryRun 'Dry run' -Type Toggle -Parameter WhatIf
}
function Set-Options($Values) {
    foreach ($name in 'Parallel', 'Report', 'Format', 'Notify', 'Email', 'DryRun') { $plan[$name] = $Values[$name] }
}
$options = New-ConsoleForm Options -NoReview -SubmitText Save { Get-OptionFields }
$targetItem = New-ConsoleMenuItem Targets -Hotkey t -IconName Targets -Description 'Inventory grouped by site; unavailable servers stay visible.' `
    -LoadChoices {
        param($Values, $Inventory)
        Start-Sleep -Milliseconds 700
        $Inventory
    } -ArgumentList (, $inventory) -Display Name -GroupBy Site -Describe Detail `
    -Selected $plan.Targets -Minimum 1 -ChoiceBadge Role -ChoiceDisabled Offline -Preview {
        New-ConsoleKeyValue -Data ([ordered] @{ Server = $_.Name; Site = $_.Site; Role = $_.Role; OS = $_.Detail })
    }
$taskItem = New-ConsoleMenuItem Tasks -Hotkey k -IconName Tasks -MultiSelect $catalog -Display Name -GroupBy Category `
    -Describe Description -Selected $plan.Checks -Minimum 1 -Preview { Get-Preview $_ } -ChoiceBadge {
        $badge = $(if ($_.Changes) { 'changes · ' }) + "~$($_.Minutes) min"
        New-ConsoleText $badge -Role $(if ($_.Changes) { 'Warn' } else { 'Muted' })
    }
$menu = New-ConsoleMenu 'Server toolbox' -Layout Shell -Subtitle 'Tue 20 Oct 2026 · 09:30' {
    $targetItem; $taskItem
    New-ConsoleMenuItem Options -Hotkey o -IconName Settings -Form $options -OnSubmit { Set-Options $Values }
    New-ConsoleMenuItem Presets -Hotkey p -IconName Favorite -Presets
    New-ConsoleMenuPreset 'Morning · Warsaw' -Form $options -Values @{ Parallel = 2; DryRun = $true } -Selections @{
        $targetItem = @($inventory | Where-Object Site -EQ Warsaw)
        $taskItem = @($catalog | Where-Object Id -In 'PatchStatus', 'Certificates', 'DiskSpace')
    } -Preview { New-ConsoleKeyValue -Data ([ordered] @{ Scope = 'Warsaw servers'; Tasks = 'Security and disk space'; 'Dry run' = 'yes' }) }
    New-ConsoleMenuPreset 'Health · all sites' -Selections @{
        $targetItem = @($inventory | Where-Object { -not $_.Offline })
        $taskItem = @($catalog | Where-Object Id -In 'EventErrors', 'Services', 'DiskSpace', 'Uptime')
    }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem 'Review & run' -Hotkey r -IconName Run -Review {
        $plan.Targets = @($targetItem.Selection | ForEach-Object { $_ })
        $plan.Checks = @($taskItem.Selection | ForEach-Object { $_ })
        $values = $options.GetValues()
        Set-Options $values
        $problems = @(
            if (-not $plan.Targets.Count) { 'Pick at least one target' }
            if (-not $plan.Checks.Count) { 'Pick at least one task' }
            if ($plan.Notify -and $plan.Email -notlike '*@*') { 'Enter the email recipient in Options' }
        )
        # Quote selected identifiers as PowerShell literals; options are produced by the native form.
        $names = @($plan.Targets.Name | ForEach-Object { "'$($_.Replace("'", "''"))'" }) -join ', '
        $ids = @($plan.Checks.Id | ForEach-Object { "'$($_.Replace("'", "''"))'" }) -join ', '
        @{
            Title = 'Review & run'; Description = 'Everything the run uses; the command carries the same options.'
            Rows = [ordered] @{ Targets = $plan.Targets.Name -join ', '; Tasks = $plan.Checks.Name -join ', '
                'Servers at a time' = [string] $plan.Parallel; Report = "$($plan.Report) ($($plan.Format.ToUpperInvariant()))"
                Email = $(if ($plan.Notify) { $plan.Email } else { 'off' }); 'Dry run' = $(if ($plan.DryRun) { 'yes' } else { 'no' }) }
            Problems = $problems; Command = "Invoke-ServerToolbox -ComputerName $names -Task $ids $($options.BuildCommand())"
            Run = { Invoke-Work $MenuContext @(Get-Work) }
        }
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
} else { Show-ConsoleMenu $menu -Layout Shell -AppName 'Server toolbox gallery' }
