Import-Module ConsoleMonster

# What ConsoleMonster detected about this console, and why.
$capability = Get-ConsoleCapability
$capability | Format-ConsoleList -Title 'This console' -Property Mode, ColorDepth, Glyphs, Width, Height, ThemePreference, FontFace
$capability.Decisions | ForEach-Object { "  $_" }