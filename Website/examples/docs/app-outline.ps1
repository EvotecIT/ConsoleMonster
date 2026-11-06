Import-Module ConsoleMonster

$sections = New-ConsoleOutline -Key Sections -SelectedIndex 0 -Items @(
    'Install'
    @{ Title = 'Windows'; Level = 2 }
    @{ Title = 'Linux'; Level = 2 }
    'Configure'
    'Run'
)
Show-ConsoleApp -Content {
    New-ConsoleBreadcrumb -Key Where -Ancestors 'Guide' -Outline $sections
    $sections
}