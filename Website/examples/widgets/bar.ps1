$failures = New-ConsoleBarChart ([ordered] @{
    'Domain controllers' = 14
    'Group Policy'       = 9
    'Kerberos'           = 6
    'DNS'                = 4
    'PKI'                = 2
})
$failures.BarToken = 'Fail'
$failures | Write-ConsoleWidget
