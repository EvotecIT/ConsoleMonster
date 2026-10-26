# Forest assessment: twelve targets, six workers and a full findings dashboard.

Import-Module -Name ConsoleMonster

# Data: fictional rules and targets. Everything stays in memory; no services or files are changed.
$DcRules = @(
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'LDAP signing required'
        FailReason = 'LDAPServerIntegrity is 1 (signing not required)'
        WarnReason = '12 unsigned binds in the last 24h'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'LDAP channel binding'
        FailReason = 'LdapEnforceChannelBinding is 0'
        WarnReason = 'channel binding set to ''when supported'''; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'SMB signing enforced'
        FailReason = 'RequireSecuritySignature is disabled'
        WarnReason = 'SMB1 client still installed'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'Print Spooler disabled'
        FailReason = 'Spooler service is running'; WarnReason = 'Spooler set to manual start'
        Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'LSA protection enabled'
        FailReason = 'RunAsPPL is not set'; WarnReason = 'RunAsPPL set without UEFI lock'
        Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'Credential Guard posture'
        FailReason = 'virtualization-based security is off'
        WarnReason = 'Credential Guard configured but not running'; Weight = 1.4 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'Secure Boot certificate rollout'
        FailReason = '2023 CA certificate not applied'; WarnReason = 'update staged, reboot pending'
        Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'Windows Update compliance'
        FailReason = 'last cumulative update 94 days ago'
        WarnReason = 'update installed, restart pending'; Weight = 2.2 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'Operating system support'
        FailReason = 'Windows Server 2012 R2 is out of support'
        WarnReason = 'support ends in 11 months'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Domain controllers'; Name = 'NTLM audit configuration'
        FailReason = 'NTLM auditing is disabled'
        WarnReason = 'audit enabled for incoming traffic only'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Replication'; Name = 'Replication latency per partner'
        FailReason = 'no successful inbound replication from DC02 for 4h'
        WarnReason = 'latency to DC05 is 42 minutes'; Weight = 2.6 }
    [pscustomobject] @{ Category = 'Replication'; Name = 'USN rollback detection'
        FailReason = 'event 2095 logged (USN rollback suspected)'
        WarnReason = 'invocation ID changed after restore'; Weight = 1.8 }
    [pscustomobject] @{ Category = 'Replication'; Name = 'SYSVOL DFSR state'
        FailReason = 'DFSR state is ''In Error'' (5)'; WarnReason = 'DFSR backlog of 37 files'
        Weight = 1.5 }
    [pscustomobject] @{ Category = 'Replication'; Name = 'Tombstone lifetime'
        FailReason = 'tombstone lifetime is 60 days'
        WarnReason = 'tombstone lifetime not set explicitly'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Replication'; Name = 'Lingering objects'
        FailReason = '3 lingering objects in the configuration partition'
        WarnReason = 'strict replication consistency is off'; Weight = 2.0 }
    [pscustomobject] @{ Category = 'DNS'; Name = 'DNS forwarders consistency'
        FailReason = 'forwarders differ from the domain baseline'
        WarnReason = 'one forwarder does not answer'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'DNS'; Name = 'DNS scavenging'
        FailReason = 'scavenging disabled on all zones'
        WarnReason = 'no-refresh interval longer than 7 days'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'DNS'; Name = 'Root hints reachable'
        FailReason = '4 of 13 root servers unreachable'
        WarnReason = 'root hints file older than 2 years'; Weight = 1.6 }
    [pscustomobject] @{ Category = 'DNS'; Name = 'DC locator SRV records'
        FailReason = '_ldap._tcp.dc._msdcs record missing'
        WarnReason = 'stale SRV record for a retired DC'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Time'; Name = 'Time source hierarchy'
        FailReason = 'PDC emulator syncs from the local CMOS clock'
        WarnReason = 'NTP server configured by IP address'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Time'; Name = 'Clock skew'
        FailReason = 'clock skew of 7 minutes against the PDC emulator'
        WarnReason = 'clock skew of 48 seconds'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Kerberos'; Name = 'Kerberos AES-only encryption'
        FailReason = 'RC4 still allowed for Kerberos'; WarnReason = '3 accounts without AES keys'
        Weight = 1.0 }
    [pscustomobject] @{ Category = 'Kerberos'; Name = 'krbtgt password age'
        FailReason = 'krbtgt password not changed for 812 days'
        WarnReason = 'krbtgt password is 190 days old'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Kerberos'; Name = 'Unconstrained delegation'
        FailReason = '2 servers trusted for unconstrained delegation'
        WarnReason = '1 service account trusted for delegation'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Security'; Name = 'Audit policy baseline'
        FailReason = 'advanced audit policy not applied'
        WarnReason = 'logon auditing set to success only'; Weight = 1.3 }
    [pscustomobject] @{ Category = 'Security'; Name = 'Event log sizes'
        FailReason = 'Security log is 20 MB and overwrites in 2 hours'
        WarnReason = 'Directory Service log under 64 MB'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Security'; Name = 'Local administrators'
        FailReason = '4 unexpected members in BUILTIN\Administrators'
        WarnReason = 'nested group in BUILTIN\Administrators'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Security'; Name = 'Firewall profile'
        FailReason = 'domain firewall profile is disabled'
        WarnReason = 'inbound rule allows any remote address'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Security'; Name = 'Remote Desktop exposure'
        FailReason = 'RDP allowed without network level authentication'
        WarnReason = 'RDP open to all internal subnets'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Security'; Name = 'PowerShell logging'
        FailReason = 'script block logging is disabled'; WarnReason = 'transcription not configured'
        Weight = 1.0 }
)

$DomainRules = @(
    [pscustomobject] @{ Category = 'Group Policy'; Name = 'GPO permissions baseline'
        FailReason = 'Authenticated Users can edit 2 GPOs'
        WarnReason = 'a GPO without Authenticated Users read'; Weight = 2.4 }
    [pscustomobject] @{ Category = 'Group Policy'; Name = 'Orphaned GPO folders'
        FailReason = '5 SYSVOL folders without a GPO object'
        WarnReason = '1 GPO object without a SYSVOL folder'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Group Policy'; Name = 'Empty GPOs'
        FailReason = '9 GPOs with no settings are linked'; WarnReason = '3 unlinked empty GPOs'
        Weight = 1.0 }
    [pscustomobject] @{ Category = 'Group Policy'; Name = 'LAPS policy'
        FailReason = 'no LAPS policy applies to member servers'
        WarnReason = 'legacy LAPS still deployed alongside Windows LAPS'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Accounts'; Name = 'Stale computer accounts'
        FailReason = '214 computers inactive for over 180 days'
        WarnReason = '41 computers inactive for over 90 days'; Weight = 1.8 }
    [pscustomobject] @{ Category = 'Accounts'; Name = 'Password never expires'
        FailReason = '18 user accounts never expire their password'
        WarnReason = '2 service accounts never expire their password'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Accounts'; Name = 'Privileged group membership'
        FailReason = 'Domain Admins has 14 members'
        WarnReason = 'a disabled account is still in Enterprise Admins'; Weight = 1.5 }
    [pscustomobject] @{ Category = 'Accounts'; Name = 'AdminSDHolder protected objects'
        FailReason = '6 orphaned adminCount=1 accounts'
        WarnReason = 'AdminSDHolder ACL differs from the default'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Accounts'; Name = 'Service account SPN hygiene'
        FailReason = 'duplicate SPN HTTP/intranet registered twice'
        WarnReason = '3 SPNs on user accounts with old passwords'; Weight = 1.4 }
    [pscustomobject] @{ Category = 'PKI'; Name = 'CA weak key size'
        FailReason = 'issuing CA uses a 1024-bit RSA key'
        WarnReason = 'issuing CA certificate expires in 9 months'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'PKI'; Name = 'Certificate template exposure'
        FailReason = 'template allows SAN from the requester (ESC1)'
        WarnReason = 'template enrollable by Domain Users'; Weight = 1.7 }
    [pscustomobject] @{ Category = 'Kerberos'; Name = 'Constrained delegation overview'
        FailReason = 'protocol transition allowed to DC services'
        WarnReason = 'delegation to CIFS on 2 servers'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Forest'; Name = 'Recycle Bin enabled'
        FailReason = 'AD Recycle Bin is not enabled'
        WarnReason = 'deleted object lifetime is 60 days'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Forest'; Name = 'Site link costs'
        FailReason = 'site link with cost 0 and 15-minute schedule gaps'
        WarnReason = '2 site links use default cost 100'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Forest'; Name = 'Subnets without sites'
        FailReason = '11 client subnets are not mapped to a site'
        WarnReason = '3 overlapping subnet definitions'; Weight = 1.0 }
    [pscustomobject] @{ Category = 'Forest'; Name = 'Functional level'
        FailReason = 'domain functional level is Windows Server 2008 R2'
        WarnReason = 'forest functional level is behind the domains'; Weight = 1.0 }
)
$workers = 6
$totalRules = 2 * $DomainRules.Count + 10 * $DcRules.Count
$targets = @(
    [pscustomobject] @{ Key = 'dom-ad'; Label = 'ad.adatum.com (domain)' }
    [pscustomobject] @{ Key = 'dom-eu'; Label = 'eu.ad.adatum.com (domain)' }
    [pscustomobject] @{ Key = 'dc1'; Label = 'DC01.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc2'; Label = 'DC02.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc3'; Label = 'DC03.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc4'; Label = 'DC04.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc5'; Label = 'DC05.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc6'; Label = 'DC06.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc7'; Label = 'DC07.eu.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc8'; Label = 'DC08.eu.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc9'; Label = 'DC09.eu.ad.adatum.com' }
    [pscustomobject] @{ Key = 'dc10'; Label = 'DC10.eu.ad.adatum.com' }
)
$randomState = @{
    Seed = 2026
    Seeded = $false
    EventOrder = 0
}
$culture = [cultureinfo]::InvariantCulture

# Build timestamped input events, independently of the layout and live session.
function New-AssessmentEvent {
    param(
        $Time,
        $Target,
        $Operation,
        $Key,
        $Text,
        $State,
        $Value = 0,
        $Total = 0
    )

    [pscustomobject]@{ Order = $randomState.EventOrder++
        Time = [Math]::Max(1, [long][Math]::Round($Time)); Target = $Target; Operation = $Operation
        Key = $Key; Text = $Text; State = $State; Value = $Value; Total = $Total }
}
function Get-AssessmentSample {
    if (-not $randomState.Seeded) {
        $randomState.Seeded = $true
        Get-Random -SetSeed $randomState.Seed -Minimum 0.0 -Maximum 1.0
    }
    else {
        Get-Random -Minimum 0.0 -Maximum 1.0
    }
}
function Get-Gaussian {
    $u1 = 1.0 - (Get-AssessmentSample)
    $u2 = (Get-AssessmentSample)
    [Math]::Sqrt(-2.0 * [Math]::Log($u1)) * [Math]::Cos(2.0 * [Math]::PI * $u2)
}
$events = @(
    $assessmentEventParameters = @{
        Time = 0
        Target = 'dashboard'
        Operation = 'status'
        Key = $null
        Text = 'discovering forest'
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 0
        Target = 'phases'
        Operation = 'state'
        Key = 'Discover'
        Text = $null
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    foreach ($target in $targets) {
        $count = if ($target.Key.StartsWith('dom')) { $DomainRules.Count } else { $DcRules.Count }
        $assessmentEventParameters = @{
            Time = 0
            Target = 'lanes'
            Operation = 'add'
            Key = $target.Key
            Text = $target.Label
            State = $null
            Value = 0
            Total = $count
        }
        New-AssessmentEvent @assessmentEventParameters
    }
    $assessmentEventParameters = @{
        Time = 400
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'Connecting to ad.adatum.com via LDAP (DC01.ad.adatum.com)'
        State = 'info'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 1300
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'Forest ad.adatum.com: 2 domains, 10 domain controllers, 4 sites'
        State = 'info'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 2100
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'DC10.eu.ad.adatum.com responded slowly (812 ms LDAP bind)'
        State = 'warn'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 2800
        Target = 'phases'
        Operation = 'state'
        Key = 'Discover'
        Text = $null
        State = 'pass'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 2800
        Target = 'phases'
        Operation = 'state'
        Key = 'Collect'
        Text = $null
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 2800
        Target = 'dashboard'
        Operation = 'status'
        Key = $null
        Text = 'collecting rule inputs'
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 3600
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'Collecting 18 data sources per domain controller'
        State = 'info'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 5200
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'Using cached schema snapshot from 2026-10-15'
        State = 'verbose'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 6800
        Target = 'phases'
        Operation = 'state'
        Key = 'Collect'
        Text = $null
        State = 'pass'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 6800
        Target = 'phases'
        Operation = 'state'
        Key = 'Evaluate'
        Text = $null
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = 6800
        Target = 'dashboard'
        Operation = 'status'
        Key = $null
        Text = "evaluating $totalRules rules on $workers workers"
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $workerClock = @(0..5 | ForEach-Object -Process { 7000 + $_ * 180.0 })
    $completions = @()
    $failures = @()
    $finishes = @()
    $counts = @{ pass = 0; warn = 0; fail = 0; skip = 0 }
    foreach ($target in $targets) {
        $worker = 0
        foreach ($w in 1..5) {
            if ($workerClock[$w] -lt $workerClock[$worker]) {
                $worker = $w
            }
        }
        $t = $workerClock[$worker]
        $domain = $target.Key.StartsWith('dom')
        $rules = if ($domain) { $DomainRules } else { $DcRules }
        $laneFailed = $false
        $assessmentEventParameters = @{
            Time = $t
            Target = 'lanes'
            Operation = 'state'
            Key = $target.Key
            Text = $rules[0].Name
            State = 'running'
        }
        New-AssessmentEvent @assessmentEventParameters
        $assessmentEventParameters = @{
            Time = $t
            Target = 'log'
            Operation = 'write'
            Key = $null
            Text = ($target.Label + ': evaluating ' + $rules.Count + ' rules')
            State = 'info'
        }
        New-AssessmentEvent @assessmentEventParameters
        for ($r = 0; $r -lt $rules.Count; $r++) {
            $rule = $rules[$r]
            $factor = if ($domain) { 1.6 } else { 1.0 }
            $spread = [Math]::Exp(-0.9 + 0.75 * (Get-Gaussian))
            $seconds = [Math]::Round($spread * $rule.Weight * $factor, 9)
            $t += $seconds * 1000
            $roll = (Get-AssessmentSample)
            $result = if ($target.Key -eq 'dc7' -and $rule.Category -eq 'Replication') {
                if ($roll -lt 0.45) { 'fail' } elseif ($roll -lt 0.7) { 'warn' } else { 'pass' }
            }
            else {
                if ($roll -lt 0.045) { 'fail' }
                elseif ($roll -lt 0.15) { 'warn' }
                elseif ($roll -lt 0.19) { 'skip' }
                else { 'pass' }
            }
            $next = if ($r + 1 -lt $rules.Count) { $rules[$r + 1].Name } else { 'finishing' }
            $assessmentEventParameters = @{
                Time = $t
                Target = 'lanes'
                Operation = 'advance'
                Key = $target.Key
                Text = $next
                State = $result
                Value = 1
            }
            New-AssessmentEvent @assessmentEventParameters
            $assessmentEventParameters = @{
                Time = $t
                Target = 'durations'
                Operation = 'add'
                Key = $null
                Text = $null
                State = $null
                Value = $seconds
            }
            New-AssessmentEvent @assessmentEventParameters
            $counts[$result]++
            $completions += $t
            $segment = @{
                pass = 'Passed'
                warn = 'Warnings'
                fail = 'Failed'
                skip = 'Skipped'
            }[$result]
            $assessmentEventParameters = @{
                Time = $t
                Target = 'results'
                Operation = 'add'
                Key = $segment
                Text = $null
                State = $null
                Value = 1
            }
            New-AssessmentEvent @assessmentEventParameters
            if ($result -eq 'warn' -and (Get-AssessmentSample) -lt 0.5) {
                $assessmentEventParameters = @{
                    Time = $t
                    Target = 'log'
                    Operation = 'write'
                    Key = $null
                    Text = ($target.Label + ': ' + $rule.Name + ' - ' + $rule.WarnReason)
                    State = 'warn'
                }
                New-AssessmentEvent @assessmentEventParameters
            }
            if ($result -eq 'fail') {
                $laneFailed = $true
                $failures += $t
                $assessmentEventParameters = @{
                    Time = $t
                    Target = 'findings'
                    Operation = 'add'
                    Key = $rule.Category
                    Text = $null
                    State = $null
                    Value = 1
                }
                New-AssessmentEvent @assessmentEventParameters
                $assessmentEventParameters = @{
                    Time = $t
                    Target = 'log'
                    Operation = 'write'
                    Key = $null
                    Text = ($target.Label + ': ' + $rule.Name + ' failed - ' + $rule.FailReason)
                    State = 'err'
                }
                New-AssessmentEvent @assessmentEventParameters
            }
        }
        $t += 150
        New-AssessmentEvent -Time $t -Target lanes -Operation complete -Key $target.Key
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
        while ($ci -lt $completions.Count -and $completions[$ci] -lt $second) {
            $completed++
            $ci++
        }
        while ($fi -lt $failures.Count -and $failures[$fi] -lt $second) {
            $failed++
            $fi++
        }
        $assessmentEventParameters = @{
            Time = $second
            Target = 'throughput'
            Operation = 'add'
            Key = 'completed'
            Text = $null
            State = $null
            Value = $completed
        }
        New-AssessmentEvent @assessmentEventParameters
        $assessmentEventParameters = @{
            Time = $second
            Target = 'throughput'
            Operation = 'add'
            Key = 'failed'
            Text = $null
            State = $null
            Value = $failed
        }
        New-AssessmentEvent @assessmentEventParameters
    }
    for ($second = 7000; $second -le $evaluateEnd + 1000; $second += 1000) {
        $done = @($completions | Where-Object -FilterScript { $_ -le $second }).Count
        $rate = if ($second -gt 7000) { $done / (($second - 7000) / 1000.0) } else { 0 }
        $remaining = if ($rate -gt 0) { ($totalRules - $done) / $rate } else { 0 }
        $left = if ($done -ge $totalRules) { '-' }
        else { '~' + [Math]::Ceiling($remaining).ToString($culture) + 's' }
        $finished = @($finishes | Where-Object -Property Time -LE -Value $second)
        $assessmentEventParameters = @{
            Time = $second
            Target = 'summary'
            Operation = 'set'
            Key = 'Evaluated'
            Text = "$done / $totalRules"
        }
        New-AssessmentEvent @assessmentEventParameters
        $assessmentEventParameters = @{
            Time = $second
            Target = 'summary'
            Operation = 'set'
            Key = 'Average rate'
        }
        New-AssessmentEvent @assessmentEventParameters -Text ($rate.ToString('0.0',
            $culture) + ' rules/s')
        New-AssessmentEvent -Time $second -Target summary -Operation set -Key Remaining -Text $left
        $assessmentEventParameters = @{
            Time = $second
            Target = 'summary'
            Operation = 'set'
            Key = 'Targets done'
            Text = ($finished.Count.ToString($culture) + ' / 12')
        }
        New-AssessmentEvent @assessmentEventParameters
        $assessmentEventParameters = @{
            Time = $second
            Target = 'summary'
            Operation = 'set'
            Key = 'Targets failing'
            Text = (@($finished | Where-Object -Property Failed).Count.ToString($culture))
        }
        New-AssessmentEvent @assessmentEventParameters
    }
    $level = if ($counts.fail -gt 0) { 'warn' } else { 'pass' }
    $assessmentEventParameters = @{
        Time = $evaluateEnd
        Target = 'phases'
        Operation = 'state'
        Key = 'Evaluate'
        Text = $null
        State = $level
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = $evaluateEnd
        Target = 'phases'
        Operation = 'state'
        Key = 'Report'
        Text = $null
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = $evaluateEnd
        Target = 'dashboard'
        Operation = 'status'
        Key = $null
        Text = 'writing report'
        State = 'running'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = ($evaluateEnd + 900)
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'Report written to Reports/Assessment-ad.adatum.com.html (4.2 MB)'
        State = 'pass'
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = ($evaluateEnd + 1800)
        Target = 'phases'
        Operation = 'state'
        Key = 'Report'
        Text = $null
        State = 'pass'
    }
    New-AssessmentEvent @assessmentEventParameters
    $final = "$totalRules rules · $($counts.pass) passed · $($counts.fail) failed · $($counts.warn) warnings · $($counts.skip) skipped"
    $assessmentEventParameters = @{
        Time = ($evaluateEnd + 1800)
        Target = 'dashboard'
        Operation = 'status'
        Key = $null
        Text = $final
        State = $level
    }
    New-AssessmentEvent @assessmentEventParameters
    $assessmentEventParameters = @{
        Time = ($evaluateEnd + 4500)
        Target = 'log'
        Operation = 'write'
        Key = $null
        Text = 'Done.'
        State = 'info'
    }
    New-AssessmentEvent @assessmentEventParameters
)
# Keep equal timestamps in their original order on both PowerShell runtimes.
$events = @($events | Sort-Object -Property Time, Order)
# Layout: start with empty counters, lanes, charts and logs; native events populate them.
$results = [ordered] @{
    Passed = 0
    Warnings = 0
    Failed = 0
    Skipped = 0
}
$summary = [ordered] @{
    Evaluated = "0 / $totalRules"
    'Average rate' = '-'
    Remaining = '-'
    'Targets done' = '0 / 12'
    'Targets failing' = '0'
}
$findings = [ordered] @{
    'Domain controllers' = 0
    Replication = 0
    DNS = 0
    Kerberos = 0
    Security = 0
    'Group Policy' = 0
    Accounts = 0
    PKI = 0
}
$lanes = New-ConsoleLanes
$dashboardParameters = @{
    Title = '[bold]Nightly checks[/]'
    Subtitle = 'ad.adatum.com forest assessment · 12 targets · 332 rules'
    Footer = '[muted]Discover · Collect · Evaluate · Report    Ctrl+C stop[/]'
}
$dashboard = New-ConsoleDashboard @dashboardParameters -Content {
    New-ConsolePadding -Padding 1, 0, 1, 1 -Content {
        New-ConsolePhases -Content {
            foreach ($phase in 'Discover', 'Collect', 'Evaluate', 'Report') {
                [pscustomobject]@{ Label = $phase; Status = 'Pending' }
            }
        }
    }
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title Targets -HeaderRight '6 workers' -Weight 2.3 -Content { $lanes }
        New-ConsolePanel -Title Results -Content {
            New-ConsoleChart -WidgetId results -InputObject $results -Type Stacked
            New-ConsoleRule
            New-ConsoleKeyValue -WidgetId summary -InputObject $summary
        }
    }
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title Throughput -HeaderRight 'rules/s' -Weight 1.2 -Content {
            New-ConsoleChart -WidgetId throughput -Height 7 -Content {
                [pscustomobject]@{ Key = 'completed'; Role = 'Accent' }
                [pscustomobject]@{ Key = 'failed'; Role = 'Fail' }
            } -Type Line
        }
        New-ConsolePanel -Title 'Rule duration' -Content {
            New-ConsoleChart -WidgetId durations -Height 6 -Unit s -Type Histogram -Edges 0, 0.25,
                0.5, 1, 2, 4, 8
        }
        New-ConsolePanel -Title 'Failures by category' -Content {
            New-ConsoleChart -WidgetId findings -Role Fail -InputObject $findings -Type Bar
        }
    }
    New-ConsolePanel -Title Log -Fill -Content {
        New-ConsoleLogRegion -Height 6
    }
}
$dashboard.ShowElapsed = $false

# Behaviour: native events update individual widgets; the complete run is bounded and paced at 4x.
Invoke-ConsoleDashboard -Dashboard $dashboard -ArgumentList (, $events) -ScriptBlock {
    param($Events)
    $previous = 0
    foreach ($event in $Events) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) {
            break
        }
        $delay = [int][Math]::Truncate(($event.Time - $previous) / 4)
        $fields = @{
            WidgetId = $event.Target
            Operation = $event.Operation
            Value = $event.Value
            Total = $event.Total
            TimeMilliseconds = $event.Time
        }
        if ($null -ne $event.Key) {
            $fields.Key = $event.Key
        }
        if ($null -ne $event.Text) {
            $fields.Text = $event.Text
        }
        if ($null -ne $event.State) {
            $fields.Status = $event.State
        }
        Send-ConsoleEvent @fields
        Wait-ConsoleDashboard -Milliseconds $delay
        $previous = $event.Time
    }
}
