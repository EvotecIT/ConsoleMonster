param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 0)][int]$Step = 0
)

Import-Module ConsoleMonster

# The same synthetic objects feed the plain baseline and the formatted table.
$tasks = @(
    [pscustomobject]@{ Task = 'Draft release notes'; State = 'Ready'; Minutes = 12 }
    [pscustomobject]@{ Task = 'Review examples'; State = 'In progress'; Minutes = 25 }
    [pscustomobject]@{ Task = 'Archive old drafts'; State = 'Waiting'; Minutes = 8 }
)
$baseline = ($tasks | ForEach-Object { "$($_.Task) | $($_.State) | $($_.Minutes) min" }) -join "`n"

New-ConsoleRows -Gap 1 {
    New-ConsoleRule '[bold]Everyday formatting[/] [muted]| same objects, clearer output[/]'
    New-ConsoleText -Markup '[bold]Before[/] [muted]| plain text[/]'
    New-ConsoleText $baseline
    New-ConsoleText -Markup '[bold]After[/] [muted]| ConsoleMonster table[/]'
    $tasks | New-ConsoleTable -Property Task, State, Minutes -Expand
    New-ConsoleText -Markup '[muted]Three sample tasks. Aligned columns and right-aligned numbers.[/]'
} | Write-ConsoleWidget
