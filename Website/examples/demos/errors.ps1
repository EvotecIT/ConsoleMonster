# Format-ConsoleError draws an error the way Enable-ConsoleFormatting -Feature ErrorView shows every error that reaches the console.
Get-Item C:\Reports\Weekly-2026-43.pdf -ErrorAction SilentlyContinue -ErrorVariable problem
$problem | Format-ConsoleError -NoStack
