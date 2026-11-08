# Trusted summary scripts: the facts line under a built-in view on the console. Each gets the objects shown together and
# returns facts (a string, or a hashtable with Value, Label, Caption, Presenter, Role and NewLine). Small results return nothing.
# Language features only, no commands: a user's function of the same name never runs, and large listings stay fast.

# Get-ChildItem: folders, files, the size of the files and the largest one.
@{
    TypeName = @('System.IO.FileInfo', 'System.IO.DirectoryInfo')
    Summary = {
        param($items)
        if ($items.Count -lt 3) { return }
        $folders = 0; $files = 0; [long] $bytes = 0; $largest = $null
        foreach ($item in $items) {
            $value = $item.PSObject.BaseObject
            if ($value -is [System.IO.DirectoryInfo]) { $folders++; continue }
            $files++
            if (($value.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { continue }
            $bytes += $value.Length
            if ($null -eq $largest -or $value.Length -gt $largest.Length) { $largest = $value }
        }
        if ($folders) { @{ Value = $folders; Label = $(if ($folders -eq 1) { 'folder' } else { 'folders' }) } }
        if ($files) {
            @{ Value = $files; Label = $(if ($files -eq 1) { 'file' } else { 'files' }) }
            @{ Value = $bytes; Presenter = 'Bytes' }
            if ($files -gt 1 -and $largest) { @{ Caption = 'largest'; Value = $largest.Name } }
        }
    }
}

# Get-Process: how many, their memory together and the one using the most.
@{
    TypeName = 'System.Diagnostics.Process'
    Summary = {
        param($items)
        if ($items.Count -lt 5) { return }
        [long] $memory = 0; $top = $null
        # Through the wrapper, so deserialized processes (remoting, jobs) sum up the same way.
        foreach ($item in $items) {
            $memory += $item.WorkingSet64
            if ($null -eq $top -or $item.WorkingSet64 -gt $top.WorkingSet64) { $top = $item }
        }
        @{ Value = $items.Count; Label = 'processes' }
        @{ Value = $memory; Presenter = 'Bytes'; Label = 'memory' }
        if ($top) { @{ Caption = 'most memory'; Value = $top.ProcessName } }
    }
}

# Get-Service: running and stopped, and automatic services that are not running (the usual thing to look for).
@{
    TypeName = 'System.ServiceProcess.ServiceController'
    Summary = {
        param($items)
        if ($items.Count -lt 5) { return }
        $running = 0; $stopped = 0; $other = 0; $automatic = 0
        foreach ($item in $items) {
            $status = [string] $item.Status
            if ($status -eq 'Running') { $running++ } elseif ($status -eq 'Stopped') { $stopped++ } else { $other++ }
            if ($status -ne 'Running' -and [string] $item.StartType -eq 'Automatic') { $automatic++ }
        }
        if ($running) { @{ Value = $running; Label = 'running'; Role = 'Pass' } }
        if ($stopped) { @{ Value = $stopped; Label = 'stopped' } }
        if ($other) { @{ Value = $other; Label = 'starting or stopping' } }
        if ($automatic) { @{ Value = $automatic; Label = 'automatic not running'; Role = 'Warn' } }
    }
}

# Get-WinEvent: events by level, the time they cover and when they happened.
@{
    TypeName = 'System.Diagnostics.Eventing.Reader.EventLogRecord'
    Summary = {
        param($items)
        if ($items.Count -lt 3) { return }
        $levels = [ordered]@{}
        $first = $null; $last = $null
        foreach ($item in $items) {
            $level = [string] $item.LevelDisplayName
            if (-not $level) { $level = 'Information' }
            if ($levels.Contains($level)) { $levels[$level]++ } else { $levels[$level] = 1 }
            $time = $item.TimeCreated
            if ($time) {
                if ($null -eq $first -or $time -lt $first) { $first = $time }
                if ($null -eq $last -or $time -gt $last) { $last = $time }
            }
        }
        foreach ($name in 'Critical', 'Error', 'Warning', 'Information', 'Verbose') {
            if ($levels.Contains($name)) {
                $count = $levels[$name]
                $role = switch ($name) { 'Critical' { 'Fail' } 'Error' { 'Fail' } 'Warning' { 'Warn' } default { $null } }
                $label = switch ($name) {
                    'Critical' { 'critical' }
                    'Error' { if ($count -eq 1) { 'error' } else { 'errors' } }
                    'Warning' { if ($count -eq 1) { 'warning' } else { 'warnings' } }
                    'Information' { 'information' }
                    default { 'verbose' }
                }
                @{ Value = $count; Label = $label; Role = $role }
                $levels.Remove($name)
            }
        }
        foreach ($name in @($levels.Keys)) { @{ Value = $levels[$name]; Label = $name.ToLowerInvariant() } }
        if ($first -and $last -and $last -gt $first) {
            $span = $last - $first
            @{ Caption = 'over'; Value = $span; Presenter = 'Duration' }
            # Events per slot across the time they cover, oldest on the left.
            $slots = [Math]::Min(24, [Math]::Max(2, $items.Count))
            $counts = [double[]]::new($slots)
            foreach ($item in $items) {
                if ($item.TimeCreated) {
                    $slot = [int][Math]::Floor(($item.TimeCreated - $first).Ticks / ($span.Ticks / $slots))
                    $counts[[Math]::Min($slots - 1, $slot)]++
                }
            }
            @{ Value = $counts; Presenter = 'Sparkline' }
        }
    }
}

# Test-Connection: per destination, the replies, any loss and the latency with its shape.
@{
    TypeName = 'Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus'
    Summary = {
        param($items)
        if ($items.Count -lt 2) { return }
        $destinations = [ordered]@{}
        foreach ($item in $items) {
            $name = [string] $item.Destination
            if (-not $destinations.Contains($name)) { $destinations[$name] = [System.Collections.Generic.List[object]]::new() }
            $destinations[$name].Add($item)
        }
        foreach ($name in $destinations.Keys) {
            $pings = $destinations[$name]
            $latencies = [System.Collections.Generic.List[double]]::new()
            foreach ($ping in $pings) { if ([string] $ping.Status -eq 'Success') { $latencies.Add([double] $ping.Latency) } }
            # Each destination on its own line when there are several.
            @{ Caption = $(if ($destinations.Count -gt 1) { $name }); NewLine = $destinations.Count -gt 1
                Value = "$($latencies.Count)/$($pings.Count)"; Label = 'replies'; Role = $(if ($latencies.Count -eq 0) { 'Fail' } elseif ($latencies.Count -lt $pings.Count) { 'Warn' } else { 'Pass' }) }
            if ($latencies.Count) {
                $min = $latencies[0]; $max = $latencies[0]; $sum = 0
                foreach ($latency in $latencies) { $sum += $latency; if ($latency -lt $min) { $min = $latency }; if ($latency -gt $max) { $max = $latency } }
                @{ Caption = 'min'; Value = $min; Label = 'ms' }
                @{ Caption = 'avg'; Value = [Math]::Round($sum / $latencies.Count, 1); Label = 'ms' }
                @{ Caption = 'max'; Value = $max; Label = 'ms' }
                if ($latencies.Count -gt 2) { @{ Value = $latencies.ToArray(); Presenter = 'Sparkline' } }
            }
        }
    }
}

# Get-History: how many commands, how many failed, the time they took and the slowest.
@{
    TypeName = 'Microsoft.PowerShell.Commands.HistoryInfo'
    Summary = {
        param($items)
        if ($items.Count -lt 3) { return }
        $failed = 0; $total = [TimeSpan]::Zero; $slowest = $null; $slowestTime = [TimeSpan]::Zero
        foreach ($item in $items) {
            if ([string] $item.ExecutionStatus -eq 'Failed') { $failed++ }
            $time = $item.EndExecutionTime - $item.StartExecutionTime
            $total += $time
            if ($time -gt $slowestTime) { $slowestTime = $time; $slowest = $item }
        }
        @{ Value = $items.Count; Label = 'commands' }
        if ($failed) { @{ Value = $failed; Label = 'failed'; Role = 'Fail' } }
        @{ Caption = 'took'; Value = $total; Presenter = 'Duration' }
        if ($slowest) { @{ Caption = 'slowest'; Value = $slowestTime; Presenter = 'Duration'; Label = "(#$($slowest.Id))" } }
    }
}

# Certificates: how many, and how many have expired or expire within 30 days (the same rule as the Status column).
@{
    TypeName = 'System.Security.Cryptography.X509Certificates.X509Certificate2'
    Summary = {
        param($items)
        if ($items.Count -lt 3) { return }
        $now = [datetime]::Now; $expired = 0; $expiring = 0
        foreach ($item in $items) {
            if ($item.NotAfter -lt $now) { $expired++ } elseif ($item.NotAfter -lt $now.AddDays(30)) { $expiring++ }
        }
        @{ Value = $items.Count; Label = 'certificates' }
        if ($expired) { @{ Value = $expired; Label = 'expired'; Role = 'Fail' } }
        if ($expiring) { @{ Value = $expiring; Label = 'expiring within 30 days'; Role = 'Warn' } }
    }
}

# Select-String: how many matches in how many files.
@{
    TypeName = 'Microsoft.PowerShell.Commands.MatchInfo'
    Summary = {
        param($items)
        if ($items.Count -lt 3) { return }
        $files = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($item in $items) { [void] $files.Add([string] $item.Path) }
        @{ Value = $items.Count; Label = 'matches' }
        if ($files.Count -gt 1) { @{ Caption = 'in'; Value = $files.Count; Label = 'files' } }
    }
}

# Get-Job: jobs by state.
@{
    TypeName = 'System.Management.Automation.Job'
    Summary = {
        param($items)
        if ($items.Count -lt 3) { return }
        $states = [ordered]@{}
        foreach ($item in $items) {
            $state = [string] $item.State
            if ($states.Contains($state)) { $states[$state]++ } else { $states[$state] = 1 }
        }
        foreach ($state in $states.Keys) {
            $role = switch ($state) { 'Completed' { 'Pass' } 'Failed' { 'Fail' } { $_ -in 'Stopped', 'Blocked', 'Suspended', 'Disconnected' } { 'Warn' } default { $null } }
            @{ Value = $states[$state]; Label = $state.ToLowerInvariant(); Role = $role }
        }
    }
}
