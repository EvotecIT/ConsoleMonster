BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
}
Describe 'Native flow strip' {
    It 'builds pipeline nodes and Content edges through the item grammar' {
        $nodes = @(
            [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'client'; Status = 'Success' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'b'; Label = 'issuer'; Status = 'Failed' }
        )
        $flow = New-ConsoleFlow -WidgetId path -Content {
            $nodes
            [pscustomobject] @{ Kind = 'Edge'; Key = 'verify'; From = 'a'; To = 'b'; Label = 'signature'; Status = 'Fail'; Cut = $true }
            [pscustomobject] @{ Kind = 'Branch'; From = 'b'; Label = 'expired 2026-09-30'; Status = 'Fail' }
        }
        $text = $flow | Export-ConsoleFrame -Format Text -Width 48
        $text | Should -Match 'client'
        $text | Should -Match 'issuer'
        $text | Should -Match 'expired 2026-09-30'
        $flow.Nodes.Count | Should -Be 2
        $flow.Edges[0].Cut | Should -BeTrue
        $flow.WidgetId | Should -Be 'path'
    }
    It 'accepts records and composes inside a panel' {
        $flow = New-ConsoleFlow -Content {
            [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = '東京'; Status = 'Pass' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'b'; Label = 'root'; Status = 'Pending' }
            [pscustomobject] @{ Kind = 'Edge'; Key = 'trust'; From = 'a'; To = 'b'; Label = 'trust' }
        }
        $text = New-ConsolePanel -Title 'Certificate path' { $flow } | Export-ConsoleFrame -Format Text -Width 48
        $text | Should -Match '東京'
        $text | Should -Match 'root'
        $text | Should -Match 'Certificate path'
    }
    It 'renders a flow stored in a table cell' {
        $flow = New-ConsoleFlow -Content {
            [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'client'; Status = 'Pass' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'b'; Label = 'issuer'; Status = 'Fail' }
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
            [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'a' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'b'; Label = 'b' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'c'; Label = 'c' }
            [pscustomobject] @{ Kind = 'Edge'; Key = 'skip'; From = 'a'; To = 'c' }
        } } | Should -Throw
    }
    It 'validates node and edge updates before enqueueing and preserves explicit false' {
        $flow = New-ConsoleFlow -WidgetId path -Content {
            [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'client' }
            [pscustomobject] @{ Kind = 'Node'; Key = 'b'; Label = 'issuer' }
            [pscustomobject] @{ Kind = 'Edge'; Key = 'trust'; From = 'a'; To = 'b'; Cut = $true }
        }
        $type = (Get-Command Update-Console).Parameters['Session'].ParameterType
        $session = $type.GetConstructors()[0].Invoke(@($flow.PSObject.BaseObject, $null))
        try {
            { Update-Console path -Session $session -NodeKey missing -Status Fail } | Should -Throw '*Unknown flow NodeKey*'
            { Update-Console path -Session $session -NodeKey a -Status Fail -Cut } | Should -Throw
            $session.EventsPosted | Should -Be 0
            Update-Console path -Session $session -EdgeKey trust -Status Pass -Cut:$false
            $session.EventsPosted | Should -Be 1
            Update-Console path -Session $session -NodeKey b -Status Pass
            $session.EventsPosted | Should -Be 2
        } finally { $session.Dispose() }
        { [pscustomobject] @{ Kind = 'Node'; Key = 'a'; Label = 'client'; Detail = 'misspelled schema' } | New-ConsoleFlow } | Should -Throw '*Unknown item field*'
    }
    It 'exposes WidgetId without aliases for new live commands' {
        (Get-Command New-ConsoleFlow).Parameters.Keys | Should -Contain 'WidgetId'
        (Get-Command Update-Console).Parameters.Keys | Should -Contain 'WidgetId'
        (Get-Command New-ConsoleFlow).Parameters['WidgetId'].Aliases.Count | Should -Be 0
    }
    It 'turns plain strings into connected steps' {
        $flow = 'Build', 'Test', 'Deploy' | New-ConsoleFlow
        $flow.Nodes.Key | Should -Be @('Build', 'Test', 'Deploy')
        $flow.Nodes.Label | Should -Be @('Build', 'Test', 'Deploy')
        $text = $flow | Export-ConsoleFrame -Format Text -Width 60
        $text | Should -Match 'Build.*Test.*Deploy'
    }
    It 'infers nodes and edges and derives their keys when Kind and Key are left out' {
        $flow = New-ConsoleFlow -Content {
            [pscustomobject] @{ Label = 'Build'; Status = 'Pass' }
            [pscustomobject] @{ Key = 'test'; Label = 'Test' }
            [pscustomobject] @{ From = 'Build'; To = 'test'; Label = 'artifacts' }
        }
        $flow.Nodes.Key | Should -Be @('Build', 'test')
        $flow.Edges.Count | Should -Be 1
        $flow.Edges[0].Key | Should -BeExactly 'Build->test'
        $flow.Edges[0].Label | Should -BeExactly 'artifacts'
        { New-ConsoleFlow -Content { [pscustomobject] @{ From = 'a'; Label = 'side' } } } | Should -Throw '*Kind*'
    }
}
