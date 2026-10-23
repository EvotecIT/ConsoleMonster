Import-Module ConsoleMonster

# One poll returns the last 16 completed lines. Omit -Count to keep following.
Watch-ConsoleLog -Path (Join-Path $PSScriptRoot 'synthetic.log') -Tail 16 -Highlight database -Count 1
