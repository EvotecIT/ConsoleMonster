param(
    [ValidateSet('Shell', 'Classic', 'Inline')][string] $Layout = 'Shell',
    # Simulated seconds per real second during the run; 0 runs it without waiting.
    [double] $Speed = 7
)
# Fleet health check, the complete-application showcase as a script: built-in presets with previews, a form
# whose servers load for the chosen domain, grouped checks with a preview, the review with the equivalent
# command, and a run into a dashboard on a simulated clock that ends on a results summary with next actions.
# The outcomes are fixed, so every run tells the same story. All data is invented.
Import-Module ConsoleMonster

#region more
# The data: servers by domain, the checks, the results that are not a plain pass, presets and earlier runs.
# Database servers and the remote site have longer synthetic timings.
$servers = @(
    [pscustomobject] @{ Name = 'DC01'; Domain = 'corp.contoso.com'; Site = 'Warsaw'; Role = 'Domain controller'; Address = '10.20.1.11'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'DC02'; Domain = 'corp.contoso.com'; Site = 'Warsaw'; Role = 'Domain controller'; Address = '10.20.1.12'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'FS01'; Domain = 'corp.contoso.com'; Site = 'Warsaw'; Role = 'File'; Address = '10.20.1.21'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'SQL01'; Domain = 'corp.contoso.com'; Site = 'Warsaw'; Role = 'Database'; Address = '10.20.1.31'; Offline = $null; Slowness = 1.5 }
    [pscustomobject] @{ Name = 'WEB01'; Domain = 'corp.contoso.com'; Site = 'Warsaw'; Role = 'Web'; Address = '10.20.1.41'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'DC11'; Domain = 'corp.contoso.com'; Site = 'Dublin'; Role = 'Domain controller'; Address = '10.30.1.11'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'FS11'; Domain = 'corp.contoso.com'; Site = 'Dublin'; Role = 'File'; Address = '10.30.1.21'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'SQL11'; Domain = 'corp.contoso.com'; Site = 'Dublin'; Role = 'Database'; Address = '10.30.1.31'; Offline = $null; Slowness = 1.5 }
    [pscustomobject] @{ Name = 'WEB11'; Domain = 'corp.contoso.com'; Site = 'Dublin'; Role = 'Web'; Address = '10.30.1.41'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'DC21'; Domain = 'corp.contoso.com'; Site = 'Berlin'; Role = 'Domain controller'; Address = '10.40.1.11'; Offline = $null; Slowness = 1.3 }
    [pscustomobject] @{ Name = 'WEB21'; Domain = 'corp.contoso.com'; Site = 'Berlin'; Role = 'Web'; Address = '10.40.1.41'; Offline = $null; Slowness = 1.3 }
    [pscustomobject] @{ Name = 'FS22'; Domain = 'corp.contoso.com'; Site = 'Berlin'; Role = 'File'; Address = '10.40.1.22'; Offline = 'offline since 07:12'; Slowness = 1.3 }
    [pscustomobject] @{ Name = 'OSL-DC1'; Domain = 'fabrikam.net'; Site = 'Oslo'; Role = 'Domain controller'; Address = '172.16.1.10'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'OSL-FS1'; Domain = 'fabrikam.net'; Site = 'Oslo'; Role = 'File'; Address = '172.16.1.20'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'OSL-WEB1'; Domain = 'fabrikam.net'; Site = 'Oslo'; Role = 'Web'; Address = '172.16.1.40'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'MAD-DC1'; Domain = 'fabrikam.net'; Site = 'Madrid'; Role = 'Domain controller'; Address = '172.17.1.10'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'MAD-APP1'; Domain = 'fabrikam.net'; Site = 'Madrid'; Role = 'Web'; Address = '172.17.1.40'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'MAD-SQL1'; Domain = 'fabrikam.net'; Site = 'Madrid'; Role = 'Database'; Address = '172.17.1.30'; Offline = $null; Slowness = 1.5 }
    [pscustomobject] @{ Name = 'LAB-DC1'; Domain = 'lab.adatum.com'; Site = 'Lab'; Role = 'Domain controller'; Address = '192.168.50.10'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'LAB-WEB1'; Domain = 'lab.adatum.com'; Site = 'Lab'; Role = 'Web'; Address = '192.168.50.40'; Offline = $null; Slowness = 1.0 }
    [pscustomobject] @{ Name = 'LAB-SQL1'; Domain = 'lab.adatum.com'; Site = 'Lab'; Role = 'Database'; Address = '192.168.50.30'; Offline = $null; Slowness = 1.5 }
    [pscustomobject] @{ Name = 'LAB-TEST1'; Domain = 'lab.adatum.com'; Site = 'Lab'; Role = 'File'; Address = '192.168.50.90'; Offline = 'in maintenance until Friday'; Slowness = 1.0 }
)
$domains = [ordered] @{
    'corp.contoso.com' = 'Warsaw, Dublin, Berlin'
    'fabrikam.net'     = 'Oslo, Madrid'
    'lab.adatum.com'   = 'Lab'
}
$checks = @(
    [pscustomobject] @{ Id = 'SecureChannel'; Name = 'Secure channel'; Category = 'Identity'; Expected = 'trusted'; Seconds = 1.2; About = "Tests the computer account's trust with the domain." }
    [pscustomobject] @{ Id = 'TimeOffset'; Name = 'Time offset'; Category = 'Identity'; Expected = 'under 1 s'; Seconds = 0.8; About = "Compares the clock with the domain's time source." }
    [pscustomobject] @{ Id = 'LocalAdmins'; Name = 'Local admins'; Category = 'Identity'; Expected = 'approved list'; Seconds = 2.5; About = 'Compares local Administrators with the approved list.' }
    [pscustomobject] @{ Id = 'ForwardRecord'; Name = 'Forward record'; Category = 'DNS'; Expected = 'matches IP'; Seconds = 0.6; About = "Resolves the host name and compares the A record with the server's address." }
    [pscustomobject] @{ Id = 'ReverseRecord'; Name = 'Reverse record'; Category = 'DNS'; Expected = 'PTR present'; Seconds = 0.7; About = "Looks up the PTR record of the server's address." }
    [pscustomobject] @{ Id = 'Resolvers'; Name = 'DNS resolvers'; Category = 'DNS'; Expected = '2 of 2'; Seconds = 1.5; About = "Asks each configured resolver for the domain's SOA record." }
    [pscustomobject] @{ Id = 'CertExpiry'; Name = 'Cert expiry'; Category = 'Certificates'; Expected = '30+ days'; Seconds = 1.8; About = 'Finds machine certificates that expire within 30 days.' }
    [pscustomobject] @{ Id = 'CertChain'; Name = 'Cert chain'; Category = 'Certificates'; Expected = 'trusted root'; Seconds = 3.5; About = "Builds each certificate's chain to a trusted root." }
    [pscustomobject] @{ Id = 'MissingUpdates'; Name = 'Missing updates'; Category = 'Updates'; Expected = '0'; Seconds = 6.5; About = 'Lists security updates released more than 14 days ago and not installed.' }
    [pscustomobject] @{ Id = 'PendingRestart'; Name = 'Pending restart'; Category = 'Updates'; Expected = 'no'; Seconds = 1.0; About = 'Reports a restart waiting after updates or role changes.' }
    [pscustomobject] @{ Id = 'FreeSpace'; Name = 'Free space'; Category = 'Storage'; Expected = '15% free'; Seconds = 1.3; About = 'Measures free space on every fixed volume.' }
    [pscustomobject] @{ Id = 'VolumeHealth'; Name = 'Volume health'; Category = 'Storage'; Expected = 'healthy'; Seconds = 4.0; About = 'Reads the health and operational status of every volume.' }
    [pscustomobject] @{ Id = 'AutoServices'; Name = 'Auto services'; Category = 'Services'; Expected = 'all running'; Seconds = 2.2; About = 'Checks that automatic services are running.' }
    [pscustomobject] @{ Id = 'EventErrors'; Name = 'Event errors'; Category = 'Services'; Expected = 'under 20'; Seconds = 5.0; About = 'Counts System and Application errors in the last 24 hours.' }
)
$categories = 'Identity', 'DNS', 'Certificates', 'Updates', 'Storage', 'Services'
# Results that are not a plain pass; everything else passes.
$outcomes = @{
    'DC02/TimeOffset'      = 'Fail', '4.7 s'
    'FS11/MissingUpdates'  = 'Fail', '3 updates'
    'SQL11/FreeSpace'      = 'Fail', '6% on D:'
    'WEB21/CertExpiry'     = 'Fail', '9 days'
    'DC11/TimeOffset'      = 'Warn', '0.9 s'
    'DC21/Resolvers'       = 'Warn', '1 of 2'
    'WEB01/CertChain'      = 'Warn', 'intermediate ends in 40 days'
    'SQL01/PendingRestart' = 'Warn', 'since Sunday'
    'FS01/EventErrors'     = 'Warn', '27'
    'SQL11/EventErrors'    = 'Warn', '34'
    'WEB11/AutoServices'   = 'Warn', '1 restarted'
}
$nightly = @(
    'SecureChannel', 'TimeOffset', 'ForwardRecord', 'Resolvers', 'CertExpiry'
    'MissingUpdates', 'PendingRestart', 'FreeSpace', 'AutoServices', 'EventErrors'
)
$presets = @(
    [pscustomobject] @{
        Name     = 'Morning · identity & DNS'
        Scope    = 'domain controllers'
        Role     = 'Domain controller'
        Schedule = 'weekdays 07:00'
        Parallel = 4
        Format   = 'Html'
        Notify   = $false
        Checks   = 'SecureChannel', 'TimeOffset', 'LocalAdmins', 'ForwardRecord', 'ReverseRecord', 'Resolvers'
    }
    [pscustomobject] @{
        Name     = 'Nightly · all sites'
        Scope    = 'every online server'
        Role     = $null
        Schedule = 'every day 02:00'
        Parallel = 8
        Format   = 'Html'
        Notify   = $true
        Checks   = $nightly
    }
    [pscustomobject] @{
        Name     = 'Patch Tuesday · updates'
        Scope    = 'every online server'
        Role     = $null
        Schedule = 'second Tuesday 22:00'
        Parallel = 12
        Format   = 'Csv'
        Notify   = $true
        Checks   = 'MissingUpdates', 'PendingRestart', 'AutoServices', 'EventErrors'
    }
    [pscustomobject] @{
        Name     = 'Certificates · web tier'
        Scope    = 'web servers'
        Role     = 'Web'
        Schedule = 'Mondays 06:00'
        Parallel = 4
        Format   = 'Html'
        Notify   = $false
        Checks   = 'CertExpiry', 'CertChain', 'ForwardRecord'
    }
)
$script:history = @(
    [pscustomobject] @{
        When     = 'Sun 18 Oct 02:00'
        Domain   = 'corp.contoso.com'
        Servers  = 11
        Checks   = 10
        Passed   = 104
        Warnings = 5
        Failed   = 1
        Elapsed  = [timespan]::FromSeconds(83)
    }
    [pscustomobject] @{
        When     = 'Mon 19 Oct 02:00'
        Domain   = 'corp.contoso.com'
        Servers  = 11
        Checks   = 10
        Passed   = 103
        Warnings = 6
        Failed   = 1
        Elapsed  = [timespan]::FromSeconds(86)
    }
    [pscustomobject] @{
        When     = 'Mon 19 Oct 07:00'
        Domain   = 'fabrikam.net'
        Servers  = 2
        Checks   = 6
        Passed   = 12
        Warnings = 0
        Failed   = 0
        Elapsed  = [timespan]::FromSeconds(14)
    }
)
$loadDelay = if ($Speed -gt 0) { 1100 } else { 0 }
#endregion

# The form: a domain whose servers load, grouped checks with a preview, options, and the review with
# the equivalent command.
$domainChoices = foreach ($domain in $domains.Keys) {
    [pscustomobject] @{
        Name  = $domain
        Sites = $domains[$domain]
        Count = @($servers | Where-Object Domain -EQ $domain).Count
    }
}
$domainSettings = @{
    Key         = 'Domain'
    Label        = 'Domain'
    Type         = 'Select'
    Page         = 'Scope'
    Required     = $true
    ParameterName    = 'Domain'
    Choices      = $domainChoices
    Display      = 'Name'
    CommandValue = 'Name'
    Describe     = { "$($_.Count) servers · $($_.Sites)" }
    Badge        = { "$($_.Count) servers" }
    Help         = 'Choosing a domain loads its servers from the inventory.'
}
$domainField = New-ConsoleField @domainSettings
# The servers load in their own runspace when the domain changes: grouped by site, offline ones disabled,
# online ones chosen.
$serversSettings = @{
    Key         = 'Servers'
    Label        = 'Servers'
    Type         = 'Select'
    Multiple     = $true
    Page         = 'Scope'
    DependsOn    = 'Domain'
    Minimum      = 1
    ParameterName    = 'ComputerName'
    Help         = 'Grouped by site; offline servers cannot be picked. Space toggles, Ctrl+A selects all.'
    ArgumentList = $servers, $loadDelay
    LoadChoices  = {
        param($Values, $Inventory, $Delay)
        Start-Sleep -Milliseconds $Delay
        foreach ($server in $Inventory) {
            if ($server.Domain -eq $Values.Domain) {
                [pscustomobject] @{
                    Name    = $server.Name
                    Site    = $server.Site
                    Role    = $server.Role
                    Detail  = "$($server.Role) · $($server.Address)"
                    Offline = $server.Offline
                    Online  = -not $server.Offline
                }
            }
        }
    }
    Display      = 'Name'
    CommandValue = 'Name'
    GroupBy      = 'Site'
    Badge        = 'Role'
    Describe     = 'Detail'
    DisabledReason     = 'Offline'
    Selected     = 'Online'
}
$serversField = New-ConsoleField @serversSettings
$checksSettings = @{
    Key         = 'Checks'
    Label        = 'Checks'
    Type         = 'Select'
    Multiple     = $true
    Page         = 'Checks'
    Choices      = $checks
    Display      = 'Name'
    CommandValue = 'Id'
    GroupBy      = 'Category'
    Describe     = 'About'
    Minimum      = 1
    ParameterName    = 'Check'
    Badge        = { '~{0} s' -f $_.Seconds.ToString('0.#', [cultureinfo]::InvariantCulture) }
    Selected     = { $_.Id -in 'SecureChannel', 'TimeOffset', 'ForwardRecord' }
    Help         = 'Grouped by category; the preview shows what a healthy server reports.'
    Preview      = {
        New-ConsoleText $_.About
        New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
                Category  = $_.Category
                Healthy   = "[pass]$($_.Expected)[/]"
                Typical   = "about $($_.Seconds) s per server"
                Parameter = "[accent]-Check $_[/]"
            })
    }
}
$checksField = New-ConsoleField @checksSettings
$parallelSettings = @{
    Key      = 'Parallel'
    Label     = 'Servers at a time'
    Type      = 'Number'
    Page      = 'Options'
    Integer   = $true
    Minimum   = 1
    Maximum   = 16
    Default   = 4
    ParameterName = 'ThrottleLimit'
}
$parallelField = New-ConsoleField @parallelSettings
$formats = @(
    [pscustomobject] @{ Value = 'Html'; Title = 'HTML' }
    [pscustomobject] @{ Value = 'Csv'; Title = 'CSV' }
    [pscustomobject] @{ Value = 'Json'; Title = 'JSON' }
)
$formatSettings = @{
    Key         = 'Format'
    Label        = 'Report format'
    Type         = 'Select'
    Page         = 'Options'
    Choices      = $formats
    Display      = 'Title'
    CommandValue = 'Value'
    Default      = 'Html'
    ParameterName    = 'Format'
}
$formatField = New-ConsoleField @formatSettings
$reportField = New-ConsoleField Report 'Report folder' -Type Path -Kind Folder -Page Options `
    -Default 'Reports/Fleet' -ParameterName ReportPath
$notifyField = New-ConsoleField Notify 'Email the report' -Type Toggle -Page Options -ParameterName Notify
$emailSettings = @{
    Key        = 'Email'
    Label       = 'Recipient'
    Page        = 'Options'
    Default     = 'ops@contoso.com'
    ParameterName   = 'To'
    VisibleWhen = { $Values.Notify }
    Validator    = { if ($_ -notmatch '@') { 'Enter an email address' } }
}
$emailField = New-ConsoleField @emailSettings
$formSettings = @{
    Title       = 'New check'
    Command     = 'Invoke-FleetCheck'
    SubmitText  = 'Run checks'
    Description = 'Checks the health of every chosen server.'
    Field       = @(
        $domainField, $serversField, $checksField, $parallelField
        $formatField, $reportField, $notifyField, $emailField
    )
}
$form = New-ConsoleForm @formSettings

$menu = New-ConsoleMenu 'Fleet health check' -Subtitle 'Tue 20 Oct 2026 · 09:30' -Content {
    # Built-in presets fill in the form (servers are chosen once the domain's servers load);
    # people can save their own.
    foreach ($preset in $presets) {
        $names = @(
            $servers |
                Where-Object { -not $_.Offline -and (-not $preset.Role -or $_.Role -eq $preset.Role) } |
                ForEach-Object Name
        )
        $preview = New-ConsoleRows -Gap 1 -Content {
            $categoryNames = $checks |
                Where-Object { $preset.Checks -contains $_.Id } |
                ForEach-Object Category |
                Select-Object -Unique
            $checkNames = foreach ($id in @($preset.Checks)) { ($checks | Where-Object Id -EQ $id).Name }
            New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
                    Servers  = $preset.Scope
                    Checks   = "$(@($preset.Checks).Count) [muted]· $(@($categoryNames) -join ', ')[/]"
                    Workers  = "$($preset.Parallel) servers at a time"
                    Report   = $preset.Format.ToUpper() + $(if ($preset.Notify) { ' [muted]· emailed[/]' })
                    Schedule = $preset.Schedule
                })
            "[muted]$($checkNames -join ' · ')[/]"
        }
        $presetValues = @{
            Servers  = $names
            Checks   = $preset.Checks
            Parallel = $preset.Parallel
            Format   = $preset.Format
            Notify   = $preset.Notify
        }
        [pscustomobject]@{ Name = $preset.Name; Preview = $preview; Form = $form; Values = $presetValues }
    }
    New-ConsoleMenuItem 'Presets' -Hotkey p -IconName Favorite -Presets `
        -Description 'Scopes for New check. Applying one fills in the form; the domain stays your choice.'
    $newCheck = 'Choose a domain, its servers and the checks, review the equivalent command, and run.'
    New-ConsoleMenuItem 'New check' -Hotkey n -IconName Run -Description $newCheck -Form $form -OnSubmit {
        $chosen = @($Values.Checks | ForEach-Object { [string] $_ })
        $names = @($Values.Servers | ForEach-Object { [string] $_ })
        $plan = [pscustomobject] @{
            Domain   = [string] $Values.Domain
            Servers  = @($servers | Where-Object { $names -contains $_.Name })
            Checks   = @($checks | Where-Object { $chosen -contains $_.Id })
            Parallel = [int] $Values.Parallel
            Format   = [string] $Values.Format
            Notify   = [bool] $Values.Notify
            Email    = $Values.Email
        }
        $plan | Add-Member -NotePropertyMembers @{
            Total      = $plan.Servers.Count * $plan.Checks.Count
            ReportFile = "fleet-20261020-0930.$($plan.Format.ToLower())"
            Categories = @($categories | Where-Object { $plan.Checks.Category -contains $_ })
        }
        $reportFolder = [string] $Values.Report
        # An invented report name, kept portable in the showcase; this example writes no report file.
        $reportDisplayPath = $reportFolder.TrimEnd('/', '\').Replace('\', '/') + '/' + $plan.ReportFile
        $plan | Add-Member -NotePropertyName ReportPath -NotePropertyValue $reportDisplayPath
        $laneTotal = @{}
        foreach ($category in $plan.Categories) {
            $inCategory = @($plan.Checks | Where-Object Category -EQ $category)
            $laneTotal[$category] = $plan.Servers.Count * $inCategory.Count
        }
        $siteCount = @($plan.Servers.Site | Select-Object -Unique).Count
        $reportLabel = $plan.Format.ToUpper() + $(if ($plan.Notify) { ' [muted]· emailed[/]' })

        # The dashboard: one row per server and one column per check in the map, the categories with the plan,
        # what runs now.
        $dashboard = New-ConsoleDashboard -Title '[bold]Fleet health check[/]' -Subtitle $plan.Domain -Content {
            New-ConsoleSummaryBar -Total $plan.Total -BarWidth 24 -Counter ([ordered] @{
                    passed   = 0
                    warnings = 0
                    failed   = 0
                })
            New-ConsolePhases -Phase 'Discover', 'Connect', 'Check', 'Report'
            New-ConsoleColumns -Content {
                New-ConsolePanel -Title 'Check map' -Weight 1.0 -Content {
                    $map = @{
                        WidgetId    = 'cells'
                        Count       = $plan.Total
                        Columns     = $plan.Checks.Count
                        RowLabels   = $plan.Servers.Name
                        LegendLines = 2
                    }
                    New-ConsoleCellMap @map
                }
                New-ConsolePanel -Title 'Categories' -Weight 1.15 -Content {
                    New-ConsoleRows -Content {
                        New-ConsoleLanes -WidgetId categories -Content {
                            foreach ($category in $plan.Categories) {
                                [pscustomobject]@{ Key = $category; Total = $laneTotal[$category] }
                            }
                        }
                        New-ConsoleRule
                        New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
                                Domain  = "[accent]$($plan.Domain)[/]"
                                Servers = "$($plan.Servers.Count) [muted]· $siteCount sites[/]"
                                Checks  = "$($plan.Checks.Count) [muted]· $($plan.Total) in all[/]"
                                Report  = $reportLabel
                            })
                    }
                }
                New-ConsolePanel -Title 'Running' -Weight 0.95 -HeaderRight "$($plan.Parallel) workers" -Content {
                    New-ConsoleActivityList -WidgetId running -MaxVisible 8
                }
            }
            New-ConsoleColumns -FixedWidth 0, 0, 17 -Content {
                New-ConsolePanel -Title 'Failures' -Weight 1.52 -Content {
                    $actualColumn = @{ Name = 'Actual'; Flexible = $true; Markup = $true }
                    $columns = 'Server', 'Check', 'Expected', $actualColumn
                    New-ConsoleTable -WidgetId failures -MaxRows 5 -Border Minimal -Live -Columns $columns
                }
                New-ConsolePanel -Title 'Slowest checks' -Weight 1 -Content {
                    $tookColumn = @{ Name = 'Took'; Alignment = 'Right' }
                    New-ConsoleTable -WidgetId slowest -Border Minimal -Live -Columns 'Check', 'Server', $tookColumn
                }
                New-ConsolePanel -Title 'Durations' -Content {
                    New-ConsoleChart -WidgetId durations -Height 5 -Type Histogram -Edges 0, 2, 4, 8, 16
                }
            }
            New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion -Height 6 }
        }
        # The run moves a simulated clock: the header, the summary bar, the log and the results show its time.
        $clock = New-ConsoleSimulatedClock -Speed $Speed
        $checkRun = {
            #region more
            $random = New-Object System.Random 20261020
            # Every duration is drawn up front, server by server, so the schedule never changes
            # what the seed gives.
            $connect = @($plan.Servers | ForEach-Object { 0.4 + $random.NextDouble() * 0.7 })
            $seconds = @{}
            for ($s = 0; $s -lt $plan.Servers.Count; $s++) {
                for ($c = 0; $c -lt $plan.Checks.Count; $c++) {
                    # A check that finds a problem looks closer (a retry, a second source), so it takes
                    # a few seconds longer.
                    $key = "$($plan.Servers[$s].Name)/$($plan.Checks[$c].Id)"
                    $retry = if ($outcomes.ContainsKey($key)) { 3 } else { 0 }
                    $typical = $plan.Checks[$c].Seconds * $plan.Servers[$s].Slowness
                    $spread = 0.6 + $random.NextDouble() * 0.9
                    $seconds["$s/$c"] = [math]::Round($typical * $spread + $retry, 1)
                }
            }
            $format = { param($Value) $Value.ToString('0.0', [cultureinfo]::InvariantCulture) + ' s' }
            # Runs work items on workers in simulated time: each item is steps that run one after another on
            # the same worker; a free worker takes the next item. Finishing order follows the simulated clock,
            # never the real one.
            $schedule = {
                param($Items, $Steps, $Started, $Finished, $ItemDone)
                # The running items by item number (a plain hashtable: [ordered] would read a number as a
                # position).
                $slots = @{}
                $next = 0
                while ($next -lt $Items -or $slots.Count -gt 0) {
                    while ($slots.Count -lt [math]::Max(1, $plan.Parallel) -and $next -lt $Items) {
                        $sequence = @(& $Steps $next)
                        $slots[$next] = [pscustomobject] @{
                            Item  = $next
                            Steps = $sequence
                            Step  = 0
                            Ends  = $clock.Seconds + $sequence[0]
                        }
                        & $Started $next 0
                        $next++
                    }
                    $clock.WaitUntil(($slots.Values | Measure-Object -Property Ends -Minimum).Minimum)
                    $due = @(
                        $slots.Values |
                            Where-Object { $_.Ends -le $clock.Seconds + 0.000001 } |
                            Sort-Object Ends, Item
                    )
                    foreach ($slot in $due) {
                        & $Finished $slot.Item $slot.Step $slot.Steps[$slot.Step]
                        if ($slot.Step + 1 -lt $slot.Steps.Count) {
                            $slot.Step++
                            $slot.Ends = $clock.Seconds + $slot.Steps[$slot.Step]
                            & $Started $slot.Item $slot.Step
                        } else {
                            $slots.Remove($slot.Item)
                            & $ItemDone $slot.Item
                        }
                    }
                }
            }

            # Discover: the inventory and the plan.
            Send-ConsoleEvent dashboard status -Text 'discovering' -Status Running
            Update-Console -Key Discover -Status Running -WidgetId 'phases'
            Write-ConsoleLogEntry "Reading the inventory of $($plan.Domain)" -Highlight Smart
            $clock.Wait(0.8)
            $inDomain = @($servers | Where-Object Domain -EQ $plan.Domain)
            $inSites = @($inDomain.Site | Select-Object -Unique).Count
            $found = "$($inDomain.Count) servers in $inSites sites; $($plan.Servers.Count) selected"
            Write-ConsoleLogEntry $found -Highlight Smart
            foreach ($offline in @($inDomain | Where-Object Offline)) {
                Write-ConsoleLogEntry "$($offline.Name) skipped: $($offline.Offline)" -Level warn -Highlight Smart
            }
            $clock.Wait(0.9)
            $size = "$($plan.Servers.Count) servers, $($plan.Checks.Count) checks"
            $planned = "$($plan.Total) checks planned: $size, $($plan.Parallel) at a time"
            Write-ConsoleLogEntry $planned -Highlight Smart
            $clock.Wait(0.5)
            Update-Console -Key Discover -Status Pass -Detail "$($plan.Servers.Count) servers" -WidgetId 'phases'

            # Connect: a remote session per server, several at a time.
            Send-ConsoleEvent dashboard status -Text 'connecting' -Status Running
            Update-Console -Key Connect -Status Running -WidgetId 'phases'
            & $schedule $plan.Servers.Count { param($S) $connect[$S] } {
                param($S, $C)
                $name = $plan.Servers[$S].Name
                Update-Console -WidgetId running -Key (1000 + $S) -Label "Connecting $name"
            } {
                param($S, $C, $Took)
                $server = $plan.Servers[$S]
                Update-Console -WidgetId running -Key (1000 + $S) -Complete
                $address = "$($server.Name) ($($server.Address))"
                Write-ConsoleLogEntry "Connected to $address in $(& $format $Took)" -Highlight Smart
            } { param($S) }
            $connected = "$($plan.Servers.Count)/$($plan.Servers.Count)"
            Update-Console -Key Connect -Status Pass -Detail $connected -WidgetId 'phases'

            # Check: every server runs its checks in order; the busiest part of the screen.
            Send-ConsoleEvent dashboard status -Text 'checking' -Status Running
            Update-Console -Key Check -Status Running -WidgetId 'phases'
            $tally = @{ Done = 0; Passed = 0; Warned = 0; Failed = 0 }
            $laneDone = @{}
            $state = @{ Slow = @(); Findings = [ordered] @{} }
            $serverSeconds = @{}
            $checkSteps = {
                param($S)
                for ($c = 0; $c -lt $plan.Checks.Count; $c++) { $seconds["$S/$c"] }
            }
            & $schedule $plan.Servers.Count $checkSteps {
                param($S, $C)
                $check = $plan.Checks[$C]
                $doing = "$($plan.Servers[$S].Name) · $($check.Name)"
                Update-Console -Index ($S * $plan.Checks.Count + $C) -Status Running -WidgetId 'cells'
                Update-Console -WidgetId running -Key ($S * 100 + $C) -Label $doing
                if (-not $laneDone.ContainsKey($check.Category)) {
                    $laneDone[$check.Category] = 0
                    Update-Console -WidgetId categories -Key $check.Category -Status Running -Detail $doing
                }
            } {
                param($S, $C, $Took)
                $server = $plan.Servers[$S]
                $check = $plan.Checks[$C]
                $key = "$($server.Name)/$($check.Id)"
                $outcome = $outcomes[$key]
                $status = if ($outcome) { $outcome[0] } else { 'Pass' }
                $tally.Done++
                $serverSeconds[$S] += $Took
                Update-Console -WidgetId running -Key ($S * 100 + $C) -Complete
                Update-Console -Index ($S * $plan.Checks.Count + $C) -Status $status -WidgetId 'cells'
                $done = "$($server.Name) · $($check.Name)"
                Update-Console -WidgetId categories -Key $check.Category -Result $status -Detail $done
                $laneDone[$check.Category]++
                if ($laneDone[$check.Category] -eq $laneTotal[$check.Category]) {
                    Update-Console -WidgetId categories -Key $check.Category -Complete
                }
                switch ($status) {
                    'Fail' {
                        $tally.Failed++
                        $actual = "[fail]$($outcome[1])[/]"
                        $state.Findings[$key] = [pscustomobject] @{
                            Server   = $server.Name
                            Check    = $check.Name
                            Expected = $check.Expected
                            Actual   = $actual
                        }
                        $row = $server.Name, $check.Name, $check.Expected, $actual
                        Update-Console -WidgetId failures -Row ([pscustomobject]@{ Key = $key; Values = @($row) })
                        $finding = "${done}: $($outcome[1]), expected $($check.Expected)"
                        Write-ConsoleLogEntry $finding -Level err -Highlight Smart
                    }
                    'Warn' {
                        $tally.Warned++
                        $finding = "${done}: $($outcome[1]), expected $($check.Expected)"
                        Write-ConsoleLogEntry $finding -Level warn -Highlight Smart
                    }
                    default { $tally.Passed++ }
                }
                Update-Console -Counter ([ordered] @{
                        passed   = $tally.Passed
                        warnings = $tally.Warned
                        failed   = $tally.Failed
                    }) -WidgetId 'summary' -Value $tally.Done
                Update-Console -WidgetId durations -Value $Took -Append
                # The slowest five so far, slowest first; the table changes only when the list does.
                $slow = $state.Slow
                if ($slow.Count -lt 5 -or $Took -gt $slow[-1].Seconds) {
                    # The new check goes after every check at least as slow, so an equal time keeps the
                    # earlier one ahead.
                    $candidate = [pscustomobject] @{
                        Check   = $check.Name
                        Server  = $server.Name
                        Seconds = $Took
                    }
                    $slower = @($slow | Where-Object Seconds -GE $Took)
                    $faster = @($slow | Where-Object Seconds -LT $Took)
                    $slow = @($slower + $candidate + $faster | Select-Object -First 5)
                    $state.Slow = $slow
                    for ($rank = 0; $rank -lt $slow.Count; $rank++) {
                        $shown = $slow[$rank].Seconds.ToString('0.0', [cultureinfo]::InvariantCulture) + 's'
                        $row = $slow[$rank].Check, $slow[$rank].Server, $shown
                        Update-Console -WidgetId slowest -Row ([pscustomobject]@{ Key = "$($rank + 1)"; Values = @($row) })
                    }
                }
            } {
                param($S)
                $name = $plan.Servers[$S].Name
                $spent = & $format $serverSeconds[$S]
                Write-ConsoleLogEntry "$name finished $($plan.Checks.Count) checks in $spent" -Highlight Smart
            }
            $overall = if ($tally.Failed) { 'Fail' } elseif ($tally.Warned) { 'Warn' } else { 'Pass' }
            Update-Console -Key Check -Status $overall -Detail "$($tally.Failed) failed" -WidgetId 'phases'

            # Report: the file, then the email.
            Send-ConsoleEvent dashboard status -Text 'writing the report' -Status Running
            Update-Console -Key Report -Status Running -WidgetId 'phases'
            foreach ($step in 1..4) {
                $writing = "Writing $($plan.ReportFile)"
                Update-Console -WidgetId running -Key 9000 -Label $writing -Value ($step * 25)
                $clock.Wait(0.6)
            }
            Update-Console -WidgetId running -Key 9000 -Complete
            Write-ConsoleLogEntry "Report saved: $($plan.ReportPath) (412 KB)" -Highlight Smart
            if ($plan.Notify) {
                $clock.Wait(0.5)
                Write-ConsoleLogEntry "Report emailed to $($plan.Email)" -Highlight Smart
            }
            Update-Console -Key Report -Status Pass -Detail $plan.Format.ToUpper() -WidgetId 'phases'
            $counts = "$($tally.Failed) failed · $($tally.Warned) warnings"
            Send-ConsoleEvent dashboard status -Text $counts -Status $overall
            # The finished screen stays a moment, with the clock standing still.
            $clock.Hold(1.2)

            $script:history = @($script:history) + [pscustomobject] @{
                When     = 'Tue 20 Oct 09:30'
                Domain   = $plan.Domain
                Servers  = $plan.Servers.Count
                Checks   = $plan.Checks.Count
                Passed   = $tally.Passed
                Warnings = $tally.Warned
                Failed   = $tally.Failed
                Elapsed  = $clock.Elapsed
            }
            $script:lastReport = $plan.ReportFile
            $script:lastFailed = $tally.Failed
            $verdict = if ($tally.Failed) {
                "$($tally.Failed) need action now; $($tally.Warned) warnings to review."
            } elseif ($tally.Warned) {
                "$($tally.Warned) warnings to review; nothing failed."
            } else {
                'Nothing needs action.'
            }
            $summary = "$($plan.Total) checks on $($plan.Servers.Count) servers in $($plan.Domain). $verdict"
            $details = if ($state.Findings.Count) {
                $actualColumn = @{ Name = 'Actual'; Flexible = $true; Markup = $true }
                $columns = 'Server', 'Check', 'Expected', $actualColumn
                $state.Findings.Values | Show-ConsoleTable -Property $columns -Border Minimal -AsWidget
            }
            $next = [ordered] @{
                'Open the report' = { $MenuContext.Toast("Opened $script:lastReport", 'Info') }
            }
            if ($tally.Failed) {
                $next['Rerun the failed checks'] = {
                    $MenuContext.Toast("Queued $script:lastFailed failed checks for a rerun", 'Info')
                }
            }
            $next['Export findings to CSV'] = { $MenuContext.Toast('Saved findings-20261020-0930.csv') }
            #endregion
            $result = @{
                Title        = 'Fleet check finished'
                Status       = $overall
                Summary      = $summary
                Figure       = [ordered] @{
                    passed   = $tally.Passed
                    warnings = $tally.Warned
                    failed   = $tally.Failed
                    servers  = $plan.Servers.Count
                }
                FigureStatus = @{
                    passed   = 'Pass'
                    warnings = $(if ($tally.Warned) { 'Warn' } else { 'Pass' })
                    failed   = $(if ($tally.Failed) { 'Fail' } else { 'Pass' })
                }
                Details      = $details
                Next         = $next
            }
            New-ConsoleMenuRunResult @result
        }
        $null = $MenuContext.Run($dashboard, $checkRun, $clock)
    }
    $historyState = { "$(@($script:history).Count) runs" }
    New-ConsoleMenuItem 'History' -Hotkey h -IconName List -Description 'Earlier runs, newest first.' `
        -Detail $historyState -Page {
        $rows = for ($i = @($script:history).Count - 1; $i -ge 0; $i--) {
            $run = @($script:history)[$i]
            $tone = if ($run.Failed) { 'fail' } elseif ($run.Warnings) { 'warn' } else { 'pass' }
            [pscustomobject] @{
                When    = $run.When
                Domain  = $run.Domain
                Servers = $run.Servers
                Checks  = $run.Servers * $run.Checks
                Result  = "[$tone]$($run.Passed) passed · $($run.Warnings) warn · $($run.Failed) failed[/]"
                Took    = '{0:hh\:mm\:ss}' -f $run.Elapsed
            }
        }
        $columns = @(
            'When'
            'Domain'
            @{ Name = 'Servers'; Alignment = 'Right' }
            @{ Name = 'Checks'; Alignment = 'Right' }
            @{ Name = 'Result'; Markup = $true }
            @{ Name = 'Took'; Alignment = 'Right' }
        )
        $rows | Show-ConsoleTable -Property $columns -Border Minimal -NoSort -AsWidget
    }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem 'Quit' -Exit
}

Show-ConsoleMenu $menu -Layout $Layout -AppName 'ConsoleMonster fleet health check'
