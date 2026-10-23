function Get-HelpFormattingCapture {
    <#
    .SYNOPSIS
    Captures native help contracts for comparison before and after Help formatting is enabled.
    .PARAMETER Command
    The help invocation to execute, including its parameters.
    .PARAMETER Directory
    An existing test-owned directory for file comparisons.
    #>
    param([string] $Command, [string] $Directory)
    $invoke = [scriptblock]::Create($Command)
    $records = @(& $invoke)
    $out = Join-Path $Directory 'out.txt'
    $redirect = Join-Path $Directory 'redirect.txt'
    & ([scriptblock]::Create($Command + ' | Out-File -LiteralPath $out -Width 100'))
    & ([scriptblock]::Create($Command + ' > $redirect'))
    $captured = @()
    # help's native function also accepts common parameters; it must retain their exact native behaviour.
    & ([scriptblock]::Create($Command + ' -OutVariable captured | Out-Null'))
    [pscustomobject]@{
        Records = [Management.Automation.PSSerializer]::Serialize($records, 20)
        Text = & ([scriptblock]::Create($Command + ' | Out-String -Width 100'))
        Selection = @(& ([scriptblock]::Create($Command + ' | Out-String -Stream -Width 100 | Select-String HELP_'))) -join "`n"
        Types = @(& ([scriptblock]::Create($Command + ' | ForEach-Object { $_.PSTypeNames -join '','' }'))) -join "`n"
        File = [Convert]::ToBase64String([IO.File]::ReadAllBytes($out))
        Redirect = [Convert]::ToBase64String([IO.File]::ReadAllBytes($redirect))
        OutVariable = [Management.Automation.PSSerializer]::Serialize($captured, 20)
    }
}
