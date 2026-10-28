# Server toolbox: menus, selections, review, live work and actionable results.
# Synthetic data and timings; reports and notifications stay in memory. No remote effects.
Import-Module -Name ConsoleMonster
$ErrorActionPreference = 'Stop'

# Data: fictional inventory, operations and findings kept in memory.
$inventory = @(
    [pscustomobject] @{ Name = 'FS01'; Domain = 'lab.example.test'; Site = 'Warsaw'; Role = 'File'
        Detail = 'Windows Server 2022'; Offline = '' }
    [pscustomobject] @{ Name = 'FS02'; Domain = 'lab.example.test'; Site = 'Warsaw'; Role = 'File'
        Detail = 'Windows Server 2022'; Offline = '' }
    [pscustomobject] @{ Name = 'WEB01'; Domain = 'lab.example.test'; Site = 'Warsaw'; Role = 'Web'
        Detail = 'Windows Server 2025'; Offline = '' }
    [pscustomobject] @{ Name = 'SQL01'; Domain = 'lab.example.test'; Site = 'Warsaw'
        Role = 'Database'; Detail = 'Windows Server 2019'; Offline = '' }
    [pscustomobject] @{ Name = 'FS11'; Domain = 'lab.example.test'; Site = 'Berlin'; Role = 'File'
        Detail = 'Windows Server 2022'; Offline = '' }
    [pscustomobject] @{ Name = 'WEB11'; Domain = 'lab.example.test'; Site = 'Berlin'; Role = 'Web'
        Detail = 'Windows Server 2025'; Offline = '' }
    [pscustomobject] @{ Name = 'PRN11'; Domain = 'lab.example.test'; Site = 'Berlin'; Role = 'Print'
        Detail = 'Windows Server 2016'; Offline = 'offline since 09:12' }
    [pscustomobject] @{ Name = 'BKP21'; Domain = 'lab.example.test'; Site = 'Dublin'
        Role = 'Backup'; Detail = 'Windows Server 2022'; Offline = '' }
    [pscustomobject] @{ Name = 'SQL21'; Domain = 'lab.example.test'; Site = 'Dublin'
        Role = 'Database'; Detail = 'Windows Server 2022'; Offline = '' }
    [pscustomobject] @{ Name = 'LAB01'; Domain = 'lab.example.test'; Site = 'Lab'; Role = 'Test'
        Detail = 'Windows Server 2025'; Offline = '' }
    [pscustomobject] @{ Name = 'LAB02'; Domain = 'lab.example.test'; Site = 'Lab'; Role = 'Test'
        Detail = 'Windows Server 2025'; Offline = 'maintenance until Friday' }
)
$catalog = @(
    [pscustomobject] @{ Id = 'DiskCleanup'; Name = 'Disk cleanup'; Category = 'Maintenance'
        Description = 'Removes temporary files and old update caches.'
        Expected = 'cache under 1 GB'; Minutes = 3.0; Changes = $true }
    [pscustomobject] @{ Id = 'LogRotation'; Name = 'Log rotation'; Category = 'Maintenance'
        Description = 'Compresses logs older than 7 days.'; Expected = 'retention 90 days'
        Minutes = 2.0; Changes = $true }
    [pscustomobject] @{ Id = 'RestartPending'; Name = 'Pending restart check'
        Category = 'Maintenance'; Description = 'Reports restarts waiting after updates.'
        Expected = 'no restart pending'; Minutes = 1.0; Changes = $false }
    [pscustomobject] @{ Id = 'PatchStatus'; Name = 'Patch status'; Category = 'Security'
        Description = 'Lists missing security updates.'; Expected = '0 missing'; Minutes = 2.0
        Changes = $false }
    [pscustomobject] @{ Id = 'Certificates'; Name = 'Certificate expiry'; Category = 'Security'
        Description = 'Finds certificates expiring within 30 days.'; Expected = '30+ days'
        Minutes = 1.0; Changes = $false }
    [pscustomobject] @{ Id = 'LocalAdmins'; Name = 'Local administrators audit'
        Category = 'Security'; Description = 'Compares local Administrators with the approved list.'
        Expected = 'approved list'; Minutes = 1.0; Changes = $false }
    [pscustomobject] @{ Id = 'Firewall'; Name = 'Firewall rules review'; Category = 'Security'
        Description = 'Flags inbound allow rules open to any address.'; Expected = 'scoped rules'
        Minutes = 2.0; Changes = $false }
    [pscustomobject] @{ Id = 'EventErrors'; Name = 'Event log errors'; Category = 'Health'
        Description = 'Summarizes errors from the last 24 hours.'; Expected = 'under 20'
        Minutes = 1.0; Changes = $false }
    [pscustomobject] @{ Id = 'Services'; Name = 'Service health'; Category = 'Health'
        Description = 'Checks and restarts stopped automatic services.'; Expected = 'all running'
        Minutes = 1.0; Changes = $true }
    [pscustomobject] @{ Id = 'DiskSpace'; Name = 'Disk space'; Category = 'Health'
        Description = 'Warns below 15% and fails below 5% free.'; Expected = '15% free'
        Minutes = 1.0; Changes = $false }
    [pscustomobject] @{ Id = 'Uptime'; Name = 'Uptime and reboots'; Category = 'Health'
        Description = 'Reports unexpected reboots in the last 30 days.'; Expected = '0 unexpected'
        Minutes = 1.0; Changes = $false }
)
$findings = @{
    'FS02/PatchStatus' = [pscustomobject] @{ Status = 'Fail'
        Actual = '3 updates missing for 41 days' }
    'WEB01/Certificates' = [pscustomobject] @{ Status = 'Warn'; Actual = 'expires in 24 days' }
    'SQL01/DiskSpace' = [pscustomobject] @{ Status = 'Fail'; Actual = '4% free on data volume' }
    'FS11/Services' = [pscustomobject] @{ Status = 'Warn'; Actual = '1 service restarted' }
    'FS01/DiskCleanup' = [pscustomobject] @{ Status = 'Warn'; Actual = '1.4 GB remains' }
}
$history = @(
    [pscustomobject] @{ When = 'Mon 19 Oct 21:04'; Servers = 3; Tasks = 4; Passed = 10; Warnings = 1
        Failed = 1; Elapsed = (New-TimeSpan -Seconds 372) }
    [pscustomobject] @{ When = 'Tue 20 Oct 07:30'; Servers = 2; Tasks = 3; Passed = 6; Warnings = 0
        Failed = 0; Elapsed = (New-TimeSpan -Seconds 66) }
)
$reportRoot = 'Reports/Toolbox'
$plan = @{
    Domain = 'lab.example.test'
    Targets = @($inventory |
        Where-Object -FilterScript { $_.Domain -eq 'lab.example.test' -and -not $_.Offline } |
        Select-Object -First 4)
    Checks = @($catalog | Where-Object -Property Id -In -Value @('DiskCleanup', 'PatchStatus',
        'Certificates', 'DiskSpace'))
    History = $history
    CompletedWork = @()
    Parallel = 3
    Report = $reportRoot
    Format = 'Html'
    Notify = $false
    Email = 'ops@example.test'
    DryRun = $false
}

function Get-Work {
    for ($ci = 0; $ci -lt $plan.Checks.Count; $ci++) {
        for ($ni = 0; $ni -lt $plan.Targets.Count; $ni++) {
            $node = $plan.Targets[$ni]
            $check = $plan.Checks[$ci]
            $outcome = $findings["$($node.Name)/$($check.Id)"]
            [pscustomobject] @{ Node = $node; Check = $check
                Status = $(if ($outcome) { $outcome.Status } else { 'Pass' })
                Actual = $(if ($outcome) { $outcome.Actual } else { $check.Expected }) }
        }
    }
}
function Get-Verdict {
    param(
        $Work
    )

    if (@($Work | Where-Object -Property Status -EQ -Value Fail).Count) { 'Fail' }
    elseif (@($Work | Where-Object -Property Status -EQ -Value Warn).Count) { 'Warn' }
    else { 'Pass' }
}
function Get-Findings {
    param(
        $Work
    )

    $rows = @($Work | Where-Object -Property Status -NE -Value Pass | ForEach-Object -Process {
            [pscustomobject] @{ Server = $_.Node.Name; Task = $_.Check.Name
                Expected = $_.Check.Expected
                Actual = New-ConsoleText -Text $_.Actual -Role $_.Status }
        })
    # Explicit text presenters keep the automatic PowerShell value shaping aligned with C#.
    $columns = @(
        @{ Name = 'Server'; Presenter = 'Text' }
        @{ Name = 'Task'; Presenter = 'Text' }
        @{ Name = 'Expected'; Presenter = 'Text' }
    )
    $columns += @{
        Name = 'Actual'
        Flexible = $true
    }
    if ($rows.Count) {
        $rows | Show-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget
    }
    else {
        New-ConsoleTable -WidgetId findings -Border Minimal -Live -Columns $columns
    }
}
function Get-History {
    $rows = @(for ($index = $plan.History.Count - 1; $index -ge 0; $index--) {
            $plan.History[$index]
        })
    $columns = @(
        @{ Name = 'When'; Presenter = 'Text' }
        @{ Name = 'Servers'; Presenter = 'Text' }
        @{ Name = 'Tasks'; Presenter = 'Text' }
        @{ Name = 'Result'
            Expression = { "$($_.Passed) passed · $($_.Warnings) warn · $($_.Failed) failed" }
            Presenter = 'Text' }
        @{ Name = 'Duration'; Expression = { $_.Elapsed.ToString('hh\:mm\:ss') }
            Presenter = 'Text' }
    )
    $rows | Show-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget
}
function Get-Preview {
    param(
        $Check
    )

    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleText -Text $Check.Description
        New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
                Group = $Check.Category
                Expected = $Check.Expected
                Duration = "about $($Check.Minutes) min per server"
                Changes = $(if ($Check.Changes) { '[warn]yes[/]' } else { '[pass]no[/]' })
                Parameter = $Check.Id
            })
    }
}

# Layout: build the dashboard from the completed and active work.
function Get-RunBody {
    param(
        $Work,
        [int] $Done,
        [int] $Phase
    )

    $finished = @($Work | Select-Object -First $Done)
    $active = if ($Phase -eq 2) {
        @($Work |
            Select-Object -Skip $Done |
            Where-Object -FilterScript { $_.Check.Id -eq $Work[$Done].Check.Id } |
            Select-Object -First $plan.Parallel)
    }
    else { @() }
    $counter = [ordered] @{
        passed = @($finished | Where-Object -Property Status -EQ -Value Pass).Count
        warnings = @($finished | Where-Object -Property Status -EQ -Value Warn).Count
        failed = @($finished | Where-Object -Property Status -EQ -Value Fail).Count
    }
    New-ConsoleRows -Content {
        $summaryBarParameters = @{
            Total = $Work.Count
            Counter = $counter
            ElapsedSeconds = ($Done * .24 / $plan.Parallel)
            Value = $Done
        }
        New-ConsoleSummaryBar @summaryBarParameters
        New-ConsolePhases -Content {
            $names = @('Discover', 'Connect', 'Tasks', 'Report')
            for ($i = 0; $i -lt $names.Count; $i++) {
                $status = if ($i -lt $Phase) {
                    if ($i -eq 2) {
                        Get-Verdict -Work $finished
                    }
                    else {
                        'Pass'
                    }
                }
                elseif ($i -eq $Phase) {
                    'Running'
                }
                else {
                    'Pending'
                }
                [pscustomobject]@{ Label = $names[$i]; Status = $status }
            }
        }
        $lanes = New-ConsoleLanes -WidgetId lanes -Content {
            foreach ($node in $plan.Targets) {
                $planned = @($Work | Where-Object -FilterScript { $_.Node.Name -eq $node.Name })
                $ready = @($finished | Where-Object -FilterScript { $_.Node.Name -eq $node.Name })
                $doing = @($active | Where-Object -FilterScript { $_.Node.Name -eq $node.Name })
                $status = if ($ready.Count -eq $planned.Count) {
                    Get-Verdict -Work $ready
                }
                elseif ($doing.Count) {
                    'Running'
                }
                else {
                    'Pending'
                }
                $detail = if ($doing.Count) { $doing[0].Check.Name }
                elseif ($ready.Count) { $ready[-1].Check.Name }
                else { $null }
                [pscustomobject]@{ Key = $node.Name
                    Label = (New-ConsoleText -Text "$($node.Name)  $($node.Site)")
                    Total = $planned.Count; Status = $status; Detail = $detail
                    Passed = @($ready | Where-Object -Property Status -EQ -Value Pass).Count
                    Warned = @($ready | Where-Object -Property Status -EQ -Value Warn).Count
                    Failed = @($ready | Where-Object -Property Status -EQ -Value Fail).Count
                    Value = $ready.Count }
            }
        }
        New-ConsoleColumns -CollapseBelow 90 -Content {
            New-ConsolePanel -Title Servers -Content { $lanes }
            New-ConsolePanel -Title 'Run options' -Content {
                New-ConsoleKeyValue -InputObject ([ordered] @{
                        Targets = "$($plan.Targets.Count) servers"
                        Tasks = "$($plan.Checks.Count) per server"
                        Parallel = [string] $plan.Parallel
                        Report = $plan.Format.ToUpperInvariant()
                        'Dry run' = $(if ($plan.DryRun) { 'yes' } else { 'no' })
                    })
            }
        }
        New-ConsolePanel -Title Findings -Content { Get-Findings -Work $finished }
        New-ConsolePanel -Title Log -Content {
            New-ConsoleLogRegion -NoElapsed -Height 7 -Content {
                [pscustomobject]@{ Message = "Inventory: $($plan.Domain) · $($plan.Targets.Count) servers · $($Work.Count) tasks" }
                if ($Phase -ge 1) {
                    [pscustomobject]@{ Message = "Connecting up to $($plan.Parallel) servers at a time" }
                }
                foreach ($w in @($finished | Select-Object -Last 5)) {
                    $level = if ($w.Status -eq 'Fail') { 'err' }
                    elseif ($w.Status -eq 'Warn') { 'warn' }
                    else { 'info' }
                    [pscustomobject]@{ Message = "$($w.Node.Name) · $($w.Check.Name): $($w.Actual)"
                        Level = $level }
                }
                if ($Phase -ge 3) {
                    [pscustomobject]@{ Message = "Preparing $($plan.Format.ToUpperInvariant()) report: $($plan.Report)" }
                }
                if ($Phase -ge 4) {
                    $text = if ($plan.Notify) { "Report ready · recipient $($plan.Email)" }
                    else {
                        "Report ready · $(@($finished | Where-Object -Property Status -NE -Value Pass).Count) findings to review"
                    }
                    [pscustomobject]@{ Message = $text }
                }
            }
        }
    }
}

# Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
function Invoke-Work {
    param(
        $Context,
        $Work
    )

    $plan.CompletedWork = @($Work)
    $dashboard = New-ConsoleDashboard -Title '[bold]Server toolbox[/]' -Subtitle $plan.Domain `
        -Footer 'Ctrl+C cancel · results keep the findings' -Content {
        New-ConsoleRegion -WidgetId body -Content { Get-RunBody -Work $Work -Done 0 -Phase 0 }
    }
    $runState = @{ Finished = $false }
    $outcome = $Context.Run($dashboard, {
            param($Run)
            $phase = 0
            $done = 0
            foreach ($phase in 0, 1) {
                Update-Console -WidgetId body -Content {
                    Get-RunBody -Work $Work -Done 0 -Phase $phase
                }
                $Run.Session.Delay([timespan]::FromMilliseconds(240))
                if ($Run.CancellationToken.IsCancellationRequested) {
                    $menuRunResultParameters = @{
                        Title = 'Run cancelled'
                        Status = 'Warn'
                        Summary = "$done of $($Work.Count) operations completed."
                    }
                    return New-ConsoleMenuRunResult @menuRunResultParameters
                }
            }
            foreach ($group in @($Work | Group-Object -Property { $_.Check.Id })) {
                for ($offset = 0; $offset -lt $group.Count; $offset += $plan.Parallel) {
                    Update-Console -WidgetId body -Content {
                        Get-RunBody -Work $Work -Done $done -Phase 2
                    }
                    $Run.Session.Delay([timespan]::FromMilliseconds(240))
                    if ($Run.CancellationToken.IsCancellationRequested) {
                        $menuRunResultParameters = @{
                            Title = 'Run cancelled'
                            Status = 'Warn'
                            Summary = "$done of $($Work.Count) operations completed."
                        }
                        return New-ConsoleMenuRunResult @menuRunResultParameters
                    }
                    $done += [math]::Min($plan.Parallel, $group.Count - $offset)
                }
            }
            foreach ($phase in 3, 4) {
                Update-Console -WidgetId body -Content {
                    Get-RunBody -Work $Work -Done $done -Phase $phase
                }
                $Run.Session.Delay([timespan]::FromMilliseconds(240))
                if ($Run.CancellationToken.IsCancellationRequested) {
                    $menuRunResultParameters = @{
                        Title = 'Run cancelled'
                        Status = 'Warn'
                        Summary = "$done of $($Work.Count) operations completed."
                    }
                    return New-ConsoleMenuRunResult @menuRunResultParameters
                }
            }
            $result = Get-Result -Work $Work
            $runState.Finished = $true
            $result
        })
    if (-not $runState.Finished) {
        return
    }
    $plan.History += [pscustomobject] @{ When = 'Tue 20 Oct 09:30'
        Servers = @($Work.Node.Name | Select-Object -Unique).Count
        Tasks = @($Work.Check.Id | Select-Object -Unique).Count
        Passed = @($Work | Where-Object -Property Status -EQ -Value Pass).Count
        Warnings = @($Work | Where-Object -Property Status -EQ -Value Warn).Count
        Failed = @($Work | Where-Object -Property Status -EQ -Value Fail).Count
        Elapsed = $outcome.Elapsed }
}
function Get-Result {
    param(
        $Work
    )

    $serverCount = @($Work.Node.Name | Select-Object -Unique).Count
    $summary = "$($Work.Count) tasks on $serverCount servers. " +
        'Review the findings and choose the next action.'
    # Retain the current run for next actions on the menu's owning thread.
    $next = [ordered] @{
        'View report' = {
            $reportParameters = @{
                Title = "Report · $($plan.Format.ToUpperInvariant())"
                Status = Get-Verdict -Work $plan.CompletedWork
                Summary = $plan.Report
                Details = Get-Findings -Work $plan.CompletedWork
            }
            $MenuContext.ShowResult((New-ConsoleMenuRunResult @reportParameters))
        }
    }
    if (@($Work | Where-Object -Property Status -EQ -Value Fail).Count) {
        $next['Rerun failed tasks'] = {
            $workParameters = @{
                Context = $MenuContext
                Work = @($plan.CompletedWork | Where-Object -Property Status -EQ -Value Fail)
            }
            Invoke-Work @workParameters
        }
    }
    $next['View history'] = {
        $MenuContext.ShowResult((New-ConsoleMenuRunResult -Title 'Run history' -Details (Get-History)))
    }
    $menuRunResultParameters = @{
        Title = 'Run finished'
        Status = (Get-Verdict -Work $Work)
        Summary = $summary
        Details = (Get-Findings -Work $Work)
        Next = $next
    }
    New-ConsoleMenuRunResult @menuRunResultParameters -Figure ([ordered] @{
            passed = @($Work | Where-Object -Property Status -EQ -Value Pass).Count
            warnings = @($Work | Where-Object -Property Status -EQ -Value Warn).Count
            failed = @($Work | Where-Object -Property Status -EQ -Value Fail).Count
        }) -FigureStatus @{
        passed = 'Pass'
        warnings = $(if (@($Work | Where-Object -Property Status -EQ -Value Warn).Count) { 'Warn' }
        else { 'Pass' })
        failed = $(if (@($Work | Where-Object -Property Status -EQ -Value Fail).Count) { 'Fail' }
        else { 'Pass' })
    }
}
function Get-OptionFields {
    param(
        [string] $Page
    )

    $pageOption = @{}
    if ($Page) {
        $pageOption.Page = $Page
    }
    $fieldParameters = @{
        Key = 'Parallel'
        Label = 'Servers at a time'
        Type = 'Number'
        Integer = $true
        Minimum = 1
        Maximum = 16
        Default = 3
        ParameterName = 'ThrottleLimit'
    }
    New-ConsoleField @pageOption @fieldParameters
    $fieldParameters = @{
        Key = 'Report'
        Label = 'Report folder'
        Type = 'Path'
        Kind = 'Folder'
        Default = $reportRoot
        ParameterName = 'ReportPath'
    }
    New-ConsoleField @pageOption @fieldParameters
    New-ConsoleField @pageOption -Key Format -Label 'Report format' -Type Select -Choices @(
        [pscustomobject] @{ Id = 'Html'; Name = 'HTML' }
        [pscustomobject] @{ Id = 'Csv'; Name = 'CSV' }
        [pscustomobject] @{ Id = 'Json'; Name = 'JSON' }
    ) -Display Name -CommandValue Id -Default Html -ParameterName Format
    $fieldParameters = @{
        Key = 'Notify'
        Label = 'Email the report'
        Type = 'Toggle'
        ParameterName = 'Notify'
    }
    New-ConsoleField @pageOption @fieldParameters
    $fieldParameters = @{
        Key = 'Email'
        Label = 'Recipient'
        Default = 'ops@example.test'
        ParameterName = 'To'
        Required = $true
    }
    New-ConsoleField @pageOption @fieldParameters -VisibleWhen {
        $Values.Notify
    } -Validator { if ($_ -notlike '*@*') { 'Enter an email address' } }
    New-ConsoleField @pageOption -Key DryRun -Label 'Dry run' -Type Toggle -ParameterName WhatIf
}
function Set-Options {
    param(
        $Values
    )

    foreach ($name in 'Parallel', 'Report', 'Format', 'Notify', 'Email', 'DryRun') {
        $plan[$name] = $Values[$name]
    }
}
$formOptions = @{
    Title = 'Options'
    NoReview = $true
    SubmitText = 'Save'
    PageDescriptions = @{ Options = 'Parallelism, reports and notifications.' }
    Content = { Get-OptionFields }
}
$options = New-ConsoleForm @formOptions
$menuItemParameters = @{
    Label = 'Targets'
    Hotkey = 't'
    IconName = 'Targets'
    Description = 'Inventory grouped by site; unavailable servers stay visible.'
    ArgumentList = (, $inventory)
    Display = 'Name'
    GroupBy = 'Site'
    Describe = 'Detail'
    Selected = $plan.Targets
    Minimum = 1
    ChoiceBadge = 'Role'
    ChoiceDisabledReason = 'Offline'
}
$targetItem = New-ConsoleMenuItem @menuItemParameters -LoadChoices {
    param($Values, $Inventory)
    Start-Sleep -Milliseconds 700
    $Inventory
} -Preview {
    New-ConsoleKeyValue -InputObject ([ordered] @{
            Server = $_.Name
            Site = $_.Site
            Role = $_.Role
            OS = $_.Detail
        })
}
$menuItemParameters = @{
    Label = 'Tasks'
    Hotkey = 'k'
    IconName = 'Tasks'
    MultiSelect = $catalog
    Display = 'Name'
    GroupBy = 'Category'
    Describe = 'Description'
    Selected = $plan.Checks
    Minimum = 1
}
$taskItem = New-ConsoleMenuItem @menuItemParameters -Preview {
    Get-Preview -Check $_
} -ChoiceBadge {
    $badge = $(if ($_.Changes) { 'changes · ' }) + "~$($_.Minutes) min"
    New-ConsoleText -Text $badge -Role $(if ($_.Changes) { 'Warn' } else { 'Muted' })
}
$menuParameters = @{
    Title = 'Server toolbox'
    Layout = 'Shell'
    Subtitle = 'Tue 20 Oct 2026 · 09:30'
}
$menu = New-ConsoleMenu @menuParameters -Content {
    $targetItem
    $taskItem
    New-ConsoleMenuItem -Label Options -Hotkey o -IconName Settings -Form $options -OnSubmit {
        Set-Options -Values $Values
    }
    New-ConsoleMenuItem -Label Presets -Hotkey p -IconName Favorite -Presets
    [pscustomobject]@{ Name = 'Morning · Warsaw'; Form = $options; Values = @{
        Parallel = 2
        DryRun = $true
    }; Selections = @{
        $targetItem = @($inventory | Where-Object -Property Site -EQ -Value Warsaw)
        $taskItem = @($catalog | Where-Object -Property Id -In -Value 'PatchStatus', 'Certificates',
            'DiskSpace')
    }; Preview = { New-ConsoleKeyValue -InputObject ([ordered] @{
                Scope = 'Warsaw servers'
                Tasks = 'Security and disk space'
                'Dry run' = 'yes'
            }) } }
    [pscustomobject]@{ Name = 'Health · all sites'; Selections = @{
        $targetItem = @($inventory | Where-Object -FilterScript { -not $_.Offline })
        $taskItem = @($catalog | Where-Object -Property Id -In -Value 'EventErrors', 'Services',
            'DiskSpace', 'Uptime')
    } }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem -Label 'Review & run' -Hotkey r -IconName Run -Review {
        $plan.Targets = @($targetItem.Selection | ForEach-Object -Process { $_ })
        $plan.Checks = @($taskItem.Selection | ForEach-Object -Process { $_ })
        $values = $options.GetValues()
        Set-Options -Values $values
        $problems = @(
            if (-not $plan.Targets.Count) { 'Pick at least one target' }
            if (-not $plan.Checks.Count) { 'Pick at least one task' }
            if ($plan.Notify -and $plan.Email -notlike '*@*') {
                'Enter the email recipient in Options'
            }
        )
        $commandArguments = [ordered] @{
            ComputerName = $plan.Targets.Name
            Task = $plan.Checks.Id
        }
        $command = Format-ConsoleCommand -Command Invoke-ServerToolbox -Arguments $commandArguments
        @{
            Title = 'Review & run'
            Description = 'Everything the run uses; the command carries the same options.'
            Rows = [ordered] @{
                Targets = $plan.Targets.Name -join ', '
                Tasks = $plan.Checks.Name -join ', '
                'Servers at a time' = [string] $plan.Parallel
                Report = "$($plan.Report) ($($plan.Format.ToUpperInvariant()))"
                Email = $(if ($plan.Notify) { $plan.Email } else { 'off' })
                'Dry run' = $(if ($plan.DryRun) { 'yes' } else { 'no' })
            }
            Problems = $problems
            Command = "$command $($options.BuildCommand())"
            Run = { Invoke-Work -Context $MenuContext -Work @(Get-Work) }
        }
    }
    New-ConsoleMenuItem -Label History -Hotkey h -IconName List -Detail {
        "$($plan.History.Count) runs"
    } -Page { Get-History }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem -Label Quit -Exit
}
Show-ConsoleMenu -Menu $menu -Layout Shell -AppName 'Server toolbox gallery'
