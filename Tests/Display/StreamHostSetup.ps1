param([string]$AssemblyDirectory,[int]$Width)
. (Join-Path $PSScriptRoot 'CollectLoopSetup.ps1') -AssemblyDirectory $AssemblyDirectory
$global:CollectLease.Dispose()
$global:StreamHostLease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('StreamHost',[version]'1.0.0',[string[]]@('DisplayHostContract.Row'))
foreach($name in 'TERM','CONSOLEMONSTER_MODE','CONSOLEMONSTER_COLOR'){[Environment]::SetEnvironmentVariable($name,$null,'Process')}
$env:CONSOLEMONSTER_COLOR='truecolor'
if($PSVersionTable.PSVersion.Major -ge 7){$PSStyle.OutputRendering='Host'}
[Console]::SetWindowSize([Math]::Min($Width,[Console]::BufferWidth),[Console]::WindowHeight)
[Console]::SetBufferSize($Width,[Console]::BufferHeight)
[Console]::SetWindowSize($Width,[Console]::WindowHeight)
$global:StreamHostWidth=$Width
$global:StreamHostBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($objects,$context)
    $global:StreamHostRows++
    $global:StreamHostMode=$context.Profile.Mode.ToString();$global:StreamHostDepth=$context.Profile.ColorDepth.ToString()
    $t=[ConsoleMonster.Widgets.Table]::new();$t.Title='stream-host-group'
    foreach($def in @(@('Id',6,8,100),@('Display',24,40,100),@('Size',9,14,70),@('Account',16,30,0))){
        $c=[ConsoleMonster.Widgets.TableColumn]::new([string]$def[0]);$c.MinWidth=[int]$def[1];$c.MaxWidth=[int]$def[2];$c.Priority=[int]$def[3]
        if($c.Header -eq 'Id'){$c.IsKey=$true}
        if($c.Header -eq 'Display'){$c.Flexible=$true}
        if($c.Header -eq 'Size'){$c.Alignment=[ConsoleMonster.Layout.HorizontalAlignment]::Right;$c.Presenter=[ConsoleMonster.Presenters.CellPresenters]::Bytes}
        $null=$t.AddColumn($c)
    }
    $size=if($global:StreamHostRows -eq 1){[long]1}else{[long]1000000000000}
    $null=$t.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue('row'+$global:StreamHostRows),[ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name),[ConsoleMonster.Widgets.TableCell]::FromValue($size),[ConsoleMonster.Widgets.TableCell]::FromValue('DOMAIN\service-account-with-long-name')))
    $t
}
function global:Use-StreamHost {
    param([string]$Glyph,[bool]$Plain)
    $global:StreamHostLease.Unregister('DisplayHostContract.Row');$global:StreamHostRows=0
    $global:StreamHostPlain=$Plain
    $env:NO_COLOR=if($Plain){'1'}else{$null}
    [ConsoleMonster.Terminal]::Refresh()
    $profile=[ConsoleMonster.Terminal]::GetCurrentProfile().WithGlyphs([ConsoleMonster.Capabilities.GlyphTier]$Glyph)
    [ConsoleMonster.Terminal]::Profile=$profile
    $null=$global:StreamHostLease.Register('DisplayHostContract.Row','stream.host',[version]'1.0.0','Stream',$null,$global:StreamHostBuilder)
    [Console]::Clear()
}
function global:Assert-StreamHost {
    param([string]$Name)
    $text=[DisplayHostContract.ConsoleText]::Read();$width=[Console]::BufferWidth
    $lines=@(for($i=0;$i -lt [Math]::Min(100,[Console]::BufferHeight);$i++){$text.Substring($i*$width,$width).TrimEnd()})
    $second=@($lines|Where-Object {$_ -match '\brow2\b'});$third=@($lines|Where-Object {$_ -match '\brow3\b'})
    $footer=@($lines|Where-Object {$_ -match '3 rows'})
    $passed=$global:StreamHostRows -eq 3 -and $second.Count -eq 1 -and $third.Count -eq 1 -and $second[0] -match 'DFS Replication' -and $third[0] -match 'Backups' -and $third[0] -match ([regex]::Escape([string][char]0x2026)+'|\.\.\.') -and @($lines|Where-Object {$_ -match '\bDisplay\b'}).Count -eq 1 -and $footer.Count -eq 1 -and ([Array]::IndexOf($lines,'stream-native-tail')-[Array]::IndexOf($lines,$footer[0])) -ge 2
    $modeCorrect=if($global:StreamHostPlain){$global:StreamHostMode -eq 'Plain' -and $global:StreamHostDepth -eq 'None'}else{$global:StreamHostMode -ne 'Plain' -and $global:StreamHostDepth -eq 'TrueColor'}
    $global:DisplayLoopResults.Add([pscustomobject]@{Name=$Name;Passed=[bool]($passed -and $modeCorrect);BufferWidth=$width;ProfileWidth=[ConsoleMonster.Terminal]::GetCurrentProfile().Width;Mode=$global:StreamHostMode;Depth=$global:StreamHostDepth;Lines=@($lines|Where-Object {$_})})
}

function global:Use-IdentityStreamHost {
    param([string]$Glyph,[bool]$Plain)
    Use-StreamHost -Glyph $Glyph -Plain $Plain
    $global:StreamHostLease.Unregister('DisplayHostContract.Row')
    $builder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
        param($objects,$context)
        $t=[ConsoleMonster.Widgets.Table]::new();$t.Title='identity-host'
        $key=[ConsoleMonster.Widgets.TableColumn]::new('Name');$key.IsKey=$true;$key.Priority=100;$key.MinWidth=22;$key.MaxWidth=40;$null=$t.AddColumn($key)
        foreach($def in @(@('State',80),@('Neighbors',60),@('Failed',90),@('Stale Schema/Config/Domain/App',0),@('Last logon',0),@('Password set',0))){
            $c=[ConsoleMonster.Widgets.TableColumn]::new([string]$def[0]);$c.Priority=[int]$def[1];$null=$t.AddColumn($c)
        }
        $null=$t.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name),[ConsoleMonster.Widgets.TableCell]::FromValue('failing'),[ConsoleMonster.Widgets.TableCell]::FromValue(6),[ConsoleMonster.Widgets.TableCell]::FromValue(1),[ConsoleMonster.Widgets.TableCell]::FromValue(0),[ConsoleMonster.Widgets.TableCell]::FromValue($null),[ConsoleMonster.Widgets.TableCell]::FromValue($null)))
        return $t
    }
    $null=$global:StreamHostLease.Register('DisplayHostContract.Row','stream.identity',[version]'1.0.0','Stream',$null,$builder)
}
function global:Assert-IdentityStreamHost {
    param([string]$Name,[string]$First,[string]$Second)
    $text=[DisplayHostContract.ConsoleText]::Read();$width=[Console]::BufferWidth
    $lines=@(for($i=0;$i -lt [Math]::Min(100,[Console]::BufferHeight);$i++){$text.Substring($i*$width,$width).TrimEnd()})
    $firstRow=@($lines|Where-Object {$_.Contains($First)})
    $secondRow=@($lines|Where-Object {$_.Contains($Second)})
    $passed=$firstRow.Count -eq 1 -and $secondRow.Count -eq 1 -and $firstRow[0].Contains('failing') -and $secondRow[0].Contains('failing')
    $global:DisplayLoopResults.Add([pscustomobject]@{Name=$Name;Passed=[bool]$passed;BufferWidth=$width;ProfileWidth=[ConsoleMonster.Terminal]::GetCurrentProfile().Width;Lines=@($lines|Where-Object {$_})})
}
