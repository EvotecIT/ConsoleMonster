param([string]$AssemblyDirectory,[string]$ResultsPath)
$ErrorActionPreference='Stop'
foreach($name in 'CI','TF_BUILD','GITHUB_ACTIONS','GITLAB_CI','CODEX_CI','CONSOLEMONSTER_DEFAULT_VIEWS','NO_COLOR'){
    [Environment]::SetEnvironmentVariable($name,$null,'Process')
}
$core=Join-Path $AssemblyDirectory 'ConsoleMonster.dll'
Add-Type -Path $core
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll')
Add-Type -Path (Join-Path $PSScriptRoot 'Fixtures/DisplayHostFixture.cs') -ReferencedAssemblies @($core,[Console].Assembly.Location)
$global:DisplayLoopResults=New-Object 'System.Collections.Generic.List[object]'
$global:DisplayLoopBuilds=0
$global:DisplayLoopSummaries=0
function global:Assert-ConsoleLoop([string]$Name,[bool]$Condition,[string]$Details){
    $global:DisplayLoopResults.Add([pscustomobject]@{Name=$Name;Passed=$Condition;Details=$Details})
}
$global:DisplayLoopBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($objects,$context)
    $global:DisplayLoopBuilds++
    $global:DisplayLoopLastMode=$context.Profile.Mode.ToString()
    [ConsoleMonster.Widgets.TextBlock]::Plain('RICH '+$objects[0].Name)
}
$global:DisplayLoopLease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('ConsoleLoop',[version]'1.0.0',[string[]]@('DisplayHostContract.Row'))
$global:DisplayLoopView=$global:DisplayLoopLease.Register('DisplayHostContract.Row','loop.row',[version]'1.0.0','Single',[string[]]@('Scope'),$global:DisplayLoopBuilder,$null)
$global:DisplayLoopResultsPath=$ResultsPath
