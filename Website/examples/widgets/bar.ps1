Import-Module ConsoleMonster

New-ConsoleChart -Role Fail -InputObject ([ordered] @{
    'Domain controllers' = 14
    'Group Policy'       = 9
    'Kerberos'           = 6
    'DNS'                = 4
    'PKI'                = 2
}) -Type Bar | Show-Console
