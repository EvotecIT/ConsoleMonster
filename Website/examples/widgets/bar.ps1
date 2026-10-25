Import-Module ConsoleMonster

New-ConsoleBarChart -Role Fail -InputObject ([ordered] @{
    'Domain controllers' = 14
    'Group Policy'       = 9
    'Kerberos'           = 6
    'DNS'                = 4
    'PKI'                = 2
}) | Show-Console
