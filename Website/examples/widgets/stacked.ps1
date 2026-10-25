Import-Module ConsoleMonster

New-ConsoleChart -Content {
    [pscustomobject]@{ Label = 'Passed'; Value = 412; Role = 'Pass' }
    [pscustomobject]@{ Label = 'Warnings'; Value = 51; Role = 'Warn' }
    [pscustomobject]@{ Label = 'Failed'; Value = 23; Role = 'Fail' }
    [pscustomobject]@{ Label = 'Skipped'; Value = 14; Role = 'Skip' }
} -Type Stacked | Show-Console
