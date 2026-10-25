# Fleet health check: menus, selections, review, live work and actionable results.
# Synthetic data and timings; reports and notifications stay in memory. No remote effects.
Import-Module -Name ConsoleMonster
$ErrorActionPreference = 'Stop'

# Data: fictional inventory, operations and findings kept in memory.
$inventory = @(
    [pscustomobject] @{ Name = 'DC01'; Domain = 'corp.example.test'; Site = 'Warsaw'; Role = 'Domain controller'; Detail = '10.20.1.11'; Offline = ''; Online = $true }
    [pscustomobject] @{ Name = 'DC02'; Domain = 'corp.example.test'; Site = 'Warsaw'; Role = 'Domain controller'; Detail = '10.20.1.12'; Offline = ''; Online = $true }
    [pscustomobject] @{ Name = 'FS01'; Domain = 'corp.example.test'; Site = 'Dublin'; Role = 'File'; Detail = '10.30.1.21'; Offline = ''; Online = $true }
    [pscustomobject] @{ Name = 'SQL01'; Domain = 'corp.example.test'; Site = 'Dublin'; Role = 'Database'; Detail = '10.30.1.31'; Offline = ''; Online = $true }
    [pscustomobject] @{ Name = 'WEB01'; Domain = 'corp.example.test'; Site = 'Berlin'; Role = 'Web'; Detail = '10.40.1.41'; Offline = ''; Online = $true }
    [pscustomobject] @{ Name = 'FS22'; Domain = 'corp.example.test'; Site = 'Berlin'; Role = 'File'; Detail = '10.40.1.22'; Offline = 'offline since 07:12'; Online = $false }
    [pscustomobject] @{ Name = 'LAB01'; Domain = 'lab.example.test'; Site = 'Lab'; Role = 'Web'; Detail = '192.168.50.40'; Offline = ''; Online = $true }
    [pscustomobject] @{ Name = 'LAB02'; Domain = 'lab.example.test'; Site = 'Lab'; Role = 'Database'; Detail = '192.168.50.30'; Offline = ''; Online = $true }
)
$catalog = @(
    [pscustomobject] @{ Id = 'SecureChannel'; Name = 'Secure channel'; Category = 'Identity'; Description = 'Tests the computer account trust with the domain.'; Expected = 'trusted'; Seconds = 1.2; Changes = $false }
    [pscustomobject] @{ Id = 'TimeOffset'; Name = 'Time offset'; Category = 'Identity'; Description = 'Compares the clock with the domain time source.'; Expected = 'under 1 s'; Seconds = 0.8; Changes = $false }
    [pscustomobject] @{ Id = 'ForwardRecord'; Name = 'Forward record'; Category = 'DNS'; Description = 'Compares the A record with the server address.'; Expected = 'matches IP'; Seconds = 0.6; Changes = $false }
    [pscustomobject] @{ Id = 'CertExpiry'; Name = 'Cert expiry'; Category = 'Certificates'; Description = 'Finds machine certificates expiring within 30 days.'; Expected = '30+ days'; Seconds = 1.8; Changes = $false }
    [pscustomobject] @{ Id = 'CertChain'; Name = 'Cert chain'; Category = 'Certificates'; Description = 'Builds each certificate chain to a trusted root.'; Expected = 'trusted root'; Seconds = 3.5; Changes = $false }
    [pscustomobject] @{ Id = 'MissingUpdates'; Name = 'Missing updates'; Category = 'Updates'; Description = 'Lists missing security updates older than 14 days.'; Expected = '0'; Seconds = 6.5; Changes = $false }
    [pscustomobject] @{ Id = 'FreeSpace'; Name = 'Free space'; Category = 'Storage'; Description = 'Measures free space on every fixed volume.'; Expected = '15% free'; Seconds = 1.3; Changes = $false }
    [pscustomobject] @{ Id = 'AutoServices'; Name = 'Auto services'; Category = 'Services'; Description = 'Checks that automatic services are running.'; Expected = 'all running'; Seconds = 2.2; Changes = $false }
)
$findings = @{
    'DC02/TimeOffset' = [pscustomobject] @{ Status = 'Fail'; Actual = '4.7 s' }
    'FS01/MissingUpdates' = [pscustomobject] @{ Status = 'Fail'; Actual = '3 updates' }
    'SQL01/FreeSpace' = [pscustomobject] @{ Status = 'Fail'; Actual = '6% on data volume' }
    'WEB01/CertExpiry' = [pscustomobject] @{ Status = 'Fail'; Actual = '9 days' }
    'WEB01/CertChain' = [pscustomobject] @{ Status = 'Warn'; Actual = 'intermediate ends in 40 days' }
    'SQL01/AutoServices' = [pscustomobject] @{ Status = 'Warn'; Actual = '1 restarted' }
}
$history = @(
    [pscustomobject] @{ When = 'Mon 19 Oct 21:04'; Servers = 3; Checks = 4; Passed = 10; Warnings = 1; Failed = 1; Elapsed = (New-TimeSpan -Seconds 372) }
    [pscustomobject] @{ When = 'Tue 20 Oct 07:30'; Servers = 2; Checks = 3; Passed = 6; Warnings = 0; Failed = 0; Elapsed = (New-TimeSpan -Seconds 66) }
)
$reportRoot = 'Reports/Fleet'
$plan = @{
    Domain = 'corp.example.test'
    Targets = @($inventory | Where-Object -FilterScript { $_.Domain -eq 'corp.example.test' -and -not $_.Offline } | Select-Object -First 5)
    Checks = @($catalog | Where-Object -Property Id -In -Value @('SecureChannel', 'TimeOffset', 'ForwardRecord', 'CertExpiry', 'CertChain', 'MissingUpdates', 'FreeSpace', 'AutoServices'))
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
            [pscustomobject] @{ Order = $ci * $plan.Targets.Count + $ni; Node = $node; Check = $check; Cell = $ni * $plan.Checks.Count + $ci; Status = $(if ($outcome) { $outcome.Status } else { 'Pass' }); Actual = $(if ($outcome) { $outcome.Actual } else { $check.Expected }); Seconds = [math]::Round($check.Seconds * (1 + $ni * .17), 1) }
        }
    }
}
function Get-Verdict {
    param(
        $Work
    )

    if (@($Work | Where-Object -Property Status -EQ -Value Fail).Count) { 'Fail' } elseif (@($Work | Where-Object -Property Status -EQ -Value Warn).Count) { 'Warn' } else { 'Pass' }
}
function Format-Seconds {
    param(
        $Value
    )
    $Value.ToString('0.0', [cultureinfo]::InvariantCulture) + 's'
}
function Get-Findings {
    param(
        $Work
    )

    $rows = @($Work | Where-Object -Property Status -NE -Value Pass | ForEach-Object -Process {
            [pscustomobject] @{ Server = $_.Node.Name; Check = $_.Check.Name; Expected = $_.Check.Expected; Actual = New-ConsoleText -Text $_.Actual -Role $_.Status }
        })
    # Explicit text presenters keep the automatic PowerShell value shaping aligned with C#.
    $columns = @(
        @{ Name = 'Server'; Presenter = 'Text' }
        @{ Name = 'Check'; Presenter = 'Text' }
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
        New-ConsoleLiveTable -WidgetId findings -Column $columns -Border Minimal
    }
}
function Get-History {
    $rows = @(for ($index = $plan.History.Count - 1; $index -ge 0; $index--) {
            $plan.History[$index]
        })
    $columns = @(
        @{ Name = 'When'; Presenter = 'Text' }
        @{ Name = 'Servers'; Presenter = 'Text' }
        @{ Name = 'Checks'; Presenter = 'Text' }
        @{ Name = 'Result'; Expression = { "$($_.Passed) passed · $($_.Warnings) warn · $($_.Failed) failed" }; Presenter = 'Text' }
        @{ Name = 'Duration'; Expression = { $_.Elapsed.ToString('hh\:mm\:ss') }; Presenter = 'Text' }
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
                Duration = "about $(Format-Seconds -Value $Check.Seconds)"
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
    $active = if ($Phase -eq 2) { @($Work | Select-Object -Skip $Done | Where-Object -FilterScript { $_.Check.Id -eq $Work[$Done].Check.Id } | Select-Object -First $plan.Parallel) } else { @() }
    $counter = [ordered] @{
        passed = @($finished | Where-Object -Property Status -EQ -Value Pass).Count
        warnings = @($finished | Where-Object -Property Status -EQ -Value Warn).Count
        failed = @($finished | Where-Object -Property Status -EQ -Value Fail).Count
    }
    New-ConsoleRows -Content {
        New-ConsoleSummaryBar -Done $Done -Total $Work.Count -Counter $counter -ElapsedSeconds ($Done * .24 / $plan.Parallel)
        New-ConsolePhases -Content {
            $names = @('Discover', 'Connect', 'Check', 'Report')
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
                New-ConsoleStatusItem -Label $names[$i] -Status $status
            }
        }
        $lanes = New-ConsoleLanes -WidgetId lanes -Content {
            foreach ($category in @($Work.Check.Category | Select-Object -Unique)) {
                $planned = @($Work | Where-Object -FilterScript { $_.Check.Category -eq $category })
                $ready = @($finished | Where-Object -FilterScript { $_.Check.Category -eq $category })
                $status = if ($ready.Count -eq $planned.Count) {
                    Get-Verdict -Work $ready
                }
                elseif (@($active | Where-Object -FilterScript { $_.Check.Category -eq $category }).Count) {
                    'Running'
                }
                else {
                    'Pending'
                }
                New-ConsoleLane -Key $category -Total $planned.Count -Done $ready.Count -Status $status `
                    -Passed @($ready | Where-Object -Property Status -EQ -Value Pass).Count -Warned @($ready | Where-Object -Property Status -EQ -Value Warn).Count -Failed @($ready | Where-Object -Property Status -EQ -Value Fail).Count
            }
        }
        New-ConsoleColumns -CollapseBelow 100 -Content {
            New-ConsolePanel -Title 'Check map' -Content {
                $states = @('Pending') * ($plan.Targets.Count * $plan.Checks.Count)
                for ($i = 0; $i -lt $states.Count; $i++) {
                    if ($i -notin $Work.Cell) {
                        $states[$i] = 'Skip'
                    }
                }
                foreach ($w in $finished) {
                    $states[$w.Cell] = $w.Status
                }
                foreach ($w in $active) {
                    $states[$w.Cell] = 'Running'
                }
                New-ConsoleCellMap -Status $states -Columns $plan.Checks.Count -RowLabels $plan.Targets.Name -LegendLines 2
            }
            New-ConsolePanel -Title Categories -Weight 1.15 -Content {
                $lanes
                New-ConsoleRule
                New-ConsoleKeyValue -InputObject ([ordered] @{
                        Domain = $plan.Domain
                        Servers = "$($plan.Targets.Count) · $(@($plan.Targets.Site | Select-Object -Unique).Count) sites"
                        Checks = "$($plan.Checks.Count) · $($Work.Count) in all"
                        Report = $plan.Format.ToUpperInvariant()
                    })
            }
            New-ConsolePanel -Title Running -Weight 0.95 -Content {
                New-ConsoleStatusList -Content {
                    foreach ($w in $active) {
                        New-ConsoleStatusItem -Label "$($w.Node.Name) · $($w.Check.Name)" -Status Running
                    }
                    if (-not @($active).Count) {
                        $text = if ($Phase -ge 3) { 'Checks complete' } else { 'Waiting for connections' }
                        New-ConsoleStatusItem -Label $text -Status $(if ($Phase -ge 3) {
                                Get-Verdict -Work $finished
                            }
                            else {
                                'Pending'
                            })
                    }
                }
            }
        }
        New-ConsoleColumns -FixedWidth 0, 0, 20 -CollapseBelow 100 -Content {
            New-ConsolePanel -Title Findings -Weight 1.5 -Content { Get-Findings -Work $finished }
            New-ConsolePanel -Title 'Slowest checks' -Content {
                $rows = @($finished | Sort-Object -Property @{
                        Expression = 'Seconds'
                        Descending = $true
                    }, Order | Select-Object -First 5 | ForEach-Object -Process {
                        [pscustomobject] @{ Check = $_.Check.Name; Server = $_.Node.Name; Took = Format-Seconds -Value $_.Seconds }
                    })
                $columns = @(
                    @{ Name = 'Check'; Presenter = 'Text' }
                    @{ Name = 'Server'; Presenter = 'Text' }
                    @{ Name = 'Took'; Presenter = 'Text' }
                )
                if ($rows.Count) {
                    $rows | Show-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget
                }
                else {
                    New-ConsoleLiveTable -WidgetId slowest -Column $columns -Border Minimal
                }
            }
            New-ConsolePanel -Title Durations -Content { New-ConsoleHistogram -WidgetId durations -Edge 0, 2, 4, 8, 16 -Height 5 -Value @($finished | ForEach-Object -MemberName Seconds) }
        }
        New-ConsolePanel -Title Log -Content {
            New-ConsoleLogRegion -NoElapsed -Height 7 -Content {
                New-ConsoleLogEntry -Message "Inventory: $($plan.Domain) · $($plan.Targets.Count) servers · $($Work.Count) checks"
                if ($Phase -ge 1) {
                    New-ConsoleLogEntry -Message "Connecting up to $($plan.Parallel) servers at a time"
                }
                foreach ($w in @($finished | Select-Object -Last 5)) {
                    $level = if ($w.Status -eq 'Fail') { 'err' } elseif ($w.Status -eq 'Warn') { 'warn' } else { 'info' }
                    New-ConsoleLogEntry -Message "$($w.Node.Name) · $($w.Check.Name): $($w.Actual)" -Level $level
                }
                if ($Phase -ge 3) {
                    New-ConsoleLogEntry -Message "Preparing $($plan.Format.ToUpperInvariant()) report: $($plan.Report)"
                }
                if ($Phase -ge 4) {
                    $text = if ($plan.Notify) { "Report ready · recipient $($plan.Email)" } else { "Report ready · $(@($finished | Where-Object -Property Status -NE -Value Pass).Count) findings to review" }
                    New-ConsoleLogEntry -Message $text
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
    $dashboard = New-ConsoleDashboard -Title '[bold]Fleet health check[/]' -Subtitle $plan.Domain `
        -Footer 'Ctrl+C cancel · results keep the findings' -Content {
        New-ConsoleRegion -WidgetId body -Content { Get-RunBody -Work $Work -Done 0 -Phase 0 }
    }
    $runState = @{ Finished = $false }
    $outcome = $Context.Run($dashboard, {
            param($Run)
            $phase = 0
            $done = 0
            foreach ($phase in 0, 1) {
                Set-ConsoleRegion -WidgetId body -Content { Get-RunBody -Work $Work -Done 0 -Phase $phase }
                $Run.Session.Delay([timespan]::FromMilliseconds(240))
                if ($Run.CancellationToken.IsCancellationRequested) {
                    return New-ConsoleMenuRunResult -Title 'Run cancelled' -Status Warn -Summary "$done of $($Work.Count) operations completed."
                }
            }
            foreach ($group in @($Work | Group-Object -Property { $_.Check.Id })) {
                for ($offset = 0; $offset -lt $group.Count; $offset += $plan.Parallel) {
                    Set-ConsoleRegion -WidgetId body -Content { Get-RunBody -Work $Work -Done $done -Phase 2 }
                    $Run.Session.Delay([timespan]::FromMilliseconds(240))
                    if ($Run.CancellationToken.IsCancellationRequested) {
                        return New-ConsoleMenuRunResult -Title 'Run cancelled' -Status Warn -Summary "$done of $($Work.Count) operations completed."
                    }
                    $done += [math]::Min($plan.Parallel, $group.Count - $offset)
                }
            }
            foreach ($phase in 3, 4) {
                Set-ConsoleRegion -WidgetId body -Content { Get-RunBody -Work $Work -Done $done -Phase $phase }
                $Run.Session.Delay([timespan]::FromMilliseconds(240))
                if ($Run.CancellationToken.IsCancellationRequested) {
                    return New-ConsoleMenuRunResult -Title 'Run cancelled' -Status Warn -Summary "$done of $($Work.Count) operations completed."
                }
            }
            $result = Get-Result -Work $Work
            $runState.Finished = $true
            $result
        })
    if (-not $runState.Finished) {
        return
    }
    $plan.History += [pscustomobject] @{ When = 'Tue 20 Oct 09:30'; Servers = @($Work.Node.Name | Select-Object -Unique).Count; Checks = @($Work.Check.Id | Select-Object -Unique).Count; Passed = @($Work | Where-Object -Property Status -EQ -Value Pass).Count; Warnings = @($Work | Where-Object -Property Status -EQ -Value Warn).Count; Failed = @($Work | Where-Object -Property Status -EQ -Value Fail).Count; Elapsed = $outcome.Elapsed }
}
function Get-Result {
    param(
        $Work
    )

    $summary = "$($Work.Count) checks on $(@($Work.Node.Name | Select-Object -Unique).Count) servers. Review the findings and choose the next action."
    # Retain the current run for next actions on the menu's owning thread.
    $next = [ordered] @{
        'View report' = {
            $MenuContext.ShowResult((New-ConsoleMenuRunResult -Title "Report · $($plan.Format.ToUpperInvariant())" `
                        -Status (Get-Verdict -Work $plan.CompletedWork) -Summary $plan.Report -Details (Get-Findings -Work $plan.CompletedWork)))
        }
    }
    if (@($Work | Where-Object -Property Status -EQ -Value Fail).Count) {
        $next['Rerun failed checks'] = { Invoke-Work -Context $MenuContext -Work @($plan.CompletedWork | Where-Object -Property Status -EQ -Value Fail) }
    }
    $next['View history'] = { $MenuContext.ShowResult((New-ConsoleMenuRunResult -Title 'Run history' -Details (Get-History))) }
    New-ConsoleMenuRunResult -Title 'Fleet check finished' -Status (Get-Verdict -Work $Work) -Summary $summary -Details (Get-Findings -Work $Work) `
        -Figure ([ordered] @{
            passed = @($Work | Where-Object -Property Status -EQ -Value Pass).Count
            warnings = @($Work | Where-Object -Property Status -EQ -Value Warn).Count
            failed = @($Work | Where-Object -Property Status -EQ -Value Fail).Count
        }) `
        -FigureStatus @{
        passed = 'Pass'
        warnings = $(if (@($Work | Where-Object -Property Status -EQ -Value Warn).Count) { 'Warn' } else { 'Pass' })
        failed = $(if (@($Work | Where-Object -Property Status -EQ -Value Fail).Count) { 'Fail' } else { 'Pass' })
    } -Next $next
}
function Get-OptionFields {
    param(
        [string] $Page
    )

    $pageOption = @{}
    if ($Page) {
        $pageOption.Page = $Page
    }
    New-ConsoleField @pageOption -Key Parallel -Label 'Servers at a time' -Type Number -Integer -Minimum 1 -Maximum 16 -Default 3 -ParameterName ThrottleLimit
    New-ConsoleField @pageOption -Key Report -Label 'Report folder' -Type Path -Kind Folder -Default $reportRoot -ParameterName ReportPath
    New-ConsoleField @pageOption -Key Format -Label 'Report format' -Type Select -Choices @(
        [pscustomobject] @{ Id = 'Html'; Name = 'HTML' }
        [pscustomobject] @{ Id = 'Csv'; Name = 'CSV' }
        [pscustomobject] @{ Id = 'Json'; Name = 'JSON' }
    ) -Display Name -CommandValue Id -Default Html -ParameterName Format
    New-ConsoleField @pageOption -Key Notify -Label 'Email the report' -Type Toggle -ParameterName Notify
    New-ConsoleField @pageOption -Key Email -Label Recipient -Default 'ops@example.test' -ParameterName To -Required `
        -VisibleWhen { $Values.Notify } -Validator { if ($_ -notlike '*@*') { 'Enter an email address' } }
}
function Set-Options {
    param(
        $Values
    )

    foreach ($name in 'Parallel', 'Report', 'Format', 'Notify', 'Email') {
        $plan[$name] = $Values[$name]
    }
}
$checkForm = New-ConsoleForm -Title 'New check' -Command Invoke-FleetCheck -SubmitText 'Run checks' `
    -Description 'Choose the scope, checks and run options.' `
    -PageDescriptions @{ Scope = 'Which servers to check.'; Checks = 'What a healthy server reports appears in the preview.'; Options = 'How the run behaves.' } -Content {
    $fieldOptions = @{
        Key = 'Domain'
        Label = 'Domain'
        Page = 'Scope'
        Type = 'Select'
        Choices = @($inventory.Domain | Select-Object -Unique)
        Default = $plan.Domain
        ParameterName = 'Domain'
    }
    New-ConsoleField @fieldOptions
    New-ConsoleField -Key Servers -Label Servers -Page Scope -Type MultiSelect -DependsOn Domain -Minimum 1 -ParameterName ComputerName `
        -Display Name -CommandValue Name -GroupBy Site -Badge Role -DisabledReason Offline -Selected Online `
        -Describe Detail -Default $plan.Targets.Name -ArgumentList (, $inventory) -LoadChoices {
        param($Values, $Inventory)
        Start-Sleep -Milliseconds 350
        $Inventory | Where-Object -Property Domain -EQ -Value $Values.Domain
    }
    New-ConsoleField -Key Checks -Label Checks -Page Checks -Type MultiSelect -Choices $catalog -Display Name -CommandValue Id `
        -GroupBy Category -Describe Description -Badge { "~$(Format-Seconds -Value $_.Seconds)" } -Preview { Get-Preview -Check $_ } `
        -Default $plan.Checks.Id -Minimum 1 -ParameterName Check
    Get-OptionFields -Page Options
}
$menu = New-ConsoleMenu -Title 'Fleet health check' -Layout Shell -Subtitle 'Tue 20 Oct 2026 · 09:30' -Content {
    New-ConsoleMenuPreset -Name 'Morning · identity & DNS' -Form $checkForm -Values @{
        Servers = @($inventory | Where-Object -Property Role -EQ -Value 'Domain controller' | ForEach-Object -MemberName Name)
        Checks = @('SecureChannel', 'TimeOffset', 'ForwardRecord')
        Parallel = 2
    } -Preview { New-ConsoleKeyValue -InputObject ([ordered] @{
                Scope = 'Domain controllers'
                Checks = 'Identity and DNS'
            }) }
    New-ConsoleMenuPreset -Name 'Nightly · all sites' -Form $checkForm -Values @{
        Servers = @($inventory | Where-Object -FilterScript { $_.Domain -eq $plan.Domain -and -not $_.Offline } | ForEach-Object -MemberName Name)
        Checks = $catalog.Id
        Parallel = 3
        Notify = $true
    } -Preview { New-ConsoleKeyValue -InputObject ([ordered] @{
                Scope = 'Every online server'
                Checks = 'All categories'
            }) }
    New-ConsoleMenuPreset -Name 'Patch Tuesday · updates' -Form $checkForm -Values @{
        Checks = @('MissingUpdates', 'AutoServices')
        Format = 'Csv'
    }
    New-ConsoleMenuPreset -Name 'Certificates · web tier' -Form $checkForm -Values @{
        Servers = @('WEB01')
        Checks = @('CertExpiry', 'CertChain', 'ForwardRecord')
    }
    New-ConsoleMenuItem -Label Presets -Hotkey p -IconName Favorite -Presets
    New-ConsoleMenuItem -Label 'New check' -Hotkey n -IconName Run -Description 'Choose a domain, servers and checks; review the command and run.' -Form $checkForm -OnSubmit {
        $plan.Domain = [string] $Values.Domain
        $plan.Targets = @($inventory | Where-Object -FilterScript { $_.Domain -eq $plan.Domain -and -not $_.Offline -and $_.Name -in $Values.Servers })
        $plan.Checks = @($catalog | Where-Object -Property Id -In -Value $Values.Checks)
        Set-Options -Values $Values
        Invoke-Work -Context $MenuContext -Work @(Get-Work)
    }
    New-ConsoleMenuItem -Label History -Hotkey h -IconName List -Detail { "$($plan.History.Count) runs" } -Page { Get-History }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem -Label Quit -Exit
}
Show-ConsoleMenu -Menu $menu -Layout Shell -AppName 'Fleet health check gallery'
