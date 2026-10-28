# Script-module lifetime for integration tests; loads the supplied candidate without an assembly resolver.
param([Parameter(Mandatory=$true)][string]$BinaryPath)
Import-Module -Name $BinaryPath -Scope Local
Export-ModuleMember -Cmdlet *
