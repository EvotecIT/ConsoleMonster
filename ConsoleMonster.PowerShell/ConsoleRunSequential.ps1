param($__cmBody, $__cmItem, $__cmQuiet, $__cmOutput)
$__cmProgress = Get-Variable -Name ProgressPreference -Scope 0 -ErrorAction Ignore
$__cmProgressValue = if ($__cmProgress) { $__cmProgress.Value } else { $null }
$__cmGlobalProgress = $global:ProgressPreference
# Script-module functions (Compress-Archive, Install-Module) read the global value, not the caller's scope.
if ($__cmQuiet) {
    $ProgressPreference = 'SilentlyContinue'
    $global:ProgressPreference = 'SilentlyContinue'
}
# The dot-sourced block makes the redirection reach native programs inside the body (their stderr lines).
try {
    . { , $__cmItem | ForEach-Object -Process $__cmBody } *>&1 | ForEach-Object { $__cmOutput.Add($_) }
} finally {
    if ($__cmQuiet) {
        $global:ProgressPreference = $__cmGlobalProgress
        if ($__cmProgress) {
            $ProgressPreference = $__cmProgressValue
        } else {
            Remove-Variable -Name ProgressPreference -Scope 0 -ErrorAction Ignore
        }
    }
    Remove-Variable -Name __cmBody, __cmItem, __cmQuiet, __cmOutput, __cmProgress, __cmProgressValue, __cmGlobalProgress -Scope 0 -ErrorAction Ignore
}
