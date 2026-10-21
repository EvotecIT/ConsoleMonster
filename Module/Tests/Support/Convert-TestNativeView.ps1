# Uses the production host adapter explicitly so its typed cells can be inspected without intercepting host output.
function Convert-TestNativeView {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object[]] $Rows,
        [ValidateSet('Table', 'List')][string] $Kind = 'Table',
        [hashtable] $Arguments = @{}
    )
    $capture = [ConsoleMonster.PowerShell.NativeFormatCapture]::new($PSCmdlet, $Kind, $Arguments)
    $parameters = $capture.Prepare($Rows[0])
    $Rows | ForEach-Object { $capture.BeginItem($_); $_ } |
        & ('Microsoft.PowerShell.Utility\Format-' + $Kind) @parameters |
        Out-ConsoleFormatCommand -Capture $capture -AsWidget
}
