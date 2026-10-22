Import-Module ConsoleMonster

$module = (Get-Module ConsoleMonster).Path
Start-Job -ArgumentList $module {
    param($Module)
    Import-Module $Module
    [pscustomobject] @{ Check = 'SPF'; Status = 'Pass' }, [pscustomobject] @{ Check = 'DMARC'; Status = 'Fail' } |
        Write-ConsoleTable -Title 'From a background job' -StatusProperty Status
} | Receive-Job -Wait -AutoRemoveJob
