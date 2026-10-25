<#
.SYNOPSIS
Parses every PowerShell script in the module without executing it.
.DESCRIPTION
Uses the running host's parser, including its file encoding rules. CI runs this
check with Windows PowerShell 5.1 before building or importing the module.
.PARAMETER ModuleRoot
Module directory to inspect. Defaults to this repository's Module directory.
.EXAMPLE
powershell.exe -NoProfile -File ./Module/Tests/Support/Test-ModuleScriptSyntax.ps1
#>
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string] $ModuleRoot = (Join-Path $PSScriptRoot '../..')
)

$ErrorActionPreference = 'Stop'
$scripts = @(Get-ChildItem -LiteralPath $ModuleRoot -Filter '*.ps1' -File -Recurse -Force | Sort-Object FullName)
if ($scripts.Count -eq 0) {
    throw "No PowerShell scripts found under $ModuleRoot."
}

$failures = @(
    foreach ($script in $scripts) {
        $tokens = $null
        $parseErrors = $null
        $null = [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref] $tokens, [ref] $parseErrors)
        foreach ($parseError in $parseErrors) {
            '{0}:{1}:{2}: {3} ({4})' -f $script.FullName, $parseError.Extent.StartLineNumber,
                $parseError.Extent.StartColumnNumber, $parseError.Message, $parseError.ErrorId
        }
    }
)
if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host $_ }
    throw "$($failures.Count) parse errors in Module scripts under PowerShell $($PSVersionTable.PSVersion)."
}
Write-Host "Parsed $($scripts.Count) Module scripts with PowerShell $($PSVersionTable.PSVersion)."
