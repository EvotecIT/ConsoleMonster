Import-Module ConsoleMonster

$server = [pscustomobject]@{
    Name = 'web-01'
    Owner = [pscustomobject]@{
        PSTypeName = 'OwnerInfo'
        Name = 'Ada'
        Groups = @('Operators', 'Readers')
    }
    Services = @(
        [pscustomobject]@{ Name = 'web'; Status = 'Running'; Port = 443 }
        [pscustomobject]@{ Name = 'metrics'; Status = 'Running'; Port = 9090 }
    )
    Enabled = $true
}
$server | Show-ConsoleObject -Title 'Server configuration' -RootPath '$server'
