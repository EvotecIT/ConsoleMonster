Import-Module ConsoleMonster

# Choose the theme for your terminal background, then opt in explicitly.
Set-ConsoleTheme -Theme Dark
Enable-ConsoleFormatting -Include Prompt

# Inspect the profile lines; this does not edit your profile.
Enable-ConsoleFormatting -Include Prompt -ProfileSnippet

# Restore the original prompt when finished.
Disable-ConsoleFormatting -Feature Prompt
