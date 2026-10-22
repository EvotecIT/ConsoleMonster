Describe 'Parallel live stream routing' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:ConcurrencyRuntime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:ConcurrencyModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:ConcurrencyProbe = Join-Path $PSScriptRoot 'Support/ParallelStreamProbe.cs'
    }

    It 'preserves multiline host and warning messages without a log in <Mode> with <LineEnding>' -ForEach @(
        @{ Mode = 'Stream'; LineEnding = 'LF' }
        @{ Mode = 'Stream'; LineEnding = 'CRLF' }
        @{ Mode = 'Interactive'; LineEnding = 'LF' }
        @{ Mode = 'Interactive'; LineEnding = 'CRLF' }
    ) {
        $body = @'
$target = [ConsoleMonster.Output.RecordingTarget]::new([ConsoleMonster.Capabilities.TerminalProfile]::Fixed(80, 20, 'None', 'Full', '__MODE__'))
$scope = [ConsoleMonster.Terminal]::Use($target)
try {
    $newline = if ('__LINE_ENDING__' -eq 'CRLF') { "`r`n" } else { "`n" }
    $dashboard = New-ConsoleDashboard -Title 'No log' { New-ConsoleText 'working' }
    1..2 | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 2 -ArgumentList $newline {
        param($item, $newline)
        Write-Host "host-$item-first${newline}host-$item-second"
        Write-Warning "warning-$item-first${newline}warning-$item-second"
        Write-Host "colour-$item-first${newline}colour-$item-second" -ForegroundColor Cyan
    } | Out-Null
    $text = $target.Text
    foreach ($item in 1..2) {
        foreach ($prefix in 'host', 'warning', 'colour') {
            $expected = "$prefix-$item-first`n$prefix-$item-second`n"
            if ([regex]::Matches($text, [regex]::Escape($expected)).Count -ne 1) { throw "The $prefix message lost its lines or was duplicated: $text" }
        }
    }
} finally {
    $scope.Dispose()
}
'MULTILINE_NO_LOG_OK'
'@.Replace('__MODE__', $Mode).Replace('__LINE_ENDING__', $LineEnding)
        $setup = "Import-Module '$($script:ConcurrencyModule.Replace("'", "''"))' -Force`n$body"
        $setup = "`$ErrorActionPreference = 'Stop'; try {`n$setup`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }"
        $lines = @(Invoke-TestChild -Runtime $script:ConcurrencyRuntime -Script $setup -TimeoutSeconds 180)
        if ($LASTEXITCODE -ne 0) { throw ($lines -join "`n") }
        $lines -join "`n" | Should -Match 'MULTILINE_NO_LOG_OK'
    }

    It 'routes host, warning and nested progress from <CommandName> without changing result records' -ForEach @(
        @{ CommandName = 'Start-ConsoleDashboard' }
        @{ CommandName = 'ForEach-ConsoleItem' }
        @{ CommandName = 'Invoke-ConsoleRun' }
    ) {
        $setup = @(
            '$ErrorActionPreference = ''Stop'''
            "Import-Module '$($script:ConcurrencyModule.Replace("'", "''"))' -Force"
            '$references = @([ConsoleMonster.Terminal].Assembly.Location)'
            'if ($PSEdition -eq ''Core'') { $references += @(Get-ChildItem (Join-Path $PSHOME ''ref'') -Filter *.dll | ForEach-Object { $_.FullName }) } else { $references += ''System.Core'' }'
            # The development loader deliberately uses the net8 binary on newer PowerShell runtimes too.
            '$compile = @{}; if ($PSEdition -eq ''Core'') { $compile.CompilerOptions = ''/nowarn:1701'' }'
            '$assemblyPath = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString(''N'') + ''.dll'')'
            "Add-Type -Path '$($script:ConcurrencyProbe.Replace("'", "''"))' -ReferencedAssemblies $" + 'references -OutputAssembly $assemblyPath @compile'
            @'
try {
    $bytes = [IO.File]::ReadAllBytes($assemblyPath)
    if ($PSEdition -eq 'Core') {
        # The fixture must share the module's development load context so ILiveTarget has the same type identity.
        $context = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext([ConsoleMonster.Terminal].Assembly)
        $stream = [IO.MemoryStream]::new($bytes)
        try { $assembly = $context.LoadFromStream($stream) } finally { $stream.Dispose() }
    } else {
        $assembly = [Reflection.Assembly]::Load($bytes)
    }
    $probe = $assembly.GetType('ParallelStreamProbe', $true)
} finally {
    Remove-Item -LiteralPath $assemblyPath -ErrorAction Stop
}
'@
            @'
$target = [ConsoleMonster.Output.RecordingTarget]::new([ConsoleMonster.Capabilities.TerminalProfile]::Fixed(120, 40, 'None'))
$scope = [ConsoleMonster.Terminal]::Use($target)
try {
    $body = {
        param($item, $probe)
        __PROBE_SETUP__
        if ($null -eq $item) { $item = $_ }
        $probe::Attach()
        Write-Host "host-$item" -ForegroundColor Cyan
        Write-Warning "warn-$item"
        Write-Progress -Id 0 -Activity "parent-$item" -PercentComplete 20
        Write-Progress -Id 1 -ParentId 0 -Activity "child-$item" -PercentComplete 60
        $probe::Observe()
        # Completing a parent removes its child; an unfinished pipeline's activities are cleared on disposal too.
        if ($item % 2 -eq 0) { Write-Progress -Id 0 -Activity "parent-$item" -Completed }
        "result-$item"
    }
    $warnings = @()
    if ('__COMMAND__' -eq 'Start-ConsoleDashboard') {
        $dashboard = New-ConsoleDashboard -Title 'Parallel streams' { New-ConsoleLog }
        $results = @(1..4 | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 4 $body -ArgumentList $probe)
    } else {
        $results = @(1..4 | __COMMAND__ -Parallel -ThrottleLimit 4 $body -WarningVariable warnings -WarningAction SilentlyContinue)
    }
    $report = [pscustomobject]@{
        Results = $results
        Warnings = @($warnings | ForEach-Object { $_.ToString() })
        Logs = $probe::LogLines()
        MaxActivities = $probe::MaxActivities
        ActivityFrame = $probe::ActivityFrame
        RemainingActivities = $probe::Activities.Count
        Dropped = $probe::Dropped
        Errors = $probe::Errors
    }
} finally {
    $scope.Dispose()
}
'PROBE:' + ($report | ConvertTo-Json -Depth 5 -Compress)
'@.Replace('__COMMAND__', $CommandName).Replace('__PROBE_SETUP__', $(if ($CommandName -eq 'Start-ConsoleDashboard') { '' } else { '$probe = $using:probe' }))
        ) -join "`n"
        $setup = "try {`n$setup`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }"
        $lines = @(Invoke-TestChild -Runtime $script:ConcurrencyRuntime -Script $setup -TimeoutSeconds 180)
        if ($LASTEXITCODE -ne 0) { throw ($lines -join "`n") }
        $LASTEXITCODE | Should -Be 0 -Because ($lines -join "`n")
        $json = @($lines | Where-Object { $_ -like 'PROBE:*' })
        $json.Count | Should -Be 1 -Because ($lines -join "`n")
        $report = $json[0].Substring(6) | ConvertFrom-Json
        ($report.Results | Sort-Object) -join ',' | Should -Be 'result-1,result-2,result-3,result-4'
        if ($CommandName -ne 'Start-ConsoleDashboard') {
            $report.Results -join ',' | Should -Be 'result-1,result-2,result-3,result-4'
            ($report.Warnings | Sort-Object) -join ',' | Should -Be 'warn-1,warn-2,warn-3,warn-4'
        }
        ($report.Logs | Sort-Object) -join ',' | Should -Be 'host-1,host-2,host-3,host-4,warn-1,warn-2,warn-3,warn-4'
        $report.MaxActivities | Should -Be 8
        foreach ($item in 1..4) {
            $report.ActivityFrame | Should -Match "parent-$item"
            $report.ActivityFrame | Should -Match "child-$item"
        }
        $report.RemainingActivities | Should -Be 0
        $report.Dropped | Should -Be 0
        $report.Errors | Should -Be 0
    }
}
