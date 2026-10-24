# Print an offline welcome screen with a logo, host summary, git status and tip.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
# All host and git values are fictional; this runs offline without local probes.
# For $PROFILE, replace fixed strings with cached local data, not slow commands.
$hostSummary = [ordered]@{
    Host = 'comet-devbox'
    Platform = 'Cross-platform development'
    Runtime = 'PowerShell 7.4+ / .NET 8+'
    Workspace = 'comet-notes'
}
$git = '[accent]main[/]   [warn]2 modified[/]   [pass]1 staged[/]   [muted]0 ahead / 0 behind[/]'

# Layout: compose the screen from the data above.
$screen = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleLogo -Text 'HELLO' -Tagline '[muted]A little context before your next command.[/]'
    New-ConsolePanel -Title 'Host summary' -HeaderRight 'local workspace' -Content {
        New-ConsoleKeyValue -InputObject $hostSummary
    }
    New-ConsolePanel -Title 'Git | comet-notes' -Content {
        New-ConsoleText -Markup $git
    }
    New-ConsoleText -Markup '[bold]Tip[/] Get-Help New-ConsoleTable -Examples'
    New-ConsoleText -Markup '[muted]Ready when you are.[/]'
}

# Behaviour: print the screen once and return.
$screen | Show-Console
