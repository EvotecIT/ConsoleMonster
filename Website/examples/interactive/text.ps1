Import-Module ConsoleMonster

$name = Read-ConsoleText 'Share name' -Default 'Reports' -Pattern '[A-Za-z][A-Za-z0-9_-]{1,23}' -PatternMessage 'Start with a letter; letters, digits, - and _ only' -Description 'Shown to users when they browse the server.'
Write-ConsoleMarkup "Creating share [bold]$name[/]"
