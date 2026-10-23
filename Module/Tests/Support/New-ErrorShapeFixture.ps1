function New-ErrorShapeFixture {
    <#
    .SYNOPSIS
    Builds synthetic cmdlets and a deterministic script supplying real invocation extents.
    .PARAMETER Directory
    An owned existing test or capture directory.
    .PARAMETER MixedOutput
    Streams success and error records from the compiled command without an assignment.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string] $Directory, [switch] $MixedOutput)
    if (-not ('ConsoleMonsterErrorShapes.ErrorShapeFactory' -as [type])) {
        $assemblyPath = Join-Path $Directory ('ErrorShapeCmdlets-' + [guid]::NewGuid().ToString('N') + '.dll')
        Add-Type -Path (Join-Path $PSScriptRoot 'ErrorShapeCmdlets.cs') -OutputAssembly $assemblyPath -ErrorAction Stop
        $assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($assemblyPath))
        Import-Module -Assembly $assembly -ErrorAction Stop
    }
    $source = Join-Path $Directory 'Invoke-SyntheticInventory.ps1'
    $lines = @(
        'param([string] $Shape)',
        '$command = Get-Command Test-SyntheticErrorShape',
        '$arguments = @{ Shape = $Shape }',
        '$x = @(& $command @arguments)',
        '$x'
    )
    $name = 'Invoke-SyntheticInventory.ps1'
    if ($MixedOutput) {
        $lines = @(
            '$ErrorActionPreference = "Continue"',
            'Test-SyntheticErrorShape -Shape mixed-output'
        )
        $name = 'Invoke-SyntheticOutput.ps1'
    }
    [IO.File]::WriteAllLines($source, $lines, [Text.UTF8Encoding]::new($false))
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseInput(($lines -join "`n"), $name, [ref] $tokens, [ref] $errors)
    if ($errors.Count) { throw 'The synthetic invocation script did not parse.' }
    $ast.GetScriptBlock()
}
