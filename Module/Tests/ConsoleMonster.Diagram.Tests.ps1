Describe 'Show-ConsoleDiagram' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force -ErrorAction Stop
        $script:DiagramRoot = Join-Path $TestDrive 'diagrams'
        New-Item -ItemType Directory -Path $script:DiagramRoot -Force | Out-Null
        function New-DiagramFile([string] $Name, [string[]] $Lines) {
            $path = Join-Path $script:DiagramRoot $Name
            Set-Content -LiteralPath $path -Encoding UTF8 -Value $Lines
            $path
        }

        $script:PlainPath = New-DiagramFile 'plain.cfx' @(
            'server web "Web" subtitle:"Front end"'
            'database sql "SQL" status:warning'
            'web -> sql "query"'
        )
        $script:StoryPath = New-DiagramFile 'story.md' @(
            '# Ordering'
            ''
            '```chartforgex architecture v1 {#ordering title="Ordering"}'
            'user shopper "Shopper"'
            'service api "Orders API"'
            'database db "Orders DB"'
            'shopper -> api "places order"'
            'api -> db "insert"'
            ''
            'usecase checkout "Checkout"'
            '  shopper -> api "POST /orders" json { "sku": "A-100", "quantity": 2 }'
            '  api -> db "INSERT order"'
            '  api --> shopper "201 Created"'
            'end'
            '```'
        )
        $script:TwoDiagramsPath = New-DiagramFile 'two.md' @(
            '```chartforgex architecture v1 {#first}'
            'server a "A"'
            '```'
            ''
            '```chartforgex architecture v1 {#second}'
            'server b "B"'
            '```'
        )
        $script:UnnamedDiagramsPath = New-DiagramFile 'unnamed.md' @(
            '```chartforgex architecture v1'
            'server a "Alpha"'
            '```'
            ''
            '```chartforgex architecture v1'
            'server b "Beta"'
            '```'
        )
        $script:DuplicateDiagramsPath = New-DiagramFile 'duplicate.md' @(
            '```chartforgex architecture v1 {#repeated}'
            'server a "Alpha"'
            '```'
            ''
            '```chartforgex architecture v1 {#repeated}'
            'server b "Beta"'
            '```'
        )
        $script:MixedPath = New-DiagramFile 'mixed.md' @(
            '```chartforgex architecture v1 {#good}'
            'server a "Alpha"'
            '```'
            ''
            '```chartforgex architecture v1 {#bad}'
            'server b "Beta" status:sideways'
            '```'
        )
        $script:ProsePath = New-DiagramFile 'prose.md' @('# Notes', '', 'No diagrams here.')
        $script:WarningPath = New-DiagramFile 'warning.cfx' @(
            'server a "A"'
            'a -> b "calls"'
        )
    }

    It 'reads an architecture file without fences and prints the diagram' {
        $text = Show-ConsoleDiagram -Path $script:PlainPath -Width 80 -AsString
        $text | Should -Match 'Web'
        $text | Should -Match 'Front end'
        $text | Should -Match 'SQL'
        $text | Should -Match 'query'
    }

    It 'shows the chosen step of a use case with its payload' {
        $text = Show-ConsoleDiagram -Path $script:StoryPath -Scenario checkout -Step 1 -Width 100 -AsString
        $text | Should -Match 'Checkout'
        $text | Should -Match 'step 1 of 3'
        $text | Should -Match 'POST /orders'
        $text | Should -Match '"sku"'
    }

    It 'returns the diagram widget with the selected use case and step' {
        $widget = Show-ConsoleDiagram -Path $script:StoryPath -Step 3 -AsWidget
        $widget.GetType().FullName | Should -Be 'ConsoleMonster.Widgets.Diagram'
        $widget.ScenarioId | Should -Be 'checkout'
        $widget.StepIndex | Should -Be 2
    }

    It 'asks for -Diagram when a file has several diagrams' {
        { Show-ConsoleDiagram -Path $script:TwoDiagramsPath -AsString } | Should -Throw -ErrorId 'DiagramAmbiguous,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
        Show-ConsoleDiagram -Path $script:TwoDiagramsPath -Diagram second -Width 60 -AsString | Should -CMatch '\bB\b'
    }

    It 'selects each unnamed architecture fence by its file-wide generated id' {
        $first = Show-ConsoleDiagram -Path $script:UnnamedDiagramsPath -Diagram architecture-1 -Width 60 -AsString
        $first | Should -Match 'Alpha'
        $first | Should -Not -Match 'Beta'
        $second = Show-ConsoleDiagram -Path $script:UnnamedDiagramsPath -Diagram architecture-2 -Width 60 -AsString
        $second | Should -Match 'Beta'
        $second | Should -Not -Match 'Alpha'
    }

    It 'rejects a selected explicit id shared by two diagrams as ambiguous' {
        { Show-ConsoleDiagram -Path $script:DuplicateDiagramsPath -Diagram repeated -AsString } |
            Should -Throw -ErrorId 'DiagramAmbiguous,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
    }

    It 'rejects -AsWidget combined with -<ConflictingSwitch>' -ForEach @(
        @{ ConflictingSwitch = 'AsString' }
        @{ ConflictingSwitch = 'StaticSnapshot' }
    ) {
        $parameters = @{ AsWidget = $true }
        $parameters[$ConflictingSwitch] = $true
        { Show-ConsoleDiagram -Path $script:PlainPath @parameters } |
            Should -Throw -ErrorId 'DiagramOutputModeInvalid,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram' -ExpectedMessage '*-AsWidget cannot be combined with -AsString or -StaticSnapshot.*'
    }

    It 'shows the chosen diagram when another fence has errors, and warns about the other fence' {
        $warnings = $null
        $text = Show-ConsoleDiagram -Path $script:MixedPath -Diagram good -Width 60 -AsString -WarningVariable warnings -WarningAction SilentlyContinue
        $text | Should -Match 'Alpha'
        (@($warnings) -join "`n") | Should -Match 'mixed\.md:\d+(:\d+)? CFXA015 '
        { Show-ConsoleDiagram -Path $script:MixedPath -Diagram bad -AsString } | Should -Throw -ErrorId 'DiagramMarkupInvalid,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
    }

    It 'names the known use cases and the valid step range' {
        { Show-ConsoleDiagram -Path $script:StoryPath -Scenario missing -AsString } | Should -Throw -ExpectedMessage '*Use cases: checkout.*'
        { Show-ConsoleDiagram -Path $script:StoryPath -Step 9 -AsString } | Should -Throw -ExpectedMessage '*from 1 to 3*'
        { Show-ConsoleDiagram -Path $script:PlainPath -Step 1 -AsString } | Should -Throw -ErrorId 'DiagramStepUnavailable,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
    }

    It 'writes markup warnings with the file line and diagnostic code' {
        $warnings = $null
        $text = Show-ConsoleDiagram -Path $script:WarningPath -Width 80 -AsString -WarningVariable warnings -WarningAction SilentlyContinue
        $text | Should -CMatch '\bA\b'
        @($warnings).Count | Should -BeGreaterThan 0
        @($warnings)[0].Message | Should -Match '^warning\.cfx:\d+(:\d+)? CFXA\d{3} '
    }

    It 'does not read Markdown without visual fences as an architecture file' {
        { Show-ConsoleDiagram -Path $script:ProsePath -AsString } | Should -Throw -ErrorId 'DiagramNotFound,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
    }

    It 'rejects a folder and a missing file' {
        { Show-ConsoleDiagram -Path $script:DiagramRoot -AsString } | Should -Throw -ErrorId 'DiagramPathIsFolder,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
        { Show-ConsoleDiagram -Path (Join-Path $script:DiagramRoot 'missing.cfx') -AsString } | Should -Throw -ErrorId 'DiagramFileNotFound,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
    }
}

Describe 'Diagram construction and live updates' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force -ErrorAction Stop
    }

    It 'collects nested groups and ordered use-case payloads with label identities and implicit endpoints' {
        $diagram = New-ConsoleDiagram -Title Ordering -WidgetId orders -Content {
            New-ConsoleDiagramGroup Office -Content {
                New-ConsoleDiagramGroup Services -Boundary:$false -Content {
                    New-ConsoleDiagramNode 'Order API' -Id api -Kind Server -Subtitle 'api.contoso.test' -Value '12 ms' -Status Warn -To Queue
                }
            }
            New-ConsoleDiagramLink api Database -Label insert -Status Fail -Style Dashed -Both
            New-ConsoleDiagramUseCase Checkout -Delay 1200 -Loop -Content {
                New-ConsoleDiagramStep Shopper api 'POST /orders' -Payload @{ sku = 'A-100'; quantity = 2; confirmed = $false; metadata = @{ region = 'EU' } }
                New-ConsoleDiagramStep api Shopper Created -Reply
                New-ConsoleDiagramStep api Queue Rejected -Failed
            }
        }
        $diagram.GetType().FullName | Should -Be 'ConsoleMonster.Widgets.Diagram'
        $diagram.WidgetId | Should -Be orders
        $diagram.ShowSummary | Should -BeTrue
        $diagram.Groups.Count | Should -Be 1
        $nested = $diagram.Groups[0].Groups[0]
        $nested.Label | Should -Be Services
        $nested.Boundary | Should -BeFalse
        $node = $nested.Nodes[0]
        $node.Id | Should -Be api
        $node.Label | Should -Be 'Order API'
        $node.Kind.ToString() | Should -Be Server
        $node.Status.ToString() | Should -Be Warn
        $node.Status.GetType().FullName | Should -Be 'ConsoleMonster.Styling.StatusKind'
        @($node.To) | Should -Be @('Queue')
        $diagram.Links[0].Both | Should -BeTrue
        $diagram.Links[0].Style.ToString() | Should -Be Dashed
        $diagram.Links[0].Status.ToString() | Should -Be Fail
        @($diagram.Nodes.Id | Sort-Object) | Should -Be @('Database', 'Queue', 'Shopper')
        $diagram.Nodes | ForEach-Object { $_.Id | Should -Be $_.Label }
        $useCase = $diagram.UseCases[0]
        $useCase.Id | Should -Be Checkout
        $useCase.Delay | Should -Be 1200
        $useCase.Loop | Should -BeTrue
        @($useCase.Steps.Kind | ForEach-Object ToString) | Should -Be @('Request', 'Reply', 'Failed')
        $useCase.Steps[0].Payload['quantity'] | Should -BeOfType ([int])
        $useCase.Steps[0].Payload['quantity'] | Should -Be 2
        $useCase.Steps[0].Payload['confirmed'] | Should -BeFalse
        $useCase.Steps[0].Payload['metadata']['region'] | Should -Be EU
        $text = $diagram | Export-ConsoleFrame -Format Text -Width 100
        $text | Should -Match 'Needs attention'
        $text | Should -Match 'api\.contoso\.test'
        $text | Should -Match '12 ms'
        $selected = $diagram | Show-ConsoleDiagram -Scenario Checkout -Step 1 -AsWidget
        $selected.StepIndex | Should -Be 0
        $selectedText = $selected | Export-ConsoleFrame -Format Text -Width 100
        $selectedText | Should -Match 'POST /orders'
        $selectedText | Should -Match 'A-100'
        $hidden = $diagram | Show-ConsoleDiagram -NoSummary -AsWidget
        $hidden.ShowSummary | Should -BeFalse
        ($hidden | Show-ConsoleDiagram -AsWidget).ShowSummary | Should -BeFalse
        ($hidden | Show-ConsoleDiagram -NoSummary:$false -AsWidget).ShowSummary | Should -BeTrue
    }

    It 'maps <Shape> connection rows through all five selectors' -ForEach @(
        @{ Shape = 'typed property' }
        @{ Shape = 'hashtable default' }
        @{ Shape = 'scriptblock' }
    ) {
        $selectors = @{}
        switch ($Shape) {
            'typed property' {
                $rows = @([Tuple[string, string, string, string, string]]::new('Files', 'Backup', 'nightly', 'Warn', 'Office'))
                $selectors = @{ From = 'Item1'; To = 'Item2'; Label = 'Item3'; Status = 'Item4'; Group = 'Item5' }
            }
            'hashtable default' {
                $rows = @(@{ from = 'Files'; TO = 'Backup'; Label = 'nightly'; Status = 'Warn'; Group = 'Office' })
            }
            'scriptblock' {
                $rows = @([pscustomobject] @{ Origin = 'files'; Destination = 'Backup'; Job = 'nightly'; Health = 'Warn'; Site = 'Office' })
                $selectors = @{
                    From = { $_.Origin.Substring(0, 1).ToUpperInvariant() + $_.Origin.Substring(1) }
                    To = { $PSItem.Destination }
                    Label = { $_.Job }
                    Status = { $_.Health }
                    Group = { $_.Site }
                }
            }
        }
        $diagram = $rows | New-ConsoleDiagram @selectors -Title Replication -NoSummary
        $diagram.Title | Should -Be Replication
        $diagram.ShowSummary | Should -BeFalse
        $diagram.Links.Count | Should -Be 1
        $diagram.Links[0].From | Should -Be Files
        $diagram.Links[0].To | Should -Be Backup
        $diagram.Links[0].Label | Should -Be nightly
        $diagram.Links[0].Status.ToString() | Should -Be Warn
        $diagram.Groups[0].Label | Should -Be Office
        $diagram.Groups[0].Nodes[0].Id | Should -Be Files
        $diagram.Groups[0].Nodes[0].Label | Should -Be Files
        $diagram.Nodes[0].Id | Should -Be Backup
        $shown = $rows | Show-ConsoleDiagram @selectors -Title Replication -NoSummary -AsWidget
        ($shown | Export-ConsoleFrame -Format Text -Width 80) |
            Should -Be ($diagram | Export-ConsoleFrame -Format Text -Width 80)
    }

    It 'rejects a multi-value selector instead of silently discarding connections' {
        $row = [pscustomobject] @{ From = 'Source'; To = 'unused' }
        { $row | New-ConsoleDiagram -To { 'A'; 'B' } -ErrorAction Stop } |
            Should -Throw '*one value per row*'
    }

    It 'applies addressed values and statuses through a running session and stops accepting updates after stop' {
        $diagram = New-ConsoleDiagram -WidgetId ping -Content {
            New-ConsoleDiagramNode VPN -Id vpn -Subtitle 'vpn.contoso.test'
            New-ConsoleDiagramNode Router -Value unchanged
        }
        $core = [ConsoleMonster.Terminal].Assembly
        $target = ($core.GetType('ConsoleMonster.Testing.TestTerminal', $true))::Target(80, 24)
        $target.SkipDelays = $true
        $options = ($core.GetType('ConsoleMonster.Live.LiveSessionOptions', $true))::new()
        $options.Target = $target
        $options.CaptureConsole = $false
        $options.HandleCancelKey = $false
        $options.PrintFinalState = $false
        $sessionType = (Get-Command Update-Console).Parameters['Session'].ParameterType
        $session = $sessionType::new($diagram, $options)
        try {
            $session.Start()
            Update-Console -WidgetId ping -Session $session -Key vpn -Value '180 ms' -Status Warn
            Wait-ConsoleDashboard -Session $session -Seconds 0
            $diagram.Nodes[0].Value | Should -Be '180 ms'
            $diagram.Nodes[0].Status.ToString() | Should -Be Warn
            $diagram.Nodes[0].Subtitle | Should -Be 'vpn.contoso.test'
            $diagram.Nodes[1].Value | Should -Be unchanged
            $before = $session.EventsPosted
            { Update-Console -WidgetId ping -Session $session -Key missing -Value timeout -Status Fail } | Should -Throw '*Unknown diagram*'
            $session.EventsPosted | Should -Be $before
            Update-Console -WidgetId ping -Session $session -Key vpn -Status Fail
            Wait-ConsoleDashboard -Session $session -Seconds 0
            $diagram.Nodes[0].Value | Should -Be '180 ms'
            $diagram.Nodes[0].Status.ToString() | Should -Be Fail
            Update-Console -WidgetId ping -Session $session -Key vpn -Value $null -Status Pass
            Wait-ConsoleDashboard -Session $session -Seconds 0
            $diagram.Nodes[0].Value | Should -BeNullOrEmpty
            $diagram.Nodes[0].Status.ToString() | Should -Be Pass
            $session.Stop()
            $before = $session.EventsPosted
            Update-Console -WidgetId ping -Session $session -Key vpn -Value late -Status Fail
            $session.EventsPosted | Should -Be $before
            $diagram.Nodes[0].Value | Should -BeNullOrEmpty
            $diagram.Nodes[0].Status.ToString() | Should -Be Pass
        } finally { $session.Dispose() }
    }
}
