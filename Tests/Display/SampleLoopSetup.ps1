param([string]$AssemblyDirectory)
. (Join-Path $PSScriptRoot 'AdaptiveLoopSetup.ps1') -AssemblyDirectory $AssemblyDirectory
function global:Use-SampleView {
    param([long]$Bytes=16777216)
    Use-AdaptiveView -Bytes $Bytes
    $global:VerbosePreference='Continue'
    $global:SampleClosed=0; $global:SampleHeaders=0; $global:SampleThrow=$false
    $global:AdaptiveView.CollectedBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
        param($objects,$context)
        $global:SampleClosed++
        if($global:SampleThrow){throw 'Expected closed-sample failure.'}
        [ConsoleMonster.Widgets.TextBlock]::Plain('CLOSED<'+(($objects|ForEach-Object Name)-join ',')+'>')
    }
    $global:AdaptiveView.StreamingHeaderBuilder=[Func[object,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
        param($value,$context)
        $global:SampleHeaders++
        $header=[ConsoleMonster.Widgets.ViewHeader]::new('Sample stream',$null,[string[]]@())
        $header.Streaming=$true
        [ConsoleMonster.Widgets.ConsoleViewFrame]::new($header,[ConsoleMonster.Layout.IWidget[]]@())
    }
}
Use-SampleView
