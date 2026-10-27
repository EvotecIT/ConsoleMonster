param([string]$AssemblyDirectory)
. (Join-Path $PSScriptRoot 'ConsoleLoopSetup.ps1') -AssemblyDirectory $AssemblyDirectory -ResultsPath ''
$global:DisplayLoopLease.Dispose()
$global:CollectLease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('CollectHost',[version]'1.0.0',[string[]]@('DisplayHostContract.Row'))
$global:CollectBatches=New-Object 'System.Collections.Generic.List[string]'
$global:CollectVersion=0
$global:CollectBroken=$false
$global:CollectLarge=$false
$global:CollectBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($objects,$context)
    $names=if($objects.Count -gt 4){$objects.Count.ToString()+':'+$objects[0].Name+':'+$objects[$objects.Count-1].Name}else{($objects|ForEach-Object {$_.Name}) -join ','}
    $global:CollectBatches.Add($names)
    $global:CollectMode=$context.Profile.Mode.ToString()
    if($global:CollectBroken){return [DisplayHostContract.BrokenWidget]::new()}
    if($global:CollectLarge){return [ConsoleMonster.Widgets.TextBlock]::Plain(('z'*4096))}
    [ConsoleMonster.Widgets.TextBlock]::new('[bold]COLLECT<'+$names+'>[/]')
}
function global:Use-CollectView {
    param([int]$Records=2000,[long]$Bytes=16777216,[string]$View='collect.host')
    $global:CollectVersion++
    $global:CollectBatches.Clear()
    $global:CollectLease.Unregister('DisplayHostContract.Row')
    $global:CollectView=$global:CollectLease.Register('DisplayHostContract.Row',$View,[version]('1.0.'+$global:CollectVersion),'Collect',[string[]]@('Scope'),$global:CollectBuilder,[ConsoleMonster.PowerShell.Display.DefaultDisplayBudget]::new($Records,$Bytes))
    [Console]::Clear()
}
function global:Assert-Collect {
    param([string]$Name,[bool]$Condition)
    $details=if($Condition){$null}else{[DisplayHostContract.ConsoleText]::Read().Substring(0,4000)}
    $global:DisplayLoopResults.Add([pscustomobject]@{Name=$Name;Passed=$Condition;Batches=$global:CollectBatches.ToArray();Details=$details})
}
function global:Test-CollectOrder {
    param([string[]]$Markers)
    $screen=[DisplayHostContract.ConsoleText]::Read()
    $last=-1
    foreach($marker in $Markers){$position=$screen.IndexOf($marker);if($position -le $last){return $false};$last=$position}
    return $true
}
function global:Read-CollectStatus { [DisplayHostContract.ConsoleText]::Read().Contains('collecting 2 rows...') }
function global:Read-PlainCollectStatus {
    $raw=$Host.UI.RawUI;$y=$raw.CursorPosition.Y-1
    $cells=$raw.GetBufferContents([Management.Automation.Host.Rectangle]::new(0,$y,0,$y))
    $cells[0,0].Character -eq 'c' -and $cells[0,0].ForegroundColor -eq $raw.ForegroundColor
}
Use-CollectView
