Import-Module ConsoleMonster
Enable-ConsoleFormatting

# Someone else's command: typed objects, no view of its own.
function Get-ContosoBackup {
    $took = [timespan] '00:41:00'
    [pscustomobject] @{ PSTypeName = 'Contoso.Backup'; Server = 'sql01'
        State = 'Completed'; Size = 52GB; Took = $took }
    [pscustomobject] @{ PSTypeName = 'Contoso.Backup'; Server = 'web01'
        State = 'Completed'; Size = 3GB; Took = [timespan] '00:04:10' }
    [pscustomobject] @{ PSTypeName = 'Contoso.Backup'; Server = 'files01'
        State = 'Failed'; Size = 0; Took = [timespan] '00:12:30' }
}

Get-ContosoBackup

# The first type name is the one to register.
(Get-ContosoBackup)[0].PSObject.TypeNames[0]

$state = @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }
$size = @{ Name = 'Size'; Expression = { $_.Size }; Presenter = 'BytesBar' }
$took = @{ Name = 'Took'; Expression = { $_.Took }; Presenter = 'Duration' }
Register-ConsoleView -TypeName Contoso.Backup -Property Server, $state, $size, $took -Summary {
    param($backups)
    @{ Value = $backups.Count; Label = 'backups' }
    $failed = @($backups | Where-Object State -eq 'Failed').Count
    if ($failed) { @{ Value = $failed; Label = 'failed'; Role = 'Fail' } }
}

Get-ContosoBackup
