# Module build pipeline: phases, sixteen steps, compiler lanes and captured output.

Import-Module -Name ConsoleMonster

# Data: fictional tool output and timings. No builds, signing, publishing or file changes occur.
$steps = @(
    [pscustomobject] @{ Phase = 'Prepare'; Key = 'config'; Label = 'Read Build/project.build.json'; Seconds = 0.4; Result = 'Pass'; Output = @('Loaded project.build.json (3 projects, version 2.0.X)', 'Resolved version 2.0.0 from PowerShell Gallery (current 0.0.4)') }
    [pscustomobject] @{ Phase = 'Prepare'; Key = 'manifest'; Label = 'Update ConsoleMonster.psd1'; Seconds = 0.5; Result = 'Pass'; Output = @('ModuleVersion 0.0.4 -> 2.0.0', 'CmdletsToExport: 38 binary cmdlets') }
    [pscustomobject] @{ Phase = 'Restore'; Key = 'restore'; Label = 'Restore NuGet packages'; Seconds = 1.6; Result = 'Pass'; Output = @('Restored src\Orion\Orion.csproj (in 412 ms)', 'Restored src\Orion.Shell\Orion.Shell.csproj (in 380 ms)') }
    [pscustomobject] @{ Phase = 'Build'; Key = 'core-ns'; Label = 'Compile ConsoleMonster (netstandard2.0)'; Seconds = 1.8; Result = 'Pass'; Output = @('ConsoleMonster -> bin\Release\netstandard2.0\ConsoleMonster.dll') }
    [pscustomobject] @{ Phase = 'Build'; Key = 'core-fx'; Label = 'Compile ConsoleMonster (net472)'; Seconds = 1.5; Result = 'Pass'; Output = @('ConsoleMonster -> bin\Release\net472\ConsoleMonster.dll') }
    [pscustomobject] @{ Phase = 'Build'; Key = 'core-net'; Label = 'Compile ConsoleMonster (net8.0)'; Seconds = 1.4; Result = 'Pass'; Output = @('ConsoleMonster -> bin\Release\net8.0\ConsoleMonster.dll') }
    [pscustomobject] @{ Phase = 'Build'; Key = 'ps'; Label = 'Compile ConsoleMonster.PowerShell'; Seconds = 1.9; Result = 'Pass'; Output = @('ConsoleMonster.PowerShell -> bin\Release\net472\ConsoleMonster.PowerShell.dll', 'ConsoleMonster.PowerShell -> bin\Release\net8.0\ConsoleMonster.PowerShell.dll') }
    [pscustomobject] @{ Phase = 'Test'; Key = 'xunit'; Label = 'Run xUnit tests (net472, net8.0)'; Seconds = 3.2; Result = 'Pass'; Output = @('Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net472)', 'Passed!  - Failed: 0, Passed: 286, Skipped: 0 - ConsoleMonster.Tests.dll (net8.0)') }
    [pscustomobject] @{ Phase = 'Test'; Key = 'compatibility'; Label = 'Run compatibility tests'; Seconds = 2.4; Result = 'Pass'; Output = @('Tests completed in 2.1s', 'Tests Passed: 41, Failed: 0, Skipped: 0') }
    [pscustomobject] @{ Phase = 'Test'; Key = 'pester7'; Label = 'Run Pester tests (PowerShell 7.5)'; Seconds = 2.1; Result = 'Warn'; Output = @('Tests completed in 1.8s', 'Tests Passed: 40, Failed: 0, Skipped: 1 (requires conhost)') }
    [pscustomobject] @{ Phase = 'Docs'; Key = 'md'; Label = 'Generate markdown help (38 commands)'; Seconds = 1.7; Result = 'Pass'; Output = @('Docs\Readme.md updated', 'Docs\Show-ConsoleTable.md ... Docs\Invoke-ConsoleDashboard.md') }
    [pscustomobject] @{ Phase = 'Docs'; Key = 'maml'; Label = 'Generate en-US MAML help'; Seconds = 0.9; Result = 'Pass'; Output = @('en-US\ConsoleMonster.PowerShell.dll-Help.xml (412 KB)') }
    [pscustomobject] @{ Phase = 'Sign'; Key = 'sign'; Label = 'Sign module files'; Seconds = 0.3; Result = 'Skip'; Output = @('SignModule is $false for this build; skipping Authenticode signing') }
    [pscustomobject] @{ Phase = 'Package'; Key = 'nupkg'; Label = 'Pack ConsoleMonster.2.0.0.nupkg'; Seconds = 1.2; Result = 'Pass'; Output = @('Successfully created package ''Artefacts\ProjectBuild\ConsoleMonster.2.0.0.nupkg''.') }
    [pscustomobject] @{ Phase = 'Package'; Key = 'zip'; Label = 'Create module archive'; Seconds = 0.8; Result = 'Pass'; Output = @('Artefacts\Packed\ConsoleMonster-v2.0.0.zip (1.1 MB)') }
    [pscustomobject] @{ Phase = 'Publish'; Key = 'publish'; Label = 'Publish to PowerShell Gallery'; Seconds = 0.3; Result = 'Skip'; Output = @('RunMode is Build; publishing is disabled') }
)
$phaseNames = 'Prepare', 'Restore', 'Build', 'Test', 'Docs', 'Sign', 'Package', 'Publish'
$compilerNames = 'netstandard2.0', 'net472', 'net8.0', 'PowerShell'
$culture = [cultureinfo]::InvariantCulture
$completed = 0
function Format-PipelinePath {
    param(
        $Text
    )
    $Text.Replace([char]92, [char]47)
}
function Format-PipelineDuration {
    param(
        $Seconds
    )
    $Seconds.ToString('0.0', $culture) + 's'
}

# Layout: preserve the complete checklist, phase strip, module facts, progress, compiler lanes and log.
$dashboard = New-ConsoleDashboard -Title '[bold]Module build[/]' -Subtitle 'Build-Module ConsoleMonster' -Footer '[muted]Prepare · Restore · Build · Test · Docs · Sign · Package · Publish    Ctrl+C stop[/]' -Content {
    New-ConsolePadding -Padding 1, 0, 1, 1 -Content {
        New-ConsolePhases -Content {
            $offset = 0
            foreach ($phase in $phaseNames) {
                $group = @($steps | Where-Object -Property Phase -EQ -Value $phase)
                $last = $offset + $group.Count
                $state = if ($last -le $completed) {
                    if ($group.Result -contains 'Warn') { 'Warn' } elseif (@($group | Where-Object -Property Result -NE -Value Skip).Count -eq 0) { 'Skip' } else { 'Pass' }
                }
                elseif ($offset -le $completed -and $completed -lt $steps.Count) {
                    'Running'
                }
                else {
                    'Pending'
                }
                $detail = if ($last -le $completed) {
                    Format-PipelineDuration -Seconds ($group | Measure-Object -Property Seconds -Sum).Sum
                }
                else {
                    ''
                }
                [pscustomobject]@{ Label = $phase; Status = $state; Detail = $detail }
                $offset = $last
            }
        }
    }
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title Steps -Weight 1.5 -Content {
            New-ConsoleStatusList -WidgetId steps -Content {
                for ($i = 0; $i -lt $steps.Count; $i++) {
                    $item = $steps[$i]
                    $state = if ($i -lt $completed) { $item.Result } elseif ($i -eq $completed) { 'Running' } else { 'Pending' }
                    $detail = if ($i -lt $completed) {
                        Format-PipelineDuration -Seconds $item.Seconds
                    }
                    else {
                        ''
                    }
                    [pscustomobject]@{ Label = (Format-PipelinePath -Text $item.Label); Key = $item.Key; Status = $state; Detail = $detail }
                }
            }
        }
        New-ConsolePanel -Title Module -Content {
            New-ConsoleKeyValue -WidgetId module -Markup -InputObject ([ordered]@{
                    Module = '[bold]ConsoleMonster[/]'
                    Version = '0.0.4 [muted]→[/] [bold]2.0.0[/]'
                    Frameworks = 'netstandard2.0 · net472 · net8.0'
                    PowerShell = '5.1 Desktop · 7.x Core'
                    'Run mode' = 'Build'
                    Elapsed = if ($completed) {
                        Format-PipelineDuration -Seconds ($steps | Select-Object -First $completed | Measure-Object -Property Seconds -Sum).Sum
                    }
                    else {
                        '-'
                    }
                })
            New-ConsoleRule
            $progress = New-ConsoleProgress -WidgetId overall -Label Overall -Total $steps.Count -Value $completed
            $progress.LabelWidth = 8
            $progress
            New-ConsoleRule -Title 'Compiler workers'
            New-ConsoleLanes -WidgetId compilers -Content {
                for ($i = 0; $i -lt $compilerNames.Count; $i++) {
                    $done = if ($completed -gt $i + 3) { 1 } else { 0 }
                    $state = if ($done) { 'Pass' } elseif ($completed -eq $i + 3) { 'Running' } else { 'Pending' }
                    $detail = if ($done) { 'compiled' } elseif ($state -eq 'Running') { 'compiling' } else { 'queued' }
                    [pscustomobject]@{ Key = "c$i"; Label = $compilerNames[$i]; Total = 1; Passed = $done; Status = $state; Detail = $detail; Value = $done }
                }
            }
        }
    }
    New-ConsolePanel -Title Output -Fill -Content {
        New-ConsoleLogRegion -Height 8 -NoElapsed -Content {
            [pscustomobject]@{ Message = 'Build engine · release pipeline' }
            foreach ($item in ($steps | Select-Object -First $completed)) {
                $level = if ($item.Result -eq 'Warn') { 'warn' } elseif ($item.Result -eq 'Skip') { 'skip' } else { 'info' }
                foreach ($line in $item.Output) {
                    [pscustomobject]@{ Message = (Format-PipelinePath -Text $line); Level = $level }
                }
            }
            if ($completed -lt $steps.Count) {
                [pscustomobject]@{ Message = ((Format-PipelinePath -Text $steps[$completed].Label) + '...') }
            }
        }
    }
}
$dashboard.ShowElapsed = $false

# Behaviour: native cmdlets post progress; Write-Host and Write-Warning land in the output region.
Invoke-ConsoleDashboard -Dashboard $dashboard -ScriptBlock {
    $Steps = $using:steps
    $culture = [cultureinfo]::InvariantCulture
    $elapsed = 0.0
    for ($i = 0; $i -lt $Steps.Count; $i++) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) {
            break
        }
        $item = $Steps[$i]
        Update-Console -Key $item.Phase -Status Running -WidgetId 'phases'
        Update-Console -WidgetId steps -Key $item.Key -Status Running
        Update-Console -WidgetId overall -Label $item.Phase
        if ($i -ge 3 -and $i -le 6) {
            Update-Console -WidgetId compilers -Key ('c' + ($i - 3)) -Status Running -Detail compiling
        }
        Write-Host -Object ($item.Label.Replace([char]92, [char]47) + '...')
        Wait-ConsoleDashboard -Seconds ($item.Seconds * 0.4)
        foreach ($line in $item.Output) {
            $text = $line.Replace([char]92, [char]47)
            if ($item.Result -eq 'Warn') {
                Write-Warning -Message $text
            }
            else {
                Write-Host -Object $text
            }
        }
        $elapsed += $item.Seconds
        $detail = $item.Seconds.ToString('0.0', $culture) + 's'
        Update-Console -WidgetId steps -Key $item.Key -Status $item.Result -Detail $detail
        Update-Console -WidgetId module -Key Elapsed -Value ($elapsed.ToString('0.0', $culture) + 's')
        Update-Console -WidgetId overall -Value ($i + 1)
        if ($i -ge 3 -and $i -le 6) {
            Update-Console -WidgetId compilers -Key ('c' + ($i - 3)) -Result Pass -Detail compiled -Complete
        }
        if ($i -eq $Steps.Count - 1 -or $Steps[$i + 1].Phase -ne $item.Phase) {
            $group = @($Steps | Where-Object -Property Phase -EQ -Value $item.Phase)
            $state = if ($group.Result -contains 'Warn') { 'Warn' } elseif (@($group | Where-Object -Property Result -NE -Value Skip).Count -eq 0) { 'Skip' } else { 'Pass' }
            $detail = ($group | Measure-Object -Property Seconds -Sum).Sum.ToString('0.0', $culture) + 's'
            Update-Console -Key $item.Phase -Status $state -Detail $detail -WidgetId 'phases'
        }
    }
}
