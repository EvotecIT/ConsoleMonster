<#
Capture-only setup for recorded demos: drives the program with scripted keys whose pauses become recording holds,
so the replay keeps its reading pauses without the capture sleeping. Never part of a shown example.
#>
param([Parameter(Mandatory)][string] $Keys)

$assembly = [ConsoleMonster.Terminal].Assembly
$recorder = $assembly.GetType('ConsoleMonster.Sessions.SessionRecorder')
if (-not ('SiteFramesDemoHolds' -as [type])) {
    # Input is read off the PowerShell pipeline thread, so the hold writer is compiled rather than a script block.
    Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
public static class SiteFramesDemoHolds {
    public static readonly Dictionary<long, string> Markers = new Dictionary<long, string>();
    public static readonly Action<TimeSpan> Write = hold => {
        string marker;
        if (Markers.TryGetValue((long)Math.Round(hold.TotalMilliseconds), out marker)) { Console.Out.Write(marker); Console.Out.Flush(); }
    };
}
"@
}
foreach ($match in [regex]::Matches($Keys, 'wait:(\d+)')) {
    $milliseconds = [long] $match.Groups[1].Value
    [SiteFramesDemoHolds]::Markers[$milliseconds] = $recorder::MarkerSequence([TimeSpan]::FromMilliseconds($milliseconds), $null)
}
$scripted = ($assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new($Keys)
# A frame after every key, and each pause sent to the recorder as a hold.
$scripted.Paced = $true
$scripted.SkippedWait = [SiteFramesDemoHolds]::Write
($assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::Override = $scripted