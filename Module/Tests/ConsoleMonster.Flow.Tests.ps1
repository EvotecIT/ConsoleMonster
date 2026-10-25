BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
}
Describe 'Native flow strip' {
    It 'builds pipeline rows and grows through the item grammar' {
        $nodes = @(
            [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'client'; Status = 'Success' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'b'; Label = 'issuer'; Status = 'Failed' }
        )
        $flow = $nodes | New-ConsoleFlow -WidgetId path
        $flow | Add-ConsoleItem {
            New-ConsoleFlowEdge -Key verify -From a -To b -Label 'signature' -Status Fail -Cut
            New-ConsoleFlowBranch -From b -Label 'expired 2026-09-30' -Status Fail
        }
        $text = $flow | Export-ConsoleFrame -Format Text -Width 48
        $text | Should -Match 'client'
        $text | Should -Match 'issuer'
        $text | Should -Match 'expired 2026-09-30'
        $flow.Nodes.Count | Should -Be 2
        $flow.Edges[0].Cut | Should -BeTrue
        $flow.WidgetId | Should -Be 'path'
    }
    It 'accepts item commands and composes inside a panel' {
        $flow = New-ConsoleFlow -Content {
            New-ConsoleFlowNode -Key a -Label '東京' -Status Pass
            New-ConsoleFlowNode -Key b -Label 'root' -Status Pending
            New-ConsoleFlowEdge -Key trust -From a -To b -Label 'trust'
        }
        $text = New-ConsolePanel -Title 'Certificate path' { $flow } | Export-ConsoleFrame -Format Text -Width 48
        $text | Should -Match '東京'
        $text | Should -Match 'root'
        $text | Should -Match 'Certificate path'
    }
    It 'renders a flow stored in a table cell' {
        $flow = New-ConsoleFlow -Content {
            New-ConsoleFlowNode -Key a -Label 'client' -Status Pass
            New-ConsoleFlowNode -Key b -Label 'issuer' -Status Fail
        }
        $rows = @([pscustomobject] @{ Name = 'Certificate'; Path = $flow })
        $text = $rows | New-ConsoleTable -Property Name, Path | Export-ConsoleFrame -Format Text -Width 48
        $text | Should -Match 'client'
        $text | Should -Match 'issuer'
        $text | Should -Not -Match 'ConsoleMonster.Widgets.Flow'
    }
    It 'rejects unknown statuses and nonadjacent edges' {
        { [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'a'; Status = 'not-a-status' } | New-ConsoleFlow } | Should -Throw
        { New-ConsoleFlow -Content {
            New-ConsoleFlowNode -Key a -Label a
            New-ConsoleFlowNode -Key b -Label b
            New-ConsoleFlowNode -Key c -Label c
            New-ConsoleFlowEdge -Key skip -From a -To c
        } } | Should -Throw
    }
    It 'exposes WidgetId without aliases for new live commands' {
        (Get-Command New-ConsoleFlow).Parameters.Keys | Should -Contain 'WidgetId'
        (Get-Command Update-ConsoleFlow).Parameters.Keys | Should -Contain 'WidgetId'
        (Get-Command New-ConsoleFlow).Parameters['WidgetId'].Aliases.Count | Should -Be 0
    }
}
