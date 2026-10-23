Describe 'Everyday formatting features together' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:CombinedRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:CombinedModule = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
    }

    It 'keeps the surviving adapters and restores every opt-in on disable and module removal' {
        $source = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__' -Force
function global:prompt { 'original> ' }
$beforeHelp = ${function:global:help}.ToString()
$beforePrompt = ${function:global:prompt}.ToString()
$features = 'Output', 'FormatCommands', 'Help', 'Errors', 'Streams', 'ReadLine', 'GridView', 'Prompt'
foreach ($finish in 'Disable', 'Remove') {
    Enable-ConsoleFormatting -Feature $features -Force
    if ((Get-Command Format-Table).CommandType -ne 'Function') { throw 'Format proxy was not installed' }
    if (${function:global:help}.ToString() -ceq $beforeHelp) { throw 'Help proxy was not installed' }
    Disable-ConsoleFormatting -Feature Help
    if (${function:global:help}.ToString() -cne $beforeHelp) { throw 'Help was not restored' }
    if (-not ${function:global:Out-Default}) { throw 'Other output features were removed with Help' }
    Disable-ConsoleFormatting -Feature FormatCommands
    if ((Get-Command Format-Table).CommandType -ne 'Cmdlet') { throw 'Native Format-Table was not restored' }
    if (-not ${function:global:Out-Default}) { throw 'Errors and Output lost their adapter' }
    if ($finish -eq 'Remove') { Remove-Module ConsoleMonster } else { Disable-ConsoleFormatting }
    if (${function:global:Out-Default}) { throw 'Output adapter was left installed' }
    if (${function:global:help}.ToString() -cne $beforeHelp) { throw 'Help changed during cleanup' }
    if (${function:global:prompt}.ToString() -cne $beforePrompt) { throw 'Prompt was not restored' }
}
'COEXISTENCE_OK'
'@
        $source = $source.Replace('__MODULE__', $script:CombinedModule.Replace("'", "''"))
        $result = @(Invoke-TestChild -Runtime $script:CombinedRuntime -Script $source)
        $LASTEXITCODE | Should -Be 0 -Because ($result -join "`n")
        $result -join "`n" | Should -Match 'COEXISTENCE_OK'
    }
}
