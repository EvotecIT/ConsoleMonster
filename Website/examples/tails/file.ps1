Import-Module ConsoleMonster

# One poll returns the last 16 completed lines. Omit -Count to keep following.
$logParameters = @{
    Path = (Join-Path $PSScriptRoot 'synthetic.log')
    Tail = 16
    Highlight = 'database'
    Count = 1
}
Watch-ConsoleLog @logParameters
