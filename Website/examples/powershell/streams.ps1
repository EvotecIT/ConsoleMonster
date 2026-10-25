Import-Module ConsoleMonster

# Show every stream for this demo.
$VerbosePreference = 'Continue'
$DebugPreference = 'Continue'

Show-Console -Title "PowerShell's own colours" -View Rule
Write-Warning 'Certificate for mail.evotec.xyz expires in 12 days'
Write-Verbose 'Queried 4 domain controllers in 2 domains'
Write-Debug 'Cache hit for evotec.xyz'

Enable-ConsoleFormatting -Feature Streams

Show-Console -Title 'With -Feature Streams: the theme''s colours' -View Rule
Write-Warning 'Certificate for mail.evotec.xyz expires in 12 days'
Write-Verbose 'Queried 4 domain controllers in 2 domains'
Write-Debug 'Cache hit for evotec.xyz'

Disable-ConsoleFormatting -Feature Streams