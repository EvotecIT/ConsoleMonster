[CmdletBinding()]
param(
    [Parameter(Position = 0)][string] $Activity,
    [Parameter(Position = 1)][string] $Status,
    [Parameter(Position = 2)][int] $Id = 0,
    [int] $PercentComplete = -1,
    [int] $SecondsRemaining = -1,
    [string] $CurrentOperation,
    [int] $ParentId = -1,
    [switch] $Completed,
    [int] $SourceId
)
if ($ProgressPreference -eq 'SilentlyContinue' -or $ProgressPreference -eq 'Ignore') {
    return
}
$text = if ($CurrentOperation) {
    if ($Status) { $Status + ' - ' + $CurrentOperation } else { $CurrentOperation }
} else {
    $Status
}
$__cmProgressBridge.Progress($Id, $ParentId, $Activity, $text, $PercentComplete, $SecondsRemaining, $Completed.IsPresent)
