[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
$results=New-Object 'System.Collections.Generic.List[object]'
function Assert-HostContract([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Test-HostContract([string]$Name,[scriptblock]$Action){
    try{ & $Action; $results.Add([pscustomobject]@{Name=$Name;Passed=$true;Error=$null}) }
    catch{$results.Add([pscustomobject]@{Name=$Name;Passed=$false;Error=$_.Exception.ToString()})}
}
try {
    # This script runs in its own real console process, without redirected OS handles. Only that test process is changed.
    foreach($name in 'CI','TF_BUILD','GITHUB_ACTIONS','GITLAB_CI','CODEX_CI','CONSOLEMONSTER_DEFAULT_VIEWS','NO_COLOR'){
        [Environment]::SetEnvironmentVariable($name,$null,'Process')
    }
    $core=Join-Path $AssemblyDirectory 'ConsoleMonster.dll'
    Add-Type -Path $core
    Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll')
    Add-Type -Path (Join-Path $PSScriptRoot 'Fixtures/DisplayHostFixture.cs') -ReferencedAssemblies @($core,[Console].Assembly.Location)
    $displayProfile=[ConsoleMonster.Terminal]::GetCurrentProfile()
    Assert-HostContract (-not [Console]::IsOutputRedirected -and $displayProfile.IsConsoleHandle) 'This proof requires a real non-redirected console process.'
    $script:HostBuilds=0
    $builder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{
        param($objects,$context)
        $script:HostBuilds++
        [ConsoleMonster.Widgets.TextBlock]::Plain('RICH '+$objects[0].Name)
    }
    $lease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('HostContract',[version]'1.0.0',[string[]]@('DisplayHostContract.Row'))
    $view=$lease.Register('DisplayHostContract.Row','host.row',[version]'1.0.0','Single',[string[]]@('Scope'),$builder,$null)
    function Invoke-HostSubmission([object[]]$Items){
        $pipeline=[PowerShell]::Create([Management.Automation.RunspaceMode]::CurrentRunspace)
        try {
            # A nested API submission retains Internal provenance even with an appended Out-Default.
            $null=$pipeline.AddScript('param($items) $items').AddArgument($Items).AddCommand('Out-Default')
            $null=$pipeline.Invoke()
            if($pipeline.HadErrors){throw ($pipeline.Streams.Error|Out-String)}
        } finally{$pipeline.Dispose()}
    }
    Test-HostContract 'NestedSubmission_InternalProvenance_RemainsNative' {
        $before=$script:HostBuilds
        Invoke-HostSubmission @([DisplayHostContract.Row]::new('single','a'))
        Assert-HostContract ($script:HostBuilds -eq $before) 'An internal nested submission forced rich display.'
    }
    Test-HostContract 'NativeSinks_AssignmentFormatOutStringJson_PreserveOriginalObjects' {
        $row=[DisplayHostContract.Row]::new('native','a')
        $before=$script:HostBuilds
        $captured=$row
        $text=$row|Out-String
        $explicit=$row|Format-List Name|Out-String
        $json=$row|ConvertTo-Json -Compress
        Assert-HostContract ([object]::ReferenceEquals($row,$captured)) 'Assignment replaced the object.'
        Assert-HostContract ($script:HostBuilds -eq $before -and $text -match 'native' -and $explicit -match 'native' -and $json -match 'native') 'A non-host sink ran the rich builder.'
    }
    Test-HostContract 'ActiveTranscript_NestedSubmission_DoesNotInvokeBuilder' {
        $before=$script:HostBuilds
        $path=[IO.Path]::ChangeExtension($ResultsPath,'active-transcript.txt')
        try{Start-Transcript -LiteralPath $path|Out-Null;Invoke-HostSubmission @([DisplayHostContract.Row]::new('transcript-native','a'))}
        finally{Stop-Transcript|Out-Null}
        Assert-HostContract ($script:HostBuilds -eq $before) 'Active transcription ran the rich builder.'
    }
    Test-HostContract 'Collect_UnknownIndependentStreamRoute_FailsClosedFromFirstRow' {
        $lease.Register('DisplayHostContract.Row','host.row',[version]'1.1.0','Collect',[string[]]@('Scope'),$builder,$null)|Out-Null
        $before=$script:HostBuilds
        Invoke-HostSubmission @([DisplayHostContract.Row]::new('collect-native','a'))
        Assert-HostContract ($script:HostBuilds -eq $before) 'Unproven Collect routing rendered richly.'
    }
    Test-HostContract 'WidgetFailure_PreparationWritesNoPartialFrame' {
        $widget=[DisplayHostContract.BrokenWidget]::new()
        $thrown=$false
        try{[ConsoleMonster.PowerShell.Display.DefaultDisplayRenderer]::Prepare($widget,$displayProfile)|Out-Null}catch{$thrown=$true}
        Assert-HostContract $thrown 'A partial-render failure was not surfaced before host write.'
    }
    $lease.Dispose();$lease=$null
} catch {
    $results.Add([pscustomobject]@{Name='HostHarness';Passed=$false;Error=$_.Exception.ToString()})
} finally {if($lease){$lease.Dispose()}}
$report=[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();PhysicalProfile=$displayProfile;Tests=$results.ToArray();Passed=@($results|Where-Object Passed).Count;Failed=@($results|Where-Object{-not $_.Passed}).Count}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ResultsPath -Encoding UTF8
if($report.Failed){exit 1}
