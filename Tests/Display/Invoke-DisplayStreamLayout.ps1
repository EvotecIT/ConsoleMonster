[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.dll')
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll')
Add-Type -TypeDefinition 'namespace DisplayLayout { public sealed class Row { public string Name; public string Label; public long Size; } }'
$results=New-Object 'System.Collections.Generic.List[object]'
$lease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('Layout',[version]'1.0.0',[string[]]@('DisplayLayout.Row'))
$builder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
    param($objects,$context)
    $t=[ConsoleMonster.Widgets.Table]::new();$t.Title='layout-group'
    foreach($definition in @(@('Name',8,12,100,$false),@('Display',24,40,100,$true),@('State',8,12,80,$false),@('Size',9,14,60,$false),@('Free',9,14,60,$false),@('Account',12,20,0,$false))){
        $c=[ConsoleMonster.Widgets.TableColumn]::new([string]$definition[0]);$c.MinWidth=[int]$definition[1];$c.MaxWidth=[int]$definition[2];$c.Priority=[int]$definition[3];$c.Flexible=[bool]$definition[4]
        if($c.Header -eq 'Name'){$c.IsKey=$true}
        if($c.Header -in @('Size','Free')){$c.Alignment=[ConsoleMonster.Layout.HorizontalAlignment]::Right;$c.Presenter=[ConsoleMonster.Presenters.CellPresenters]::Bytes}
        $null=$t.AddColumn($c)
    }
    $null=$t.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name),[ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Label),[ConsoleMonster.Widgets.TableCell]::FromValue('running'),[ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Size),[ConsoleMonster.Widgets.TableCell]::FromValue([long]42),[ConsoleMonster.Widgets.TableCell]::FromValue('DOMAIN\long-service-account')))
    if($null -eq $global:LayoutPrototype){$global:LayoutPrototype=$t}
    return $t
}
function Remove-LayoutSgr([string]$Text){
    $plain=[regex]::Replace($Text,'\x1b\[[0-9;]*m','')
    ($plain -split '\r?\n'|ForEach-Object {
        $line=$_
        while($line -match '\x1b\[(\d+)G'){
            $match=[regex]::Match($line,'\x1b\[(\d+)G')
            $prefix=$line.Substring(0,$match.Index)
            $spaces=[Math]::Max(0,[int]$match.Groups[1].Value-1-[ConsoleMonster.Text.DisplayWidth]::Measure($prefix))
            $line=$prefix+(' '*$spaces)+$line.Substring($match.Index+$match.Length)
        }
        $line
    }) -join "`n"
}
try{
    $view=$lease.Register('DisplayLayout.Row','layout.rows',[version]'1.0.0','Stream',$null,$builder)
    foreach($width in 120,80,60){foreach($glyph in 'Full','Ascii'){foreach($plain in $false,$true){
        $name='Stream_LaterLongerRows_NoWrap_'+$width+'_'+$glyph+'_'+$(if($plain){'NoColor'}else{'Color'})
        $stream=$null
        try{
            $depth=if($plain){[ConsoleMonster.Capabilities.ColorDepth]::None}else{[ConsoleMonster.Capabilities.ColorDepth]::TrueColor}
            $mode=if($plain){[ConsoleMonster.Capabilities.OutputMode]::Plain}else{[ConsoleMonster.Capabilities.OutputMode]::Interactive}
            $profile=[ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width,30,$depth,[ConsoleMonster.Capabilities.GlyphTier]$glyph,$mode)
            $ctx=[ConsoleMonster.PowerShell.Display.DefaultDisplayContext]::new($profile,'scope',[ConsoleMonster.PowerShell.Display.DefaultDisplayMode]::Auto)
            $stream=[ConsoleMonster.PowerShell.Display.DefaultDisplayStream]::new($view,$ctx)
            $a=[DisplayLayout.Row]::new();$a.Name='svc1';$a.Label='DFS';$a.Size=1
            $b=[DisplayLayout.Row]::new();$b.Name='svc2';$b.Label='DFS Replication';$b.Size=1000000000000
            $c=[DisplayLayout.Row]::new();$c.Name='svc3';$c.Label='Backups and capacity unknown for an exceptionally long volume label';$c.Size=42
            $global:LayoutPrototype=$null
            $first=Remove-LayoutSgr ($stream.Append($a));$second=Remove-LayoutSgr ($stream.Append($b));$third=Remove-LayoutSgr ($stream.Append($c));$footer=Remove-LayoutSgr ($stream.Complete())
            $secondLines=@($second -split '\r?\n'|Where-Object {$_.Trim().Length -gt 0});$thirdLines=@($third -split '\r?\n'|Where-Object {$_.Trim().Length -gt 0})
            if($secondLines.Count -ne 1 -or $thirdLines.Count -ne 1){throw 'A streamed data row wrapped.'}
            if($second -notmatch 'DFS Replication' -or $third -notmatch 'Backups' -or $third -notmatch ([regex]::Escape([string][char]0x2026)+'|\.\.\.')){throw 'A later value lost its label or overflow marker.'}
            foreach($line in @($secondLines)+@($thirdLines)){if([ConsoleMonster.Text.DisplayWidth]::Measure($line,$profile.WidthProfile) -gt $width){throw ('A row exceeded the terminal width: '+[ConsoleMonster.Text.DisplayWidth]::Measure($line,$profile.WidthProfile)+' '+($line|ConvertTo-Json -Compress))}}
            if(($first -split '\r?\n'|Where-Object {$_ -match '\bName\b'}).Count -ne 1 -or $second -match '\bName\b' -or $third -match '\bName\b' -or $footer -notmatch '3 rows'){throw 'The stable header or group closure changed.'}
            if($width -eq 60 -and -not $first.Contains('+')){throw 'Low-priority columns were not dropped before cutting declared identity and display minima.'}
            if($null -ne $global:LayoutPrototype.Columns[0].Width -or $global:LayoutPrototype.Columns[0].Overflow -ne 'Wrap'){throw 'Column definitions were mutated.'}
            if($plain -and (($first+$second+$third+$footer).Contains([string][char]27))){throw 'NO_COLOR retained an escape.'}
            $results.Add([pscustomobject]@{Name=$name;Passed=$true;First=$first;Later=$second+$third;Footer=$footer})
        }catch{$results.Add([pscustomobject]@{Name=$name;Passed=$false;Error=$_.Exception.ToString()})}finally{if($stream){$stream.Dispose()}}
    }}}
    foreach($kind in 'Controller','Policy'){
        $global:LayoutIdentityKind=$kind
        $identityBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
            param($objects,$context)
            $t=[ConsoleMonster.Widgets.Table]::new();$t.Title='identity-layout'
            $key=[ConsoleMonster.Widgets.TableColumn]::new('Name');$key.IsKey=$true;$key.Priority=100;$key.MinWidth=22;$key.MaxWidth=40;$null=$t.AddColumn($key)
            foreach($def in @(@('State',80),@('Neighbors',60),@('Failed',90),@('Stale Schema/Config/Domain/App',0),@('Last logon',0),@('Password set',0))){
                $col=[ConsoleMonster.Widgets.TableColumn]::new([string]$def[0]);$col.Priority=[int]$def[1];$null=$t.AddColumn($col)
            }
            $null=$t.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name),[ConsoleMonster.Widgets.TableCell]::FromValue('failing'),[ConsoleMonster.Widgets.TableCell]::FromValue(6),[ConsoleMonster.Widgets.TableCell]::FromValue(1),[ConsoleMonster.Widgets.TableCell]::FromValue(0),[ConsoleMonster.Widgets.TableCell]::FromValue($null),[ConsoleMonster.Widgets.TableCell]::FromValue($null)))
            $global:IdentityPrototype=$t
            return $t
        }
        $identityView=$lease.Register('DisplayLayout.Row',('layout.identity.'+$kind),[version]'1.0.0','Stream',$null,$identityBuilder)
        foreach($width in 120,80,60){
            $stream=$null
            try{
                $profile=[ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width,30,[ConsoleMonster.Capabilities.ColorDepth]::None,[ConsoleMonster.Capabilities.GlyphTier]::Ascii,[ConsoleMonster.Capabilities.OutputMode]::Plain)
                $ctx=[ConsoleMonster.PowerShell.Display.DefaultDisplayContext]::new($profile,'scope',[ConsoleMonster.PowerShell.Display.DefaultDisplayMode]::Auto)
                $stream=[ConsoleMonster.PowerShell.Display.DefaultDisplayStream]::new($identityView,$ctx)
                $a=[DisplayLayout.Row]::new();$b=[DisplayLayout.Row]::new()
                if($kind -eq 'Controller'){$a.Name='KATOWICE-DC01.ad.evotec.xyz';$b.Name='KATOWICE-DC02.ad.evotec.xyz'}
                else{$a.Name='Default Domain Controllers Policy';$b.Name='Default Domain Policy'}
                $first=Remove-LayoutSgr ($stream.Append($a));$second=Remove-LayoutSgr ($stream.Append($b));$null=$stream.Complete()
                if(-not $first.Contains($a.Name) -or -not $second.Contains($b.Name)){throw ('Identifying names were shortened while optional columns retained space: '+$first+$second)}
                if(-not $second.Contains('failing') -or $second -notmatch '\b1\b'){throw 'Required state or failure count was hidden.'}
                foreach($line in ($first+$second) -split '\r?\n'){if([ConsoleMonster.Text.DisplayWidth]::Measure($line,$profile.WidthProfile) -gt $width){throw 'Identity allocation exceeded the physical width.'}}
                if($global:IdentityPrototype.Columns[0].Width -ne $null -or $global:IdentityPrototype.Columns[0].MinWidth -ne 22){throw 'Identity column contract was mutated.'}
                $results.Add([pscustomobject]@{Name=('Stream_IdentityBeforeEmptyColumns_'+$kind+'_'+$width);Passed=$true;First=$first;Later=$second})
            }catch{$results.Add([pscustomobject]@{Name=('Stream_IdentityBeforeEmptyColumns_'+$kind+'_'+$width);Passed=$false;Error=$_.Exception.ToString()})}finally{if($stream){$stream.Dispose()}}
        }
    }
    foreach($scenario in 'NarrowMinimum','LongIdentity'){
        $global:LayoutBoundaryScenario=$scenario
        $boundaryBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
            param($objects,$context)
            $t=[ConsoleMonster.Widgets.Table]::new();$t.Title='boundary-layout'
            $key=[ConsoleMonster.Widgets.TableColumn]::new('Name');$key.IsKey=$true;$key.Priority=100
            if($global:LayoutBoundaryScenario -eq 'NarrowMinimum'){$key.MinWidth=40}
            $null=$t.AddColumn($key)
            foreach($header in 'State','Failed'){$c=[ConsoleMonster.Widgets.TableColumn]::new($header);$c.Priority=90;$null=$t.AddColumn($c)}
            $null=$t.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name),[ConsoleMonster.Widgets.TableCell]::FromValue('failing'),[ConsoleMonster.Widgets.TableCell]::FromValue(1)))
            $t
        }
        $v=$lease.Register('DisplayLayout.Row',('layout.boundary.'+$scenario),[version]'1.0.0','Stream',$null,$boundaryBuilder)
        $stream=$null
        try{
            $width=if($scenario -eq 'NarrowMinimum'){30}else{80}
            $profile=[ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width,30,[ConsoleMonster.Capabilities.ColorDepth]::None,[ConsoleMonster.Capabilities.GlyphTier]::Ascii,[ConsoleMonster.Capabilities.OutputMode]::Plain)
            $ctx=[ConsoleMonster.PowerShell.Display.DefaultDisplayContext]::new($profile,'scope',[ConsoleMonster.PowerShell.Display.DefaultDisplayMode]::Auto)
            $stream=[ConsoleMonster.PowerShell.Display.DefaultDisplayStream]::new($v,$ctx)
            $a=[DisplayLayout.Row]::new();$a.Name=if($scenario -eq 'NarrowMinimum'){'DC01'}else{'Policy-'+('long-name-'*8)}
            $text=Remove-LayoutSgr ($stream.Append($a));$null=$stream.Complete()
            if(-not $text.Contains('failing') -or $text -notmatch '\b1\b'){throw 'The key starved its diagnostic state and failure count.'}
            if($scenario -eq 'NarrowMinimum' -and -not $text.Contains('DC01')){throw 'A declared minimum larger than a narrow terminal prevented record-card rendering.'}
            $results.Add([pscustomobject]@{Name=('Stream_IdentityBoundary_'+$scenario);Passed=$true;Text=$text})
        }catch{$results.Add([pscustomobject]@{Name=('Stream_IdentityBoundary_'+$scenario);Passed=$false;Error=$_.Exception.ToString()})}finally{if($stream){$stream.Dispose()}}
    }
}finally{$lease.Dispose();Remove-Variable LayoutPrototype -Scope Global -ErrorAction Ignore}
$report=[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();Tests=$results.ToArray();Failed=@($results|Where-Object {-not $_.Passed}).Count}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ResultsPath -Encoding UTF8
$report.Tests|Format-Table Name,Passed -AutoSize
if($report.Failed){throw 'Streaming layout contracts failed.'}
