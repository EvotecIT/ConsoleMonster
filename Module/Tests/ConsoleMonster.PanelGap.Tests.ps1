Describe 'Panel content spacing' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'spaces several content children with -Gap and keeps them dense by default' {
        $dense = New-ConsolePanel -Title Report -Content { New-ConsoleText 'one'; New-ConsoleText 'two' } | Export-ConsoleFrame -Format Text -Width 30
        $spaced = New-ConsolePanel -Title Report -Gap 1 -Content { New-ConsoleText 'one'; New-ConsoleText 'two' } | Export-ConsoleFrame -Format Text -Width 30
        @($spaced -split "`n").Count | Should -Be (@($dense -split "`n").Count + 1)
        $panel = New-ConsolePanel -Gap 2 -Content { New-ConsoleText 'one'; New-ConsoleText 'two' }
        $panel.Gap | Should -Be 2
        $panel.Content.Gap | Should -Be 2
        (New-ConsolePanel -Gap 1 -Content { New-ConsoleText 'only' }).Content.GetType().Name | Should -Not -Be 'Rows'
        { New-ConsolePanel -Gap -1 -Content { New-ConsoleText 'x' } } | Should -Throw
    }
}
