[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$AssemblyDirectory, [Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.dll')
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll')
Add-Type -TypeDefinition 'namespace DisplayJoinContract { public sealed class Row { public string Name {get;set;} } public sealed class AutoRow { public string Name {get;set;} } public sealed class OnlyV2 { public string Name {get;set;} } }'
$results=New-Object 'System.Collections.Generic.List[object]'
function Assert-Join([bool]$Condition,[string]$Message) { if(-not $Condition){throw $Message} }
function Test-Join([string]$Name,[scriptblock]$Action) {
    try { & $Action; $results.Add([pscustomobject]@{Name=$Name;Passed=$true;Error=$null}) }
    catch { $results.Add([pscustomobject]@{Name=$Name;Passed=$false;Error=$_.Exception.ToString()}) }
}
$builder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]] {
    param($objects,$context) [ConsoleMonster.Widgets.TextBlock]::Plain('product')
}
$before=Get-Command Out-Default
$savedEnv=$env:CONSOLEMONSTER_DEFAULT_VIEWS
$env:CONSOLEMONSTER_DEFAULT_VIEWS=$null
$module=$null
$lease=$null
$path=[IO.Path]::ChangeExtension($ResultsPath,'consoleviews.psd1')
try {
    $module=Import-Module (Join-Path $PSScriptRoot 'Fixtures/ConsoleMonster.psm1') -ArgumentList (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.dll') -PassThru
    $lease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('JoinProduct',[version]'1.0.0',[string[]]@('DisplayJoinContract.Row'))
    $view=$lease.Register('DisplayJoinContract.Row','join.row',[version]'1.0.0','Single',$null,$builder,$null)
    Test-Join 'Enable_ExistingProductBroker_JoinsWithoutReplacement' {
        $owned=(Get-Command Out-Default).ScriptBlock
        Enable-ConsoleFormatting -Feature Output -NoBuiltIn
        Assert-Join ([ConsoleMonster.PowerShell.Display.DefaultDisplay]::IsBrokerInstalled) 'Enable replaced or failed to recognize the broker.'
        Assert-Join ([object]::ReferenceEquals($owned,(Get-Command Out-Default).ScriptBlock)) 'Enable installed a second broker.'
    }
    Test-Join 'Register_UserViewWinsAndUnregisterRestoresProduct' {
        Register-ConsoleView -TypeName 'DisplayJoinContract.Row' -View Table -Property Name
        Assert-Join ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new())) 'An explicit user view did not override the product builder.'
        Unregister-ConsoleView -TypeName 'DisplayJoinContract.Row' -Confirm:$false
        Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'Unregister removed or hid the product view.'
    }
    Test-Join 'Disable_CommandLeaseOnly_PreservesProductBroker' {
        Disable-ConsoleFormatting -Feature Output
        Assert-Join ([ConsoleMonster.PowerShell.Display.DefaultDisplay]::IsBrokerInstalled) 'Disabling the command module removed the product broker.'
        Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'Disabling removed the product view.'
    }
    Test-Join 'SchemaV2_RegisterAndUnregisterPath_SharesUserPrecedence' {
        Enable-ConsoleFormatting -Feature Output -NoBuiltIn
        $path=[IO.Path]::ChangeExtension($ResultsPath,'consoleviews.psd1')
        "@{SchemaVersion=2;Views=@(@{TypeName='DisplayJoinContract.Row';View='Table';Property=@('Name');Batch='Single';ScopeKey=@('Name');Budget=@{Records=20;Bytes=4096}})}" | Set-Content -LiteralPath $path -Encoding UTF8
        Register-ConsoleView -Path $path
        $selected=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new())
        Assert-Join ($selected -and -not [object]::ReferenceEquals($selected,$view)) 'Schema v2 did not join the shared registry.'
        Unregister-ConsoleView -Path $path -Confirm:$false
        Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'Removing a schema v2 file view did not restore the product view.'
    }
    Test-Join 'SchemaV2_DisableOutput_SuspendsUserAndDiscoveredFilesAndReenableRestoresSavedViews' {
        $autoDirectory = Join-Path ([IO.Path]::GetDirectoryName($ResultsPath)) ([IO.Path]::GetFileNameWithoutExtension($ResultsPath)+'-auto')
        $null = New-Item -ItemType Directory -Path $autoDirectory -Force
        $autoName = [IO.Path]::GetFileName($autoDirectory)
        $autoScript = Join-Path $autoDirectory ($autoName+'.psm1')
        '' | Set-Content -LiteralPath $autoScript -Encoding UTF8
        "@{SchemaVersion=2;Views=@(@{TypeName='DisplayJoinContract.AutoRow';View='Table';Property='Name'})}" | Set-Content -LiteralPath (Join-Path $autoDirectory ($autoName+'.consoleviews.psd1')) -Encoding UTF8
        $autoModule = Import-Module $autoScript -PassThru
        $autoLease = [ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('AutoProduct',[version]'1.0.0',[string[]]@('DisplayJoinContract.AutoRow'))
        try {
            $autoProduct = $autoLease.Register('DisplayJoinContract.AutoRow','join.auto',[version]'1.0.0','Single',$null,$builder)
            Register-ConsoleView -Path $path
            Enable-ConsoleFormatting -Feature Output,FormatCommands -NoBuiltIn
            Assert-Join (-not [object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'The user file did not activate.'
            Assert-Join (-not [object]::ReferenceEquals($autoProduct,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.AutoRow]::new()))) 'The discovered file did not activate.'
            Disable-ConsoleFormatting -Feature Output
            Assert-Join ([ConsoleMonster.PowerShell.Display.DefaultDisplay]::IsBrokerInstalled) 'Disabling Output removed the product broker.'
            Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'A disabled user file stayed active.'
            Assert-Join ([object]::ReferenceEquals($autoProduct,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.AutoRow]::new()))) 'A disabled discovered file stayed active.'
            Register-ConsoleView -Path $path
            Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'Registering while Output was disabled activated a file.'
            Enable-ConsoleFormatting -Feature Output -NoBuiltIn
            Assert-Join (-not [object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'Re-enable did not restore the saved user file.'
            Assert-Join (-not [object]::ReferenceEquals($autoProduct,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.AutoRow]::new()))) 'Re-enable did not restore the saved discovered file.'
            Disable-ConsoleFormatting -Feature Output
            Assert-Join ([ConsoleMonster.PowerShell.ConsoleHostViews]::Unregister('DisplayJoinContract.Row')) 'Disabled saved removal was not reported.'
            Assert-Join ([ConsoleMonster.PowerShell.ConsoleHostViews]::Unregister('DisplayJoinContract.AutoRow')) 'Disabled discovered removal was not reported.'
            Enable-ConsoleFormatting -Feature Output -NoBuiltIn
            Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'Re-enable resurrected a removed user view.'
            Assert-Join ([object]::ReferenceEquals($autoProduct,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.AutoRow]::new()))) 'Re-enable resurrected a removed discovered view.'
        } finally { $autoLease.Dispose(); Remove-Module $autoModule.Name; Disable-ConsoleFormatting -Feature FormatCommands }
    }
    Test-Join 'SchemaV2_OnlyRegistration_UnregisterReportsExistenceThenAbsence' {
        $onlyPath = [IO.Path]::ChangeExtension($ResultsPath,'only.consoleviews.psd1')
        "@{SchemaVersion=2;Views=@(@{TypeName='DisplayJoinContract.OnlyV2';View='Table';Property='Name'})}" | Set-Content -LiteralPath $onlyPath -Encoding UTF8
        Register-ConsoleView -Path $onlyPath
        Assert-Join ([ConsoleMonster.PowerShell.ConsoleHostViews]::Unregister('DisplayJoinContract.OnlyV2')) 'An existing v2-only registration returned false.'
        Assert-Join (-not [ConsoleMonster.PowerShell.ConsoleHostViews]::Unregister('DisplayJoinContract.OnlyV2')) 'An absent registration returned true.'
    }
    Test-Join 'ModuleOnRemove_ReleasesOnlyItsLease' {
        Enable-ConsoleFormatting -Feature Output -NoBuiltIn
        Register-ConsoleView -Path $path
        Assert-Join (-not [object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'The file fixture was not active before module removal.'
        Remove-Module $module.Name
        $module=$null
        Assert-Join ([ConsoleMonster.PowerShell.Display.DefaultDisplay]::IsBrokerInstalled) 'Command module removal removed the product broker.'
        Assert-Join ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayJoinContract.Row]::new()))) 'An active file lease survived module removal.'
        $lease.Dispose(); $lease=$null
        Assert-Join ((Get-Command Out-Default).CommandType -eq $before.CommandType) 'A command-module lease survived module removal.'
    }
    Test-Join 'CustomOutDefault_BeforeAndAfterImport_IsPreserved' {
        function global:Out-Default { [CmdletBinding()]param([Parameter(ValueFromPipeline=$true)]$InputObject) process{} }
        $custom=(Get-Command Out-Default).ScriptBlock
        $lease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('Custom',[version]'1.0.0',[string[]]@('DisplayJoinContract.Row'))
        Assert-Join (-not [ConsoleMonster.PowerShell.Display.DefaultDisplay]::IsBrokerInstalled) 'A custom function was replaced.'
        $lease.Dispose();$lease=$null
        Assert-Join ([object]::ReferenceEquals($custom,(Get-Command Out-Default).ScriptBlock)) 'A pre-existing custom function was removed.'
        Remove-Item Function:Out-Default
        $lease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule('Replacement',[version]'1.0.0',[string[]]@('DisplayJoinContract.Row'))
        function global:Out-Default { [CmdletBinding()]param([Parameter(ValueFromPipeline=$true)]$InputObject) process{} }
        $replacement=(Get-Command Out-Default).ScriptBlock
        $lease.Dispose();$lease=$null
        Assert-Join ([object]::ReferenceEquals($replacement,(Get-Command Out-Default).ScriptBlock)) 'A later user replacement was removed.'
        Remove-Item Function:Out-Default
    }
} finally {
    if($module -and (Get-Module $module.Name)){Remove-Module $module.Name}
    if($lease){$lease.Dispose()}
    $env:CONSOLEMONSTER_DEFAULT_VIEWS=$savedEnv
}
$report=[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();Tests=$results.ToArray();Passed=@($results|Where-Object Passed).Count;Failed=@($results|Where-Object{-not $_.Passed}).Count}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ResultsPath -Encoding UTF8
$report.Tests|Format-Table Name,Passed -AutoSize
if($report.Failed){throw "$($report.Failed) coexistence tests failed; see $ResultsPath"}
