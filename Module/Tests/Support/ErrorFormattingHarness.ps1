function Get-ErrorFormattingSnapshot {
    <#
    .SYNOPSIS
    Captures native error routes for comparison before and after enabling Errors.
    .PARAMETER Record
    One unchanged synthetic ErrorRecord used in both passes.
    .PARAMETER Directory
    An existing task-owned temporary directory for byte comparisons.
    .OUTPUTS
    An object containing record serialization, continuation, text and exact file bytes.
    #>
    [CmdletBinding()]
    param(
        [Management.Automation.ErrorRecord] $Record,
        [string] $Directory
    )
    $assigned = $Record
    $file = Join-Path $Directory 'error.txt'
    $Record | Out-File -LiteralPath $file -Width 80
    $bytes = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    $Record > $file
    $redirect = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    Write-Error -ErrorRecord $Record -ErrorAction Continue 2>$file
    $stderr = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    $merged = & { Write-Error -ErrorRecord $Record -ErrorAction Continue } 2>&1 | Out-String -Width 80
    [pscustomobject]@{
        Identity = [object]::ReferenceEquals($assigned, $Record)
        Record = [Management.Automation.PSSerializer]::Serialize($assigned, 10)
        Type = ($Record | ForEach-Object { $_.GetType().FullName })
        Text = ($Record | Out-String -Width 80)
        File = $bytes
        Redirect = $redirect
        ErrorRedirect = $stderr
        MergedErrorText = $merged
    }
}
