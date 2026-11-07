Import-Module ConsoleMonster

Show-ConsoleApp -Content {
    New-ConsoleTabs -Key Page -Content {
        @{ Title = 'Overview'; Content = { 'SQL01 · Database · Warsaw'; 'Patched 2026-10-12' } }
        @{ Title = 'Disks'; Content = { 'C:  41% used'; 'D:  97% used' } }
        @{ Title = 'Services'; Content = { 'MSSQLSERVER  Running'; 'SQLAgent     Running' } }
    }
}