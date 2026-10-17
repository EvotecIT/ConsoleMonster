namespace ConsoleMonster.PowerShell.Pester {
    /// <summary>
    /// The PowerShell side of Invoke-ConsolePester. It imports Pester lazily, builds the configuration, checks that the
    /// Pester internals the adapter relies on are present, and either runs Pester with the adapter in place of Pester's
    /// screen output plugin or runs Pester unchanged. Events reach the C# <see cref="PesterBridge"/> as plain values.
    /// Pester has no public reporter API (see Docs/Pester-Upstream-Proposal.md), so the adapter:
    /// <list type="bullet">
    /// <item>uses only the documented configuration and the plugin step names shared by Pester 5 and 6 (DiscoveryEnd,
    /// ContainerRunStart, EachTestSetupStart, EachTestTeardownEnd, ContainerRunEnd);</item>
    /// <item>creates its plugin with Pester's own New-PluginObject, so the object has the shape Pester expects;</item>
    /// <item>replaces Get-WriteScreenPlugin inside the Pester module only for the duration of one Invoke-Pester call, and
    /// restores it in a finally block (also when the run is stopped);</item>
    /// <item>falls back to an unchanged Pester run, with a reason, when any of these checks fails.</item>
    /// </list>
    /// </summary>
    internal static class PesterAdapterScript {
        public const string Text = @"
param($Bridge, $Options)
# The tests run under the caller's error preference; only the set-up below stops on errors.
$callerErrorAction = $ErrorActionPreference
$ErrorActionPreference = 'Stop'

# Pester, imported lazily: a loaded Pester 5+ wins, otherwise the newest installed one.
$pester = Get-Module -Name Pester | Where-Object { $_.Version.Major -ge 5 } | Sort-Object -Property Version -Descending | Select-Object -First 1
if (-not $pester) {
    $candidate = Get-Module -Name Pester -ListAvailable | Where-Object { $_.Version.Major -ge 5 } | Sort-Object -Property Version -Descending | Select-Object -First 1
    if (-not $candidate) {
        throw [System.Management.Automation.ItemNotFoundException]::new('Invoke-ConsolePester needs Pester 5 or later, and none is installed. Install it with: Install-Module Pester -Scope CurrentUser -SkipPublisherCheck')
    }
    $pester = Import-Module -ModuleInfo $candidate -PassThru -Global -ErrorAction Stop
}
$invokePester = $pester.ExportedCommands['Invoke-Pester']
$newConfiguration = $pester.ExportedCommands['New-PesterConfiguration']

# The configuration: -Configuration (a PesterConfiguration or a hashtable), or the simple parameters as Invoke-Pester maps them.
if ($null -ne $Options.Configuration) {
    $given = $Options.Configuration
    if ($given -is [System.Management.Automation.PSObject]) { $given = $given.BaseObject }
    if ($given -is [System.Collections.IDictionary]) {
        $configuration = & $newConfiguration -Hashtable $given
    } else {
        $configuration = [PesterConfiguration]::Merge($given, (& $newConfiguration))
    }
} else {
    $configuration = & $newConfiguration
    $simple = $Options.Simple
    if ($simple.ContainsKey('Path')) { $configuration.Run.Path = [string[]]$simple['Path'] }
    if ($simple.ContainsKey('ExcludePath')) { $configuration.Run.ExcludePath = [string[]]$simple['ExcludePath'] }
    if ($simple.ContainsKey('Container')) { $configuration.Run.Container = $simple['Container'] }
    if ($simple.ContainsKey('TagFilter')) { $configuration.Filter.Tag = [string[]]$simple['TagFilter'] }
    if ($simple.ContainsKey('ExcludeTagFilter')) { $configuration.Filter.ExcludeTag = [string[]]$simple['ExcludeTagFilter'] }
    if ($simple.ContainsKey('FullNameFilter')) { $configuration.Filter.FullName = [string[]]$simple['FullNameFilter'] }
    if ($simple.ContainsKey('Output')) { $configuration.Output.Verbosity = [string]$simple['Output'] }
    if ($simple.ContainsKey('PassThru')) { $configuration.Run.PassThru = [bool]$simple['PassThru'] }
    if ($simple.ContainsKey('CI') -and $simple['CI']) {
        $configuration.Run.Exit = $true
        $configuration.TestResult.Enabled = $true
    }
}

function Get-OptionValue($section, $name, $default) {
    if ($section.PSObject.Properties[$name]) { return $section.$name.Value }
    return $default
}
$exitRequested = [bool](Get-OptionValue $configuration.Run 'Exit' $false)
$throwRequested = [bool](Get-OptionValue $configuration.Run 'Throw' $false)
$passThru = [bool](Get-OptionValue $configuration.Run 'PassThru' $false)
$verbosity = [string](Get-OptionValue $configuration.Output 'Verbosity' 'Normal')
$parallel = [bool](Get-OptionValue $configuration.Run 'Parallel' $false)

# Feature detection: the adapter runs only when every internal it relies on is present.
$reason = $null
if ($verbosity -eq 'None') {
    $reason = 'Output is None'
} elseif ($parallel) {
    $reason = 'Run.Parallel runs test files in worker runspaces, whose test events the adapter cannot follow'
} elseif ($Options.Fallback) {
    $reason = 'the adapter was turned off'
} else {
    $reason = & $pester {
        foreach ($name in 'New-PluginObject', 'Get-WriteScreenPlugin') {
            if (-not (Get-Command -Name $name -CommandType Function -ErrorAction SilentlyContinue)) { return ""Pester internals changed: $name was not found"" }
        }
        $parameters = (Get-Command -Name New-PluginObject -CommandType Function).Parameters
        foreach ($step in 'DiscoveryEnd', 'ContainerRunStart', 'EachTestSetupStart', 'EachTestTeardownEnd', 'ContainerRunEnd') {
            if (-not $parameters.ContainsKey($step)) { return ""Pester internals changed: the plugin step $step was not found"" }
        }
        if (-not (Get-Command -Name Get-WriteScreenPlugin -CommandType Function).Parameters.ContainsKey('Verbosity')) { return 'Pester internals changed: Get-WriteScreenPlugin has no Verbosity parameter' }
        $null
    }
    if (-not $reason -and -not $invokePester.ScriptBlock.ToString().Contains('Get-WriteScreenPlugin')) {
        $reason = 'Pester internals changed: Invoke-Pester no longer calls Get-WriteScreenPlugin'
    }
}
$adapter = -not $reason
$Bridge.Begin($pester.Version.ToString(), $adapter, $reason, ($verbosity -eq 'None'))

$override = & $newConfiguration
$override.Run.PassThru = $true
if ($override.Run.PSObject.Properties['Exit']) { $override.Run.Exit = $false }
if ($override.Run.PSObject.Properties['Throw']) { $override.Run.Throw = $false }
if ($adapter) { $override.Output.Verbosity = 'Normal' }
$configuration = [PesterConfiguration]::Merge($configuration, $override)

# Helpers (iterative and self-contained: they run from plugin steps inside Pester).
$keyOf = {
    param($item)
    if ($item -is [System.IO.FileInfo]) { return @($item.FullName, $item.Name, $item.FullName) }
    if ($item -is [string]) { return @($item, [System.IO.Path]::GetFileName($item), $item) }
    $id = 'ScriptBlock ' + [System.Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($item)
    return @($id, 'ScriptBlock', $null)
}
$countTests = {
    param($block)
    $count = 0
    $stack = New-Object System.Collections.Stack
    $stack.Push($block)
    while ($stack.Count -gt 0) {
        $b = $stack.Pop()
        foreach ($t in @($b.Tests)) { if ($null -ne $t -and $t.ShouldRun) { $count++ } }
        foreach ($c in @($b.Blocks)) { if ($null -ne $c) { $stack.Push($c) } }
    }
    $count
}
$blockNames = {
    param($block)
    $names = New-Object System.Collections.Generic.List[string]
    $b = $block
    while ($null -ne $b -and $b.PSObject.Properties['IsRoot'] -and -not $b.IsRoot) { $names.Insert(0, [string]$b.ExpandedName); $b = $b.Parent }
    , $names.ToArray()
}
$pesterBase = $pester.ModuleBase
# The failing line: the assertion's file and line, else where the error was raised (outside Pester), else the test or block.
$errorInfo = {
    param($record, $file, $line)
    $target = $record.TargetObject
    if ($target -is [System.Collections.IDictionary] -and $target['File']) {
        $file = [string]$target['File']
        $line = [int]$target['Line']
    } elseif ($null -ne $record.InvocationInfo -and $record.InvocationInfo.ScriptName -and -not $record.InvocationInfo.ScriptName.StartsWith($pesterBase, [System.StringComparison]::OrdinalIgnoreCase)) {
        $file = $record.InvocationInfo.ScriptName
        $line = $record.InvocationInfo.ScriptLineNumber
    }
    $type = $null
    if (-not ($target -is [System.Collections.IDictionary] -and $target['File']) -and $record.Exception -isnot [System.Management.Automation.RuntimeException]) { $type = $record.Exception.GetType().Name }
    @([string]$record.Exception.Message, $file, [int]$line, $type)
}.GetNewClosure()
# Reports a container's tree. All tests ($all) after a plain run, or only the tests that never ran (their block failed) after
# an adapter run; block failures are reported once, at the block that failed.
$walk = {
    param($Bridge, $key, $name, $root, $all)
    $stack = New-Object System.Collections.Stack
    $stack.Push(@($root, $false))
    while ($stack.Count -gt 0) {
        $pair = $stack.Pop()
        $b = $pair[0]
        $failedAbove = [bool]$pair[1]
        $errors = @($b.ErrorRecord | Where-Object { $null -ne $_ })
        if ($errors.Count -gt 0) {
            $info = & $errorInfo $errors[0] $b.ScriptBlock.File $b.StartLine
            $isRoot = $b -eq $root -or ($b.PSObject.Properties['IsRoot'] -and $b.IsRoot)
            $blockName = if ($isRoot) { $name } else { [string]$b.ExpandedName }
            $parents = if ($isRoot) { [string[]]@() } else { & $blockNames $b.Parent }
            $Bridge.BlockFailed($key, $parents, $blockName, $info[0], $info[1], $info[2])
            $failedAbove = $true
        }
        foreach ($t in @($b.Tests)) {
            if ($null -eq $t -or -not $t.ShouldRun) { continue }
            if ($all -and $t.Executed) {
                $message = $null
                $type = $null
                $file = $t.ScriptBlock.File
                $line = $t.StartLine
                $testErrors = @($t.ErrorRecord | Where-Object { $null -ne $_ })
                if ($testErrors.Count -gt 0) { $info = & $errorInfo $testErrors[0] $file $line; $message = $info[0]; $file = $info[1]; $line = $info[2]; $type = $info[3] }
                $ms = 0
                if ($null -ne $t.Duration) { $ms = $t.Duration.TotalMilliseconds }
                $Bridge.TestFinished($key, (& $blockNames $b), [string]$t.ExpandedName, [string]$t.Result, $ms, $message, $file, [int]$line, $type)
            } elseif (-not $t.Executed) {
                $result = if ($failedAbove) { 'Failed' } else { 'NotRun' }
                $Bridge.TestFinished($key, (& $blockNames $b), [string]$t.ExpandedName, $result, 0, $null, $t.ScriptBlock.File, [int]$t.StartLine, $null)
            }
        }
        foreach ($c in @($b.Blocks)) { if ($null -ne $c) { $stack.Push(@($c, $failedAbove)) } }
    }
}.GetNewClosure()

$state = @{ Container = $null; Name = $null; Seen = @{} }
if ($adapter) {
    # The test files, found up front the way Pester finds them, show as queued lanes until they run.
    try {
        $extension = [string](Get-OptionValue $configuration.Run 'TestExtension' '.Tests.ps1')
        $excluded = @(Get-OptionValue $configuration.Run 'ExcludePath' @())
        foreach ($path in @(Get-OptionValue $configuration.Run 'Path' @())) {
            if (-not $path) { continue }
            foreach ($resolved in @(Resolve-Path -Path $path -ErrorAction SilentlyContinue)) {
                $files = if (Test-Path -LiteralPath $resolved.ProviderPath -PathType Container) {
                    Get-ChildItem -LiteralPath $resolved.ProviderPath -Recurse -File -Filter ('*' + $extension) -ErrorAction SilentlyContinue | Sort-Object -Property FullName
                } else { Get-Item -LiteralPath $resolved.ProviderPath }
                foreach ($file in @($files)) {
                    $skip = $false
                    foreach ($pattern in $excluded) { if ($pattern -and $file.FullName -like $pattern) { $skip = $true } }
                    if (-not $skip) { $Bridge.ContainerQueued($file.FullName, $file.Name, $file.FullName, 0) }
                }
            }
        }
    } catch { $Bridge.Fault('queue', $_.Exception.Message) }
    $steps = @{
        ContainerDiscoveryEnd = {
            param($Context)
            try {
                $info = & $keyOf $Context.BlockContainer.Item
                $Bridge.ContainerQueued($info[0], $info[1], $info[2], (& $countTests $Context.Block))
            } catch { $Bridge.Fault('ContainerDiscoveryEnd', $_.Exception.Message) }
        }.GetNewClosure()
        DiscoveryEnd = {
            param($Context)
            try {
                $total = 0
                foreach ($root in @($Context.BlockContainers)) { if ($null -ne $root) { $total += & $countTests $root } }
                $Bridge.Discovered($total)
            } catch { $Bridge.Fault('DiscoveryEnd', $_.Exception.Message) }
        }.GetNewClosure()
        ContainerRunStart = {
            param($Context)
            try {
                $info = & $keyOf $Context.Block.BlockContainer.Item
                $state.Container = $info[0]
                $state.Name = $info[1]
                $state.Seen[$info[0]] = $true
                $Bridge.ContainerStarted($info[0], $info[1], $info[2], (& $countTests $Context.Block))
            } catch { $Bridge.Fault('ContainerRunStart', $_.Exception.Message) }
        }.GetNewClosure()
        EachTestSetupStart = {
            param($Context)
            try {
                $t = $Context.Test
                $Bridge.TestStarted($state.Container, (& $blockNames $t.Block), [string]$t.ExpandedName)
            } catch { $Bridge.Fault('EachTestSetupStart', $_.Exception.Message) }
        }.GetNewClosure()
        EachTestTeardownEnd = {
            param($Context)
            try {
                $t = $Context.Test
                $message = $null
                $type = $null
                $file = $t.ScriptBlock.File
                $line = $t.StartLine
                $errors = @($t.ErrorRecord | Where-Object { $null -ne $_ })
                if ($errors.Count -gt 0) { $info = & $errorInfo $errors[0] $file $line; $message = $info[0]; $file = $info[1]; $line = $info[2]; $type = $info[3] }
                $ms = 0
                if ($null -ne $t.Duration) { $ms = $t.Duration.TotalMilliseconds }
                $Bridge.TestFinished($state.Container, (& $blockNames $t.Block), [string]$t.ExpandedName, [string]$t.Result, $ms, $message, $file, [int]$line, $type)
            } catch { $Bridge.Fault('EachTestTeardownEnd', $_.Exception.Message) }
        }.GetNewClosure()
        ContainerRunEnd = {
            param($Context)
            try {
                & $walk $Bridge $state.Container $state.Name $Context.Block $false
                $Bridge.ContainerFinished($state.Container)
            } catch { $Bridge.Fault('ContainerRunEnd', $_.Exception.Message) }
        }.GetNewClosure()
    }
    $plugin = & $pester { param($s) New-PluginObject -Name 'ConsoleMonster' @s } $steps
    $factory = { param($Verbosity) $plugin }.GetNewClosure()
    $original = & $pester { ${function:Get-WriteScreenPlugin} }
    & $pester { param($f) ${function:script:Get-WriteScreenPlugin} = $f } $factory
    try {
        $ErrorActionPreference = $callerErrorAction
        if ($Options.Stream -or ($verbosity -eq 'None')) {
            $result = & $invokePester -Configuration $configuration
        } else {
            # Errors, warnings, verbose, debug and information records from the tests would be written by the host straight
            # over the live view; they are collected and written above it instead.
            & $invokePester -Configuration $configuration 2>&1 3>&1 4>&1 5>&1 6>&1 | ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord]) { $Bridge.Record('error', [string]$_) }
                elseif ($_ -is [System.Management.Automation.WarningRecord]) { $Bridge.Record('warning', $_.Message) }
                elseif ($_ -is [System.Management.Automation.VerboseRecord]) { $Bridge.Record('verbose', $_.Message) }
                elseif ($_ -is [System.Management.Automation.DebugRecord]) { $Bridge.Record('debug', $_.Message) }
                elseif ($_ -is [System.Management.Automation.InformationRecord]) { $Bridge.Record('information', [string]$_.MessageData) }
                else { $result = $_ }
            }
        }
    } finally {
        $ErrorActionPreference = 'Stop'
        & $pester { param($f) ${function:script:Get-WriteScreenPlugin} = $f } $original
    }
} else {
    $ErrorActionPreference = $callerErrorAction
    $result = & $invokePester -Configuration $configuration
    $ErrorActionPreference = 'Stop'
}

# Containers the run did not report (a plain run, or files that failed before they ran) come from the result object.
if ($null -ne $result) {
    foreach ($container in @($result.Containers)) {
        if ($null -eq $container) { continue }
        $info = & $keyOf $container.Item
        if ($state.Seen.ContainsKey($info[0])) { continue }
        $Bridge.ContainerStarted($info[0], $info[1], $info[2], (& $countTests $container))
        & $walk $Bridge $info[0] $info[1] $container $true
        $Bridge.ContainerFinished($info[0])
    }
}
$Bridge.End()
[pscustomobject]@{ Result = $result; Exit = $exitRequested; Throw = $throwRequested; PassThru = $passThru }
";
    }
}
