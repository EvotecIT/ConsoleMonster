# Forest assessment: twelve targets, six workers and a full findings dashboard.
param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 11)][int]$Step = 0
)

Import-Module ConsoleMonster

# Data: fictional rules and targets. Everything stays in memory; no services or files are changed.
$DcRules = @'
Domain controllers|LDAP signing required|LDAPServerIntegrity is 1 (signing not required)|12 unsigned binds in the last 24h|1
Domain controllers|LDAP channel binding|LdapEnforceChannelBinding is 0|channel binding set to 'when supported'|1
Domain controllers|SMB signing enforced|RequireSecuritySignature is disabled|SMB1 client still installed|1
Domain controllers|Print Spooler disabled|Spooler service is running|Spooler set to manual start|1
Domain controllers|LSA protection enabled|RunAsPPL is not set|RunAsPPL set without UEFI lock|1
Domain controllers|Credential Guard posture|virtualization-based security is off|Credential Guard configured but not running|1.4
Domain controllers|Secure Boot certificate rollout|2023 CA certificate not applied|update staged, reboot pending|1
Domain controllers|Windows Update compliance|last cumulative update 94 days ago|update installed, restart pending|2.2
Domain controllers|Operating system support|Windows Server 2012 R2 is out of support|support ends in 11 months|1
Domain controllers|NTLM audit configuration|NTLM auditing is disabled|audit enabled for incoming traffic only|1
Replication|Replication latency per partner|no successful inbound replication from DC02 for 4h|latency to DC05 is 42 minutes|2.6
Replication|USN rollback detection|event 2095 logged (USN rollback suspected)|invocation ID changed after restore|1.8
Replication|SYSVOL DFSR state|DFSR state is 'In Error' (5)|DFSR backlog of 37 files|1.5
Replication|Tombstone lifetime|tombstone lifetime is 60 days|tombstone lifetime not set explicitly|1
Replication|Lingering objects|3 lingering objects in the configuration partition|strict replication consistency is off|2.0
DNS|DNS forwarders consistency|forwarders differ from the domain baseline|one forwarder does not answer|1
DNS|DNS scavenging|scavenging disabled on all zones|no-refresh interval longer than 7 days|1
DNS|Root hints reachable|4 of 13 root servers unreachable|root hints file older than 2 years|1.6
DNS|DC locator SRV records|_ldap._tcp.dc._msdcs record missing|stale SRV record for a retired DC|1
Time|Time source hierarchy|PDC emulator syncs from the local CMOS clock|NTP server configured by IP address|1
Time|Clock skew|clock skew of 7 minutes against the PDC emulator|clock skew of 48 seconds|1
Kerberos|Kerberos AES-only encryption|RC4 still allowed for Kerberos|3 accounts without AES keys|1
Kerberos|krbtgt password age|krbtgt password not changed for 812 days|krbtgt password is 190 days old|1
Kerberos|Unconstrained delegation|2 servers trusted for unconstrained delegation|1 service account trusted for delegation|1
Security|Audit policy baseline|advanced audit policy not applied|logon auditing set to success only|1.3
Security|Event log sizes|Security log is 20 MB and overwrites in 2 hours|Directory Service log under 64 MB|1
Security|Local administrators|4 unexpected members in BUILTIN\Administrators|nested group in BUILTIN\Administrators|1
Security|Firewall profile|domain firewall profile is disabled|inbound rule allows any remote address|1
Security|Remote Desktop exposure|RDP allowed without network level authentication|RDP open to all internal subnets|1
Security|PowerShell logging|script block logging is disabled|transcription not configured|1
'@ -split '\r?\n' | ForEach-Object {
    $fields = $_ -split '\|'
    [pscustomobject]@{ Category = $fields[0]; Name = $fields[1]; FailReason = $fields[2]; WarnReason = $fields[3]; Weight = [double]::Parse($fields[4], [cultureinfo]::InvariantCulture) }
}

$DomainRules = @'
Group Policy|GPO permissions baseline|Authenticated Users can edit 2 GPOs|a GPO without Authenticated Users read|2.4
Group Policy|Orphaned GPO folders|5 SYSVOL folders without a GPO object|1 GPO object without a SYSVOL folder|1
Group Policy|Empty GPOs|9 GPOs with no settings are linked|3 unlinked empty GPOs|1
Group Policy|LAPS policy|no LAPS policy applies to member servers|legacy LAPS still deployed alongside Windows LAPS|1
Accounts|Stale computer accounts|214 computers inactive for over 180 days|41 computers inactive for over 90 days|1.8
Accounts|Password never expires|18 user accounts never expire their password|2 service accounts never expire their password|1
Accounts|Privileged group membership|Domain Admins has 14 members|a disabled account is still in Enterprise Admins|1.5
Accounts|AdminSDHolder protected objects|6 orphaned adminCount=1 accounts|AdminSDHolder ACL differs from the default|1
Accounts|Service account SPN hygiene|duplicate SPN HTTP/intranet registered twice|3 SPNs on user accounts with old passwords|1.4
PKI|CA weak key size|issuing CA uses a 1024-bit RSA key|issuing CA certificate expires in 9 months|1
PKI|Certificate template exposure|template allows SAN from the requester (ESC1)|template enrollable by Domain Users|1.7
Kerberos|Constrained delegation overview|protocol transition allowed to DC services|delegation to CIFS on 2 servers|1
Forest|Recycle Bin enabled|AD Recycle Bin is not enabled|deleted object lifetime is 60 days|1
Forest|Site link costs|site link with cost 0 and 15-minute schedule gaps|2 site links use default cost 100|1
Forest|Subnets without sites|11 client subnets are not mapped to a site|3 overlapping subnet definitions|1
Forest|Functional level|domain functional level is Windows Server 2008 R2|forest functional level is behind the domains|1
'@ -split '\r?\n' | ForEach-Object {
    $fields = $_ -split '\|'
    [pscustomobject]@{ Category = $fields[0]; Name = $fields[1]; FailReason = $fields[2]; WarnReason = $fields[3]; Weight = [double]::Parse($fields[4], [cultureinfo]::InvariantCulture) }
}
$workers = 6
$totalRules = 2 * $DomainRules.Count + 10 * $DcRules.Count
$targets = @(
    [pscustomobject]@{ Key = 'dom-ad'; Label = 'ad.adatum.com (domain)' }
    [pscustomobject]@{ Key = 'dom-eu'; Label = 'eu.ad.adatum.com (domain)' }
    foreach ($i in 1..10) {
        $suffix = if ($i -le 6) { '.ad.adatum.com' } else { '.eu.ad.adatum.com' }
        [pscustomobject]@{ Key = "dc$i"; Label = ('DC{0:00}' -f $i) + $suffix }
    }
)
$randomState = @{ Seed = 2026; Seeded = $false; EventOrder = 0 }
$culture = [cultureinfo]::InvariantCulture

# Build timestamped input events, independently of the layout and live session.
function New-AssessmentEvent($Time, $Target, $Operation, $Key, $Text, $State, $Value = 0, $Total = 0) {
    [pscustomobject]@{
        Order = $randomState.EventOrder++
        Time = [Math]::Max(1, [long][Math]::Round($Time))
        Target = $Target
        Operation = $Operation
        Key = $Key
        Text = $Text
        State = $State
        Value = $Value
        Total = $Total
    }
}
function Get-AssessmentSample {
    if (-not $randomState.Seeded) {
        $randomState.Seeded = $true
        Get-Random -SetSeed $randomState.Seed -Minimum 0.0 -Maximum 1.0
    } else {
        Get-Random -Minimum 0.0 -Maximum 1.0
    }
}
function Get-Gaussian {
    $u1 = 1.0 - (Get-AssessmentSample)
    $u2 = (Get-AssessmentSample)
    [Math]::Sqrt(-2.0 * [Math]::Log($u1)) * [Math]::Cos(2.0 * [Math]::PI * $u2)
}
$events = @(
    New-AssessmentEvent 0 dashboard status $null 'discovering forest' running
    New-AssessmentEvent 0 phases state Discover $null running
    foreach ($target in $targets) {
        $count = if ($target.Key.StartsWith('dom')) { $DomainRules.Count } else { $DcRules.Count }
        New-AssessmentEvent 0 lanes add $target.Key $target.Label $null 0 $count
    }
    New-AssessmentEvent 400 log write $null 'Connecting to ad.adatum.com via LDAP (DC01.ad.adatum.com)' info
    New-AssessmentEvent 1300 log write $null 'Forest ad.adatum.com: 2 domains, 10 domain controllers, 4 sites' info
    New-AssessmentEvent 2100 log write $null 'DC10.eu.ad.adatum.com responded slowly (812 ms LDAP bind)' warn
    New-AssessmentEvent 2800 phases state Discover $null pass
    New-AssessmentEvent 2800 phases state Collect $null running
    New-AssessmentEvent 2800 dashboard status $null 'collecting rule inputs' running
    New-AssessmentEvent 3600 log write $null 'Collecting 18 data sources per domain controller' info
    New-AssessmentEvent 5200 log write $null 'Using cached schema snapshot from 2026-10-15' verbose
    New-AssessmentEvent 6800 phases state Collect $null pass
    New-AssessmentEvent 6800 phases state Evaluate $null running
    New-AssessmentEvent 6800 dashboard status $null "evaluating $totalRules rules on $workers workers" running
    $workerClock = @(0..5 | ForEach-Object { 7000 + $_ * 180.0 })
    $completions = @()
    $failures = @()
    $finishes = @()
    $counts = @{ pass = 0; warn = 0; fail = 0; skip = 0 }
    foreach ($target in $targets) {
        $worker = 0
        foreach ($w in 1..5) {
            if ($workerClock[$w] -lt $workerClock[$worker]) { $worker = $w }
        }
        $t = $workerClock[$worker]
        $domain = $target.Key.StartsWith('dom')
        $rules = if ($domain) { $DomainRules } else { $DcRules }
        $laneFailed = $false
        New-AssessmentEvent $t lanes state $target.Key $rules[0].Name running
        New-AssessmentEvent $t log write $null ($target.Label + ': evaluating ' + $rules.Count + ' rules') info
        for ($r = 0; $r -lt $rules.Count; $r++) {
            $rule = $rules[$r]
            $factor = if ($domain) { 1.6 } else { 1.0 }
            $seconds = [Math]::Round([Math]::Exp(-0.9 + 0.75 * (Get-Gaussian)) * $rule.Weight * $factor, 9)
            $t += $seconds * 1000
            $roll = (Get-AssessmentSample)
            $result = if ($target.Key -eq 'dc7' -and $rule.Category -eq 'Replication') {
                if ($roll -lt 0.45) { 'fail' } elseif ($roll -lt 0.7) { 'warn' } else { 'pass' }
            } else {
                if ($roll -lt 0.045) { 'fail' } elseif ($roll -lt 0.15) { 'warn' } elseif ($roll -lt 0.19) { 'skip' } else { 'pass' }
            }
            $next = if ($r + 1 -lt $rules.Count) { $rules[$r + 1].Name } else { 'finishing' }
            New-AssessmentEvent $t lanes advance $target.Key $next $result 1
            New-AssessmentEvent $t durations add $null $null $null $seconds
            $counts[$result]++
            $completions += $t
            $segment = @{ pass = 'Passed'; warn = 'Warnings'; fail = 'Failed'; skip = 'Skipped' }[$result]
            New-AssessmentEvent $t results add $segment $null $null 1
            if ($result -eq 'warn' -and (Get-AssessmentSample) -lt 0.5) {
                New-AssessmentEvent $t log write $null ($target.Label + ': ' + $rule.Name + ' - ' + $rule.WarnReason) warn
            }
            if ($result -eq 'fail') {
                $laneFailed = $true
                $failures += $t
                New-AssessmentEvent $t findings add $rule.Category $null $null 1
                New-AssessmentEvent $t log write $null ($target.Label + ': ' + $rule.Name + ' failed - ' + $rule.FailReason) err
            }
        }
        $t += 150
        New-AssessmentEvent $t lanes complete $target.Key
        $finishes += [pscustomobject]@{ Time = $t; Failed = $laneFailed }
        $workerClock[$worker] = $t + 120
    }
    $evaluateEnd = ($workerClock | Measure-Object -Maximum).Maximum
    $completions = @($completions | Sort-Object)
    $failures = @($failures | Sort-Object)
    $ci = 0
    $fi = 0
    for ($second = 8000; $second -le $evaluateEnd + 1000; $second += 1000) {
        $completed = 0
        $failed = 0
        while ($ci -lt $completions.Count -and $completions[$ci] -lt $second) { $completed++; $ci++ }
        while ($fi -lt $failures.Count -and $failures[$fi] -lt $second) { $failed++; $fi++ }
        New-AssessmentEvent $second throughput add completed $null $null $completed
        New-AssessmentEvent $second throughput add failed $null $null $failed
    }
    for ($second = 7000; $second -le $evaluateEnd + 1000; $second += 1000) {
        $done = @($completions | Where-Object { $_ -le $second }).Count
        $rate = if ($second -gt 7000) { $done / (($second - 7000) / 1000.0) } else { 0 }
        $remaining = if ($rate -gt 0) { ($totalRules - $done) / $rate } else { 0 }
        $left = if ($done -ge $totalRules) { '-' } else { '~' + [Math]::Ceiling($remaining).ToString($culture) + 's' }
        $finished = @($finishes | Where-Object Time -LE $second)
        New-AssessmentEvent $second summary set Evaluated "$done / $totalRules"
        New-AssessmentEvent $second summary set 'Average rate' ($rate.ToString('0.0', $culture) + ' rules/s')
        New-AssessmentEvent $second summary set Remaining $left
        New-AssessmentEvent $second summary set 'Targets done' ($finished.Count.ToString($culture) + ' / 12')
        New-AssessmentEvent $second summary set 'Targets failing' (@($finished | Where-Object Failed).Count.ToString($culture))
    }
    $level = if ($counts.fail -gt 0) { 'warn' } else { 'pass' }
    New-AssessmentEvent $evaluateEnd phases state Evaluate $null $level
    New-AssessmentEvent $evaluateEnd phases state Report $null running
    New-AssessmentEvent $evaluateEnd dashboard status $null 'writing report' running
    New-AssessmentEvent ($evaluateEnd + 900) log write $null 'Report written to Reports/Assessment-ad.adatum.com.html (4.2 MB)' pass
    New-AssessmentEvent ($evaluateEnd + 1800) phases state Report $null pass
    $final = "$totalRules rules · $($counts.pass) passed · $($counts.fail) failed · $($counts.warn) warnings · $($counts.skip) skipped"
    New-AssessmentEvent ($evaluateEnd + 1800) dashboard status $null $final $level
    New-AssessmentEvent ($evaluateEnd + 4500) log write $null 'Done.' info
)
# Keep equal timestamps in their original order on both PowerShell runtimes.
$events = @($events | Sort-Object Time, Order)
$end = $events[-1].Time
$cutoff = switch ($Step) {
    0 { 1 }
    1 { 4000 }
    11 { $end }
    default { 6800 + [long][Math]::Truncate(($end - 11300) * ($Step - 2) / 8) }
}
$snapshot = $StaticSnapshot -or [Console]::IsInputRedirected
$visible = if ($snapshot) { @($events | Where-Object Time -LE $cutoff) } else { @() }

# Layout: reduce the selected event prefix into declarative widget inputs for the printed snapshot.
$laneState = [ordered]@{}
$laneTimes = @{}
foreach ($target in $targets) {
    $total = if ($target.Key.StartsWith('dom')) { $DomainRules.Count } else { $DcRules.Count }
    $laneTimes[$target.Key] = @{ Started = $null; Finished = $null }
    $laneState[$target.Key] = @{ Key = $target.Key; Label = $target.Label; Total = $total; Done = 0; Status = 'Pending'; Passed = 0; Failed = 0; Warned = 0; Skipped = 0; Detail = '' }
}
$phaseState = [ordered]@{}
foreach ($phase in 'Discover', 'Collect', 'Evaluate', 'Report') {
    $phaseState[$phase] = @{ Status = 'Pending'; Detail = ''; Started = 0 }
}
$results = [ordered]@{ Passed = 0; Warnings = 0; Failed = 0; Skipped = 0 }
$summary = [ordered]@{ Evaluated = "0 / $totalRules"; 'Average rate' = '-'; Remaining = '-'; 'Targets done' = '0 / 12'; 'Targets failing' = '0' }
$findings = [ordered]@{ 'Domain controllers' = 0; Replication = 0; DNS = 0; Kerberos = 0; Security = 0; 'Group Policy' = 0; Accounts = 0; PKI = 0 }
$rateValues = @{
    completed = @($visible | Where-Object { $_.Target -eq 'throughput' -and $_.Key -eq 'completed' } | Select-Object -ExpandProperty Value)
    failed = @($visible | Where-Object { $_.Target -eq 'throughput' -and $_.Key -eq 'failed' } | Select-Object -ExpandProperty Value)
}
$durationValues = @($visible | Where-Object Target -EQ durations | Select-Object -ExpandProperty Value)
$logEntries = @($visible | Where-Object Target -EQ log)
$statusText = $null
$statusLevel = $null
foreach ($event in $visible) {
    $key = $event.Key
    switch ($event.Target) {
        dashboard { $statusText = $event.Text; $statusLevel = $event.State }
        phases {
            $phase = $phaseState[$key]
            $phase.Status = $event.State
            if ($event.State -eq 'running') { $phase.Started = $event.Time } else {
                $seconds = ($event.Time - $phase.Started) / 1000.0
                $format = if ($seconds -lt 10) { '0.0' } else { '0' }
                $phase.Detail = $seconds.ToString($format, $culture) + 's'
            }
        }
        lanes {
            $lane = $laneState[$key]
            switch ($event.Operation) {
                state {
                    $lane.Status = $event.State
                    $lane.Detail = $event.Text
                    $laneTimes[$key].Started = [timespan]::FromMilliseconds($event.Time)
                }
                advance {
                    $lane.Done++
                    $counter = @{ pass = 'Passed'; fail = 'Failed'; warn = 'Warned'; skip = 'Skipped' }[$event.State]
                    $lane[$counter]++
                    $lane.Detail = $event.Text
                }
                complete {
                    $lane.Status = if ($lane.Failed) { 'Fail' } elseif ($lane.Warned) { 'Warn' } else { 'Pass' }
                    $lane.Done = $lane.Total
                    $laneTimes[$key].Finished = [timespan]::FromMilliseconds($event.Time)
                }
            }
        }
        results { $results[$key] += $event.Value }
        summary { $summary[$key] = $event.Text }
        findings { $findings[$key] += $event.Value }
    }
}
$lanes = New-ConsoleLanes {
    foreach ($lane in $laneState.Values) {
        New-ConsoleLane @lane -Started $laneTimes[$lane.Key].Started -Finished $laneTimes[$lane.Key].Finished
    }
}
$dashboard = New-ConsoleDashboard -Title '[bold]Nightly checks[/]' -Subtitle 'ad.adatum.com forest assessment · 12 targets · 332 rules' -Footer '[muted]Discover · Collect · Evaluate · Report    Ctrl+C stop[/]' {
    New-ConsolePadding -Padding 1, 0, 1, 1 {
        New-ConsolePhases {
            foreach ($phase in $phaseState.Keys) {
                New-ConsoleStatusItem $phase -Status $phaseState[$phase].Status -Detail $phaseState[$phase].Detail
            }
        }
    }
    New-ConsoleColumns {
        New-ConsolePanel -Title Targets -Caption '6 workers' -Weight 2.3 { $lanes }
        New-ConsolePanel -Title Results {
            New-ConsoleBarChart -Name results -Stacked -Data $results
            New-ConsoleRule
            New-ConsoleKeyValue -Name summary -Data $summary
        }
    }
    New-ConsoleColumns {
        New-ConsolePanel -Title Throughput -Caption 'rules/s' -Weight 1.2 {
            New-ConsoleLineChart -Name throughput -Height 7 {
                New-ConsoleChartSeries completed -Value $rateValues.completed -Tone Accent
                New-ConsoleChartSeries failed -Value $rateValues.failed -Tone Fail
            }
        }
        New-ConsolePanel -Title 'Rule duration' {
            New-ConsoleHistogram -Name durations -Height 6 -Unit s -Edge 0, 0.25, 0.5, 1, 2, 4, 8 -Value $durationValues
        }
        New-ConsolePanel -Title 'Failures by category' {
            New-ConsoleBarChart -Name findings -Tone Fail -Data $findings
        }
    }
    New-ConsolePanel -Title Log -Fill {
        New-ConsoleLog -Height 6 {
            foreach ($entry in $logEntries) {
                New-ConsoleLogEntry $entry.Text -Level $entry.State -At ($entry.Time / 1000.0)
            }
        }
    }
}
$dashboard.ShowClock = $false
$dashboard.Status = $statusText
$dashboard.StatusLevel = $statusLevel

# Behaviour: native events update individual widgets; the complete run is bounded and paced at 4x.
if ($snapshot) {
    $dashboard | Write-ConsoleWidget
    return
}
Start-ConsoleDashboard $dashboard -ArgumentList (, $events) {
    param($Events)
    $previous = 0
    foreach ($event in $Events) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) { break }
        $delay = [int][Math]::Truncate(($event.Time - $previous) / 4)
        if ($delay -gt 0) { Start-Sleep -Milliseconds $delay }
        $fields = @{ Target = $event.Target; Operation = $event.Operation; Value = $event.Value; Total = $event.Total }
        if ($null -ne $event.Key) { $fields.Key = $event.Key }
        if ($null -ne $event.Text) { $fields.Text = $event.Text }
        if ($null -ne $event.State) { $fields.State = $event.State }
        Send-ConsoleEvent @fields
        $previous = $event.Time
    }
}
