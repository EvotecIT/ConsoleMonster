Import-Module ConsoleMonster

# Choose the theme for your terminal background, then opt in explicitly.
Set-ConsoleTheme -Theme Dark
Enable-ConsoleFormatting -Feature Prompt

# Inspect the profile lines; this does not edit your profile.
Enable-ConsoleFormatting -Feature Prompt -ProfileSnippet

# Restore the original prompt when finished.
Disable-ConsoleFormatting -Feature Prompt
