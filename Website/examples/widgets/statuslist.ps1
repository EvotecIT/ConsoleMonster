Import-Module ConsoleMonster

New-ConsoleStatusList -Content {
    [pscustomobject]@{ Label = 'Restore NuGet packages'; Status = 'Pass'; Detail = '1.6 s' }
    [pscustomobject]@{ Label = 'Compile ConsoleMonster'; Status = 'Pass'; Detail = '4.7 s' }
    [pscustomobject]@{ Label = 'Run Pester on PowerShell 7'; Status = 'Warn'; Detail = '1 skipped' }
    [pscustomobject]@{ Label = 'Sign module files'; Status = 'Skip'; Detail = 'skipped' }
    [pscustomobject]@{ Label = 'Pack ConsoleMonster.2.0.0.nupkg'; Status = 'Running' }
    [pscustomobject]@{ Label = 'Publish to PowerShell Gallery'; Status = 'Pending' }
} | Show-Console
