param($__cmPrevious, $__cmOutDefault, $__cmReset, $__cmMarker)
{
    $__cmProgressCleanup = $true
    $__cmFunction = ${function:global:Out-Default}
    if ($__cmFunction -and $__cmFunction.ToString().IndexOf($__cmMarker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        if ($__cmOutDefault) {
            Microsoft.PowerShell.Management\Set-Item -Path 'Function:\global:Out-Default' -Value $__cmOutDefault
        } else {
            Microsoft.PowerShell.Management\Remove-Item -Path 'Function:\Out-Default' -Force
        }
    }
    $__cmReset.Invoke()
    if ($__cmPrevious) { & $__cmPrevious @args }
}.GetNewClosure()
