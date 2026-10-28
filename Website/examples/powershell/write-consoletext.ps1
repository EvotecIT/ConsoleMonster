Import-Module ConsoleMonster

Write-ConsoleText -Text 'Deploying ', 'FS01', ' to ', 'Production' -Color Gray, Cyan, Gray, Yellow
Write-ConsoleText -Markup '[pass]√[/] copied 1,204 files' -StartTab 1
Write-ConsoleText -Text 'Status: ', 'healthy' -Color DarkGray, Green -StartSpaces 8
$textParameters = @{
    Text = 'Collecting inventory '
    Color = 'Gray'
    PadRight = 40
    PadCharacter = '.'
    NoNewLine = $true
}
Write-ConsoleText @textParameters
Write-ConsoleText -Text ' done' -Color Green
Write-ConsoleText -Text 'Release 2026.10 is live' -Gradient '#38bdf8', '#c084fc' -HorizontalCenter
