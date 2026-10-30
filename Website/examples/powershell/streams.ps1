Import-Module ConsoleMonster

# Show every stream for this demo.
$VerbosePreference = 'Continue'
$DebugPreference = 'Continue'

New-ConsoleRule -Title "PowerShell's own colours" | Show-Console
Write-Warning 'Certificate for mail.evotec.xyz expires in 12 days'
Write-Verbose 'Queried 4 domain controllers in 2 domains'
Write-Debug 'Cache hit for evotec.xyz'

Enable-ConsoleFormatting -Include Streams

New-ConsoleRule -Title 'With -Include Streams: the theme''s colours' | Show-Console
Write-Warning 'Certificate for mail.evotec.xyz expires in 12 days'
Write-Verbose 'Queried 4 domain controllers in 2 domains'
Write-Debug 'Cache hit for evotec.xyz'

Disable-ConsoleFormatting -Feature Streams
