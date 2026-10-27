Import-Module ConsoleMonster

$textParameters = @{
    Default = 'Reports'
    Pattern = '[A-Za-z][A-Za-z0-9_-]{1,23}'
    PatternMessage = 'Start with a letter; letters, digits, - and _ only'
    Description = 'Shown to users when they browse the server.'
}
$name = Read-ConsoleText 'Share name' @textParameters
Write-ConsoleText -Markup "Creating share [bold]$name[/]"
