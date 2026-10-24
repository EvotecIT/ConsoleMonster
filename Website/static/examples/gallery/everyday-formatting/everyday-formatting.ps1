# Show the same task data as plain text and a formatted table.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
# The same synthetic objects feed the plain baseline and the formatted table.
$tasks = @(
    [pscustomobject]@{ Task = 'Draft release notes'; State = 'Ready'; Minutes = 12 }
    [pscustomobject]@{ Task = 'Review examples'; State = 'In progress'; Minutes = 25 }
    [pscustomobject]@{ Task = 'Archive old drafts'; State = 'Waiting'; Minutes = 8 }
)
$baseline = ($tasks | ForEach-Object -Process { "$($_.Task) | $($_.State) | $($_.Minutes) min" }) -join "`n"

# Layout: compose the screen from the data above.
$screen = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Everyday formatting[/] [muted]| same objects, clearer output[/]'
    New-ConsoleText -Markup '[bold]Before[/] [muted]| plain text[/]'
    New-ConsoleText -Text $baseline
    New-ConsoleText -Markup '[bold]After[/] [muted]| ConsoleMonster table[/]'
    $tasks | New-ConsoleTable -Property Task, State, Minutes -Expand
    New-ConsoleText -Markup '[muted]Three tasks. Aligned columns and right-aligned numbers.[/]'
}

# Behaviour: print the screen once and return.
$screen | Write-ConsoleWidget
