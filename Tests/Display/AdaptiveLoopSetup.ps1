param([string]$AssemblyDirectory)
. (Join-Path $PSScriptRoot 'CollectLoopSetup.ps1') -AssemblyDirectory $AssemblyDirectory
$global:CollectLease.Dispose()
$global:AdaptiveLease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('AdaptiveHost',[version]'1.0.0',[string[]]@('DisplayHostContract.Row'))
$global:AdaptiveSingleBuilder=[Func[object,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($value,$context)
    $global:AdaptiveSingles++
    if($global:AdaptiveThrowSingle){return [DisplayHostContract.BrokenWidget]::new()}
    [ConsoleMonster.Widgets.TextBlock]::Plain('SINGLE<'+$value.Name+'>')
}
$global:AdaptiveRowBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($objects,$context)
    $global:AdaptiveRows++
    if($objects[0].Name -eq $global:AdaptiveThrowRow){throw 'Expected adaptive row failure.'}
    $table=[ConsoleMonster.Widgets.Table]::new();$table.Title='adaptive group'
    $column=[ConsoleMonster.Widgets.TableColumn]::new('AdaptiveName');$column.Width=24
    $null=$table.AddColumn($column)
    $null=$table.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name)))
    $table
}
function global:Use-AdaptiveView {
    param([switch]$Grouped,[string]$View='adaptive.host',[long]$Bytes=16777216)
    $global:AdaptiveLease.Unregister('DisplayHostContract.Row')
    [string[]]$keys=if($Grouped){@('Scope')}else{$null}
    $global:AdaptiveView=$global:AdaptiveLease.Register('DisplayHostContract.Row',$View,[version]'1.0.0','Stream',$keys,$global:AdaptiveRowBuilder,[ConsoleMonster.PowerShell.Display.DefaultDisplayBudget]::new(2000,$Bytes))
    $global:AdaptiveView.SingleRecordBuilder=$global:AdaptiveSingleBuilder
    $global:AdaptiveView.Streaming=[ConsoleMonster.PowerShell.Display.DefaultDisplayStreaming]::new([Func[object]]{0},[Func[object,object,object]]{param($state,$row) [int]$state+1},[Func[object,long,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($state,$count,$context) $global:AdaptiveFooters++;[ConsoleMonster.Widgets.TextBlock]::Plain('ADAPTIVE_FOOTER<'+$count+'>')})
    $global:AdaptiveSingles=0;$global:AdaptiveRows=0;$global:AdaptiveFooters=0;$global:AdaptiveThrowSingle=$false;$global:AdaptiveThrowRow=''
    $global:CollectBatches.Clear()
    [Console]::Clear()
}
Use-AdaptiveView
