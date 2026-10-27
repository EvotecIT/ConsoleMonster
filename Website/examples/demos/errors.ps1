Import-Module ConsoleMonster

# Show-ConsoleError draws an error the way Enable-ConsoleFormatting -Feature ErrorView shows every error that reaches the console.
$itemParameters = @{
    ErrorAction = 'SilentlyContinue'
    ErrorVariable = 'problem'
}
Get-Item (Join-Path ([IO.Path]::GetTempPath()) 'ConsoleMonster-demo-missing/Weekly-2026-43.pdf') @itemParameters
$problem | Show-Console -NoStack -View Error
