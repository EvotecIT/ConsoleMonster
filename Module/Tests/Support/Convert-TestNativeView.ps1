# Uses the production host adapter explicitly so its typed cells can be inspected without intercepting host output.
function Convert-TestNativeView {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object[]] $Rows,
        [ValidateSet('Table', 'List')][string] $Kind = 'Table',
        [hashtable] $Arguments = @{}
    )
    $capture = [Activator]::CreateInstance((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.NativeFormatCapture'), [object[]]@($PSCmdlet, $Kind, $Arguments))
    $parameters = $capture.Prepare($Rows[0])
    $Rows | ForEach-Object { $capture.BeginItem($_); $_ } |
        & ('Microsoft.PowerShell.Utility\Format-' + $Kind) @parameters |
        & ([Management.Automation.CmdletInfo]::new('Out-ConsoleFormatCommand', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleFormatCommand'))) -Capture $capture -AsWidget
}
