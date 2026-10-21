Import-Module ConsoleMonster
Import-Module ./Contoso.Servers

# Without views: PowerShell's own table
Get-ContosoServer

Enable-ConsoleFormatting

# The module's Contoso.Servers.consoleviews.psd1 picks the columns and the groups
Get-ContosoServer