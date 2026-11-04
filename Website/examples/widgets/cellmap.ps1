Import-Module ConsoleMonster

# One cell per host and port, filled row by row, with a legend of counts.
$states = @(
    'Pass', 'Pass', 'Skip', 'Skip'
    'Pass', 'Warn', 'Skip', 'Skip'
    'Pass', 'Pass', 'Fail', 'Pass'
    'Skip', 'Skip', 'Pass', 'Pass'
)
New-ConsoleCellMap -InputObject $states -Columns 4 -LegendLines 2 `
    -RowLabels 'web-01', 'web-02', 'api-01', 'db-01' | Show-Console
