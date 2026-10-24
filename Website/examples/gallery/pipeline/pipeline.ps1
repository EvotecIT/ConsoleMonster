# Module build pipeline: phases, sixteen steps, compiler lanes and captured output.
param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 9)][int]$Step = 0
)

Import-Module ConsoleMonster

# Data: fictional tool output and timings. No builds, signing, publishing or file changes occur.
$steps = @(
    @{ Phase = 'Prepare'; Key = 'config'; Label = 'Read Build/project.build.json'; Seconds = 0.4; Result = 'Pass'; Output = @('Loaded project.build.json (3 projects, version 2.0.X)', 'Resolved version 2.0.0 from PowerShell Gallery (current 0.0.4)') }
    @{ Phase = 'Prepare'; Key = 'manifest'; Label = 'Update ConsoleMonster.psd1'; Seconds = 0.5; Result = 'Pass'; Output = @('ModuleVersion 0.0.4 -> 2.0.0', 'CmdletsToExport: 38 binary cmdlets') }
    @{ Phase = 'Restore'; Key = 'restore'; Label = 'Restore NuGet packages'; Seconds = 1.6; Result = 'Pass'; Output = @('Restored src\Orion\Orion.csproj (in 412 ms)', 'Restored src\Orion.Shell\Orion.Shell.csproj (in 380 ms)') }
    @{ Phase = 'Build'; Key = 'core-ns'; Label = 'Compile ConsoleMonster (netstandard2.0)'; Seconds = 1.8; Result = 'Pass'; Output = @('ConsoleMonster -> bin\Release\netstandard2.0\ConsoleMonster.dll') }
    @{ Phase = 'Build'; Key = 'core-fx'; Label = 'Compile ConsoleMonster (net472)'; Seconds = 1.5; Result = 'Pass'; Output = @('ConsoleMonster -> bin\Release\net472\ConsoleMonster.dll') }
    @{ Phase = 'Build'; Key = 'core-net'; Label = 'Compile ConsoleMonster (net8.0)'; Seconds = 1.4; Result = 'Pass'; Output = @('ConsoleMonster -> bin\Release\net8.0\ConsoleMonster.dll') }
    @{ Phase = 'Build'; Key = 'ps'; Label = 'Compile ConsoleMonster.PowerShell'; Seconds = 1.9; Result = 'Pass'; Output = @('ConsoleMonster.PowerShell -> bin\Release\net472\ConsoleMonster.PowerShell.dll', 'ConsoleMonster.PowerShell -> bin\Release\net8.0\ConsoleMonster.PowerShell.dll') }
    @{ Phase = 'Test'; Key = 'xunit'; Label = 'Run xUnit tests (net472, net8.0)'; Seconds = 3.2; Result = 'Pass'; Output = @('Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net472)', 'Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net8.0)') }
    @{ Phase = 'Test'; Key = 'compatibility'; Label = 'Run compatibility tests'; Seconds = 2.4; Result = 'Pass'; Output = @('Tests completed in 2.1s', 'Tests Passed: 41, Failed: 0, Skipped: 0') }
    @{ Phase = 'Test'; Key = 'pester7'; Label = 'Run Pester tests (PowerShell 7.5)'; Seconds = 2.1; Result = 'Warn'; Output = @('Tests completed in 1.8s', 'Tests Passed: 40, Failed: 0, Skipped: 1 (requires conhost)') }
    @{ Phase = 'Docs'; Key = 'md'; Label = 'Generate markdown help (38 commands)'; Seconds = 1.7; Result = 'Pass'; Output = @('Docs\Readme.md updated', 'Docs\Write-ConsoleTable.md ... Docs\Start-ConsoleDashboard.md') }
    @{ Phase = 'Docs'; Key = 'maml'; Label = 'Generate en-US MAML help'; Seconds = 0.9; Result = 'Pass'; Output = @('en-US\ConsoleMonster.PowerShell.dll-Help.xml (412 KB)') }
    @{ Phase = 'Sign'; Key = 'sign'; Label = 'Sign module files'; Seconds = 0.3; Result = 'Skip'; Output = @('SignModule is $false for this build; skipping Authenticode signing') }
    @{ Phase = 'Package'; Key = 'nupkg'; Label = 'Pack ConsoleMonster.2.0.0.nupkg'; Seconds = 1.2; Result = 'Pass'; Output = @('Successfully created package ''Artefacts\ProjectBuild\ConsoleMonster.2.0.0.nupkg''.') }
    @{ Phase = 'Package'; Key = 'zip'; Label = 'Create module archive'; Seconds = 0.8; Result = 'Pass'; Output = @('Artefacts\Packed\ConsoleMonster-v2.0.0.zip (1.1 MB)') }
    @{ Phase = 'Publish'; Key = 'publish'; Label = 'Publish to PowerShell Gallery'; Seconds = 0.3; Result = 'Skip'; Output = @('RunMode is Build; publishing is disabled') }
 )
$phaseNames = 'Prepare', 'Restore', 'Build', 'Test', 'Docs', 'Sign', 'Package', 'Publish'
$compilerNames = 'netstandard2.0', 'net472', 'net8.0', 'PowerShell'
$checkpoints = 0, 2, 3, 5, 7, 9, 10, 12, 14, 16
$culture = [cultureinfo]::InvariantCulture
$snapshot = $StaticSnapshot -or [Console]::IsInputRedirected
$completed = if ($snapshot) { $checkpoints[$Step] } else { 0 }
function Format-PipelinePath($Text) { $Text.Replace([char]92, [IO.Path]::DirectorySeparatorChar) }
function Format-PipelineDuration($Seconds) { $Seconds.ToString('0.0', $culture) + 's' }

# Layout: preserve the complete checklist, phase strip, module facts, progress, compiler lanes and log.
$dashboard = New-ConsoleDashboard -Title '[bold]Module build[/]' -Subtitle 'Build-Module ConsoleMonster' -Footer '[muted]Prepare · Restore · Build · Test · Docs · Sign · Package · Publish    Ctrl+C stop[/]' {
    New-ConsolePadding -Padding 1, 0, 1, 1 {
        New-ConsolePhases {
            $offset = 0
            foreach ($phase in $phaseNames) {
                $group = @($steps | Where-Object Phase -EQ $phase)
                $last = $offset + $group.Count
                $state = if ($last -le $completed) {
                    if ($group.Result -contains 'Warn') { 'Warn' } elseif (@($group | Where-Object Result -NE Skip).Count -eq 0) { 'Skip' } else { 'Pass' }
                } elseif ($offset -le $completed -and $completed -lt $steps.Count) { 'Running' } else { 'Pending' }
                $detail = if ($last -le $completed) { Format-PipelineDuration ($group | Measure-Object Seconds -Sum).Sum } else { '' }
                New-ConsoleStatusItem $phase -Status $state -Detail $detail
                $offset = $last
            }
        }
    }
    New-ConsoleColumns {
        New-ConsolePanel -Title Steps -Weight 1.5 {
            New-ConsoleStatusList -Name steps {
                for ($i = 0; $i -lt $steps.Count; $i++) {
                    $item = $steps[$i]
                    $state = if ($i -lt $completed) { $item.Result } elseif ($i -eq $completed) { 'Running' } else { 'Pending' }
                    $detail = if ($i -lt $completed) { Format-PipelineDuration $item.Seconds } else { '' }
                    New-ConsoleStatusItem (Format-PipelinePath $item.Label) -Key $item.Key -Status $state -Detail $detail
                }
            }
        }
        New-ConsolePanel -Title Module {
            New-ConsoleKeyValue -Name module -Markup -Data ([ordered]@{
                Module = '[bold]ConsoleMonster[/]'
                Version = '0.0.4 [muted]→[/] [bold]2.0.0[/]'
                Frameworks = 'netstandard2.0 · net472 · net8.0'
                PowerShell = '5.1 Desktop · 7.x Core'
                'Run mode' = 'Build'
                Elapsed = if ($completed) { Format-PipelineDuration ($steps | Select-Object -First $completed | Measure-Object Seconds -Sum).Sum } else { '-' }
            })
            New-ConsoleRule
            $progress = New-ConsoleProgress -Name overall -Label Overall -Total $steps.Count -Value $completed
            $progress.LabelWidth = 8
            $progress
            New-ConsoleRule 'Compiler workers'
            New-ConsoleLanes -Name compilers {
                for ($i = 0; $i -lt $compilerNames.Count; $i++) {
                    $done = if ($completed -gt $i + 3) { 1 } else { 0 }
                    $state = if ($done) { 'Pass' } elseif ($completed -eq $i + 3) { 'Running' } else { 'Pending' }
                    $detail = if ($done) { 'compiled' } elseif ($state -eq 'Running') { 'compiling' } else { 'queued' }
                    New-ConsoleLane "c$i" -Label $compilerNames[$i] -Total 1 -Done $done -Passed $done -Status $state -Detail $detail
                }
            }
        }
    }
    New-ConsolePanel -Title Output -Fill {
        New-ConsoleLog -Height 8 -NoTime {
            New-ConsoleLogEntry 'Build engine · release pipeline'
            foreach ($item in ($steps | Select-Object -First $completed)) {
                $level = if ($item.Result -eq 'Warn') { 'warn' } elseif ($item.Result -eq 'Skip') { 'skip' } else { 'info' }
                foreach ($line in $item.Output) { New-ConsoleLogEntry (Format-PipelinePath $line) -Level $level }
            }
            if ($completed -lt $steps.Count) { New-ConsoleLogEntry ((Format-PipelinePath $steps[$completed].Label) + '...') }
        }
    }
}
$dashboard.ShowClock = $false

# Behaviour: native cmdlets post progress; Write-Host and Write-Warning land in the output region.
if ($snapshot) {
    $dashboard | Write-ConsoleWidget
    return
}
Start-ConsoleDashboard $dashboard -ArgumentList (, $steps) {
    param($Steps)
    $culture = [cultureinfo]::InvariantCulture
    $elapsed = 0.0
    for ($i = 0; $i -lt $Steps.Count; $i++) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) { break }
        $item = $Steps[$i]
        Update-ConsolePhase -Key $item.Phase -Status Running
        Update-ConsoleStatus -Name steps -Key $item.Key -Status Running
        Update-ConsoleProgress -Name overall -Label $item.Phase
        if ($i -ge 3 -and $i -le 6) { Update-ConsoleLane -Name compilers -Key ('c' + ($i - 3)) -Status Running -Detail compiling }
        Write-Host ($item.Label.Replace([char]92, [IO.Path]::DirectorySeparatorChar) + '...')
        Start-Sleep -Milliseconds ([int]($item.Seconds * 400))
        foreach ($line in $item.Output) {
            $text = $line.Replace([char]92, [IO.Path]::DirectorySeparatorChar)
            if ($item.Result -eq 'Warn') { Write-Warning $text } else { Write-Host $text }
        }
        $elapsed += $item.Seconds
        $detail = $item.Seconds.ToString('0.0', $culture) + 's'
        Update-ConsoleStatus -Name steps -Key $item.Key -Status $item.Result -Detail $detail
        Set-ConsoleValue -Name module -Key Elapsed -Value ($elapsed.ToString('0.0', $culture) + 's')
        Update-ConsoleProgress -Name overall -Value ($i + 1)
        if ($i -ge 3 -and $i -le 6) { Update-ConsoleLane -Name compilers -Key ('c' + ($i - 3)) -Result Pass -Detail compiled -Complete }
        if ($i -eq $Steps.Count - 1 -or $Steps[$i + 1].Phase -ne $item.Phase) {
            $group = @($Steps | Where-Object Phase -EQ $item.Phase)
            $state = if ($group.Result -contains 'Warn') { 'Warn' } elseif (@($group | Where-Object Result -NE Skip).Count -eq 0) { 'Skip' } else { 'Pass' }
            $detail = ($group | Measure-Object Seconds -Sum).Sum.ToString('0.0', $culture) + 's'
            Update-ConsolePhase -Key $item.Phase -Status $state -Detail $detail
        }
    }
}
