[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Auto', 'Plain', 'Off')]
    [string] $Mode,
    [string[]] $Module
)
if ($Module) {
    foreach ($cmModuleName in $Module) {
        [ConsoleMonster.PowerShell.Display.DefaultDisplay]::SetMode([ConsoleMonster.PowerShell.Display.DefaultDisplayMode]$Mode, $cmModuleName)
    }
} else {
    [ConsoleMonster.PowerShell.Display.DefaultDisplay]::SetMode([ConsoleMonster.PowerShell.Display.DefaultDisplayMode]$Mode, $null)
}
