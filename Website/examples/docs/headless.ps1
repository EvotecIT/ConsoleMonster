Import-Module ConsoleMonster

# Run an app on a headless terminal, drive it with a key and a click, and keep what it showed.
$result = Invoke-ConsoleHeadless -Width 56 -Height 10 -InputScript 'Down click:4,5' {
    Show-ConsoleApp -Title 'Restart a server' -Content {
        New-ConsoleSelect -Key Server -Choices WEB01, SQL01, FS01
        New-ConsoleBinding -Content { param($State) New-ConsoleText -Text "Picked $($State.Server)" }
    }
}

# Print the kept screen; a test compares it instead: $result.Text | Should -Match 'Picked FS01'
New-ConsolePanel -Title 'headless 56×10' -Content { New-ConsoleText -Text $result.Text } |
    Show-Console