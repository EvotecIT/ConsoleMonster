Import-Module ConsoleMonster

Write-ConsoleText -Markup '[bold red]Markup[/] in the [italic]Spectre.Console[/] dialect: [cadetblue]cadetblue[/], [grey50]grey50[/], [deepskyblue3]deepskyblue3[/]'
Write-ConsoleText -Markup '[#ff8800]#ff8800[/], [rgb(120,200,90)]rgb(120,200,90)[/], [color(208)]color(208)[/] and [white on fail] theme roles [/]'
Write-ConsoleText -Markup '[[brackets]] are literal, [[nosuchcolour]] is an escaped literal tag, and [link=https://consolemonster.dev]links[/] use OSC 8'
