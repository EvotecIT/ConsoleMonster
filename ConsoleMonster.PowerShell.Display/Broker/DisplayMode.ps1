[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Auto', 'Plain', 'Off')]
    [string] $Mode,
    [string[]] $Module
)
if ($Module) {
    foreach ($cmModuleName in $Module) {
        $cmDisplayType::SetMode($Mode, $cmModuleName)
    }
} else {
    $cmDisplayType::SetMode($Mode, $null)
}
