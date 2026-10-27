param([string]$AssemblyDirectory)
. (Join-Path $PSScriptRoot 'ConsoleLoopSetup.ps1') -AssemblyDirectory $AssemblyDirectory -ResultsPath ''
$global:DisplayLoopLease.Dispose()
$global:ColorLease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('HostColors',[version]'1.0.0',[string[]]@('DisplayHostContract.Row'))
$global:ColorContexts=New-Object 'System.Collections.Generic.List[object]'
# The controller can inherit TERM=dumb from its automation shell; this owned console proves actual colored output.
foreach($name in 'TERM','CONSOLEMONSTER_MODE','CONSOLEMONSTER_COLOR'){[Environment]::SetEnvironmentVariable($name,$null,'Process')}
$env:CONSOLEMONSTER_COLOR='truecolor'
$env:CONSOLEMONSTER_THEME='light'
if($PSVersionTable.PSVersion.Major -ge 7){$PSStyle.OutputRendering='Host'}
function global:Read-HostColorPair {
    [pscustomobject]@{RawForeground=$Host.UI.RawUI.ForegroundColor.ToString();RawBackground=$Host.UI.RawUI.BackgroundColor.ToString();RawAttributes=([int]$Host.UI.RawUI.BackgroundColor*16+[int]$Host.UI.RawUI.ForegroundColor);ConsoleAttributes=([int][Console]::BackgroundColor*16+[int][Console]::ForegroundColor)}
}
function global:Set-OrdinaryHostColors {
    param([ConsoleColor]$Foreground=[ConsoleColor]::Black,[ConsoleColor]$Background=[ConsoleColor]::White)
    $Host.UI.RawUI.ForegroundColor=$Foreground;$Host.UI.RawUI.BackgroundColor=$Background
    Clear-Host
    $global:ColorBefore=Read-HostColorPair
}
function global:Save-ColorContext {
    param($Context,[string]$Role)
    $global:ColorContexts.Add([pscustomobject]@{Role=$Role;Foreground=$Context.Profile.DefaultForeground;Background=$Context.Profile.DefaultBackground;Mode=$Context.Profile.Mode.ToString();Depth=$Context.Profile.ColorDepth.ToString()})
}
function global:Test-ColorContexts {
    param([int]$Count,[ConsoleColor]$Foreground=[ConsoleColor]::Black,[ConsoleColor]$Background=[ConsoleColor]::White,[switch]$Plain)
    if($global:ColorContexts.Count -ne $Count){return $false}
    foreach($context in $global:ColorContexts){
        if($Plain){if(-not $context.Foreground.IsDefault -or -not $context.Background.IsDefault -or $context.Depth -ne 'None' -or $context.Mode -ne 'Plain'){return $false}}
        elseif(-not $context.Foreground.Equals([ConsoleMonster.Styling.Color]::FromConsoleColor($Foreground)) -or -not $context.Background.Equals([ConsoleMonster.Styling.Color]::FromConsoleColor($Background))){return $false}
    }
    return $true
}
function global:Test-ColorCells {
    param([string]$Marker,[ConsoleColor]$Background=[ConsoleColor]::White,[switch]$BlackText)
    $screen=[DisplayHostContract.ConsoleText]::Read();$index=$screen.IndexOf($Marker)
    if($index -lt 0){return $false}
    $width=[Console]::BufferWidth;$x=$index%$width;$y=[int][Math]::Floor($index/$width)
    if($x+$Marker.Length -gt $width){return $false}
    $cells=$Host.UI.RawUI.GetBufferContents([Management.Automation.Host.Rectangle]::new($x,$y,$x+$Marker.Length-1,$y))
    for($i=0;$i -lt $Marker.Length;$i++){if($cells[0,$i].BackgroundColor -ne $Background -or ($BlackText -and $cells[0,$i].ForegroundColor -ne [ConsoleColor]::Black)){return $false}}
    return $true
}
function global:Assert-HostColors {
    param([string]$Name,[bool]$Condition,[int]$ExpectedAttributes=240)
    $after=Read-HostColorPair
    $passed=$Condition -and $global:ColorBefore.RawAttributes -eq $ExpectedAttributes -and $global:ColorBefore.ConsoleAttributes -eq $ExpectedAttributes -and $after.RawAttributes -eq $ExpectedAttributes -and $after.ConsoleAttributes -eq $ExpectedAttributes
    $global:DisplayLoopResults.Add([pscustomobject]@{Name=$Name;Passed=[bool]$passed;Before=$global:ColorBefore;After=$after;Contexts=$global:ColorContexts.ToArray();Details=if($passed){$null}else{[DisplayHostContract.ConsoleText]::Read().Substring(0,4000)}})
}
$global:ColorBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($objects,$context)
    Save-ColorContext $context 'Batch'
    if($global:ColorBroken){return [DisplayHostContract.BrokenWidget]::new()}
    if($global:ColorBatch -eq 'Stream'){
        $table=[ConsoleMonster.Widgets.Table]::new();$table.Title='LIGHT-GROUP'
        $column=[ConsoleMonster.Widgets.TableColumn]::new('LIGHT-HEADER');$column.Width=28;$null=$table.AddColumn($column)
        $null=$table.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name)))
        return $table
    }
    [ConsoleMonster.Widgets.TextBlock]::new('[bold]LIGHT-FRAME[/] '+(($objects|ForEach-Object{$_.Name}) -join ','))
}
$global:ColorSingleBuilder=[Func[object,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($value,$context)
    Save-ColorContext $context 'Single'
    [ConsoleMonster.Widgets.TextBlock]::new('[bold]LIGHT-ONE[/] '+$value.Name)
}
function global:Use-ColorView {
    param([string]$Batch='Single',[switch]$Adaptive,[int]$Records=2000)
    $global:ColorLease.Unregister('DisplayHostContract.Row');$global:ColorContexts.Clear();$global:ColorBroken=$false;$global:ColorBatch=$Batch
    $global:ColorLease.Mode='Auto';$env:NO_COLOR=$null
    [ConsoleMonster.Terminal]::Refresh()
    $global:ColorView=$global:ColorLease.Register('DisplayHostContract.Row','host.colors',[version]'1.0.0',$Batch,$null,$global:ColorBuilder,[ConsoleMonster.PowerShell.Display.DefaultDisplayBudget]::new($Records))
    if($Adaptive){$global:ColorView.SingleRecordBuilder=$global:ColorSingleBuilder}
    if($Batch -eq 'Stream'){$global:ColorView.Streaming=[ConsoleMonster.PowerShell.Display.DefaultDisplayStreaming]::new([Func[object]]{0},[Func[object,object,object]]{param($state,$row) [int]$state+1},[Func[object,long,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($state,$count,$context) Save-ColorContext $context 'Footer';[ConsoleMonster.Widgets.TextBlock]::new('[dim]LIGHT-FOOTER[/] '+$count)})}
    Set-OrdinaryHostColors
}
Use-ColorView
