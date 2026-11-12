param($TypeName)
$parameters = @{
    TypeName = $TypeName
    ErrorAction = 'Ignore'
}
if ((Get-Command -Name Get-FormatData).Parameters.ContainsKey('PowerShellVersion')) {
    $parameters.PowerShellVersion = $PSVersionTable.PSVersion
}
Get-FormatData @parameters
