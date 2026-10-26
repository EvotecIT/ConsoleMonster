<#
.SYNOPSIS
Reads literal commands and parameters with PowerShell's native AST without executing the source.
.PARAMETER Path
JSON array of source blocks. Output is index, tab and command/parameter, followed by a parsed-count receipt.
#>
param([Parameter(Mandatory)] [string] $Path)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$blocks = [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8) | ConvertFrom-Json
$index = 0
foreach ($block in @($blocks)) {
    $tokens = $null
    $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseInput([string] $block, [ref] $tokens, [ref] $errors)
    if ($errors.Count) { throw ($errors | ForEach-Object Message | Out-String) }
    foreach ($command in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)) {
        $name = $command.GetCommandName()
        if (-not $name) { continue }
        $name = ($name.Split([char]92))[-1]
        "{0}`t{1}" -f $index, $name
        foreach ($parameter in $command.CommandElements) {
            if ($parameter -is [System.Management.Automation.Language.CommandParameterAst]) {
                "{0}`t{1} -{2}" -f $index, $name, $parameter.ParameterName
            }
        }
    }
    $index++
}
"parsed $index"
