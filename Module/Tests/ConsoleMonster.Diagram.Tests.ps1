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
        $widget.GetType().FullName | Should -Be 'ConsoleMonster.Graphics.TopologyDiagram'
        $widget.ScenarioId | Should -Be 'checkout'
        $widget.StepIndex | Should -Be 2
    }

    It 'asks for -Diagram when a file has several diagrams' {
        { Show-ConsoleDiagram -Path $script:TwoDiagramsPath -AsString } | Should -Throw -ErrorId 'DiagramAmbiguous,ConsoleMonster.PowerShell.CmdletShowConsoleDiagram'
        Show-ConsoleDiagram -Path $script:TwoDiagramsPath -Diagram second -Width 60 -AsString | Should -CMatch '\bB\b'
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
