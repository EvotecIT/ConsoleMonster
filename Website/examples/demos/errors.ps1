Import-Module ConsoleMonster

# Format-ConsoleError draws an error the way Enable-ConsoleFormatting -Feature ErrorView shows every error that reaches the console.
Get-Item (Join-Path ([IO.Path]::GetTempPath()) 'ConsoleMonster-demo-missing/Weekly-2026-43.pdf') -ErrorAction SilentlyContinue -ErrorVariable problem
$problem | Format-ConsoleError -NoStack
