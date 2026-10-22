# Get-Module only inspects loaded modules. Command lookup by name could autoload PSReadLine.
foreach ($module in Microsoft.PowerShell.Core\Get-Module -Name PSReadLine) {
    $getter = $module.ExportedCmdlets['Get-PSReadLineOption']
    if ($getter) { & $getter; break }
}
