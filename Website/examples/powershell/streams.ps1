Import-Module ConsoleMonster

# Show every stream for this demo.
$VerbosePreference = 'Continue'
$DebugPreference = 'Continue'

Write-ConsoleRule "PowerShell's own colours"
Write-Warning 'Certificate for mail.evotec.xyz expires in 12 days'
Write-Verbose 'Queried 4 domain controllers in 2 domains'
Write-Debug 'Cache hit for evotec.xyz'

Enable-ConsoleFormatting -Feature Streams

Write-ConsoleRule 'With -Feature Streams: the theme''s colours'
Write-Warning 'Certificate for mail.evotec.xyz expires in 12 days'
Write-Verbose 'Queried 4 domain controllers in 2 domains'
Write-Debug 'Cache hit for evotec.xyz'

Disable-ConsoleFormatting -Feature Streams