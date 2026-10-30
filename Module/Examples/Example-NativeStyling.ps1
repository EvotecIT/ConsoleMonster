Import-Module ConsoleMonster

# PSReadLine is normally already loaded in an interactive PowerShell session.
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Include ReadLine, Streams

# Type commands and select text in the normal editor. Prediction behavior stays as configured.
Write-Warning 'Disk space is low'
Write-Progress -Id 1 -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42
Write-Progress -Id 1 -Activity 'Backup' -Completed

# Print the profile lines; the profile is never edited.
Enable-ConsoleFormatting -Include ReadLine, Streams -ProfileSnippet

# Restore the original colors, including owned values edited while enabled.
Disable-ConsoleFormatting -Feature ReadLine, Streams
