param([string]$AssemblyDirectory)
$ErrorActionPreference='Stop'
foreach($name in 'CI','TF_BUILD','GITHUB_ACTIONS','GITLAB_CI','CODEX_CI','CONSOLEMONSTER_DEFAULT_VIEWS','NO_COLOR'){
    [Environment]::SetEnvironmentVariable($name,$null,'Process')
}
$core=Join-Path $AssemblyDirectory 'ConsoleMonster.dll'
Add-Type -Path $core
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll')
$fixtureOptions=@{}
# PS7 may compile the net8 fixture references on .NET 10; suppress only assembly-version unification CS1701.
if($PSVersionTable.PSVersion.Major -eq 7){$fixtureOptions.CompilerOptions=@('/nowarn:1701')}
Add-Type -Path (Join-Path $PSScriptRoot 'Fixtures/DisplayHostFixture.cs') -ReferencedAssemblies @($core,[Console].Assembly.Location) @fixtureOptions
$global:RouteModule=Import-Module (Join-Path $PSScriptRoot 'Fixtures/ConsoleMonster.psm1') -ArgumentList (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.dll') -PassThru
$global:RouteResults=New-Object 'System.Collections.Generic.List[object]'
function global:Assert-CommandRoute([string]$Name,[bool]$Condition){
    $global:RouteResults.Add([pscustomobject]@{Name=$Name;Passed=$Condition})
}
function global:Test-DisplayCommandRoute {
    [CmdletBinding()]
    param([switch]$HelpRoute)
    $feature=if($HelpRoute){[ConsoleMonster.PowerShell.ConsoleFormattingFeature]::Help}else{[ConsoleMonster.PowerShell.ConsoleFormattingFeature]::FormatCommands}
    $global:RouteDirect=[ConsoleMonster.PowerShell.FormatCommandRouter]::IsDirectHost($PSCmdlet,$feature)
    [ConsoleMonster.PowerShell.HelpHostFormatting]::BeginPaging($PSCmdlet)
    $paged=[ConsoleMonster.PowerShell.HelpHostFormatting].GetMethod('IsPaged',[Reflection.BindingFlags]'Static,NonPublic')
    $global:RoutePaged=[bool]$paged.Invoke($null,@($PSCmdlet))
    [ConsoleMonster.PowerShell.HelpHostFormatting]::EndPaging($PSCmdlet)
    $global:RoutePagedAfter=[bool]$paged.Invoke($null,@($PSCmdlet))
    'route-output'
}
Enable-ConsoleFormatting -Feature FormatCommands,Help -NoBuiltIn
