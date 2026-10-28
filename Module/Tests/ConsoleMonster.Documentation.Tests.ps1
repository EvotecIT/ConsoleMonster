Describe 'ConsoleMonster current command documentation' {
    It 'keeps retired command names out of current help, docs and runnable examples' {
        $repository = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
        $fixture = Get-Content (Join-Path $repository 'Tests/Api/round2-commands.json') -Raw | ConvertFrom-Json
        $paths = @(
            'Module/Docs', 'Module/en-US', 'Module/Examples', 'Website/examples',
            'Website/content/docs', 'ConsoleMonster.Markdown/README.md',
            'ConsoleMonster.PowerShell', 'ConsoleMonster/Widgets/InspectorTree.cs'
        )
        $files = foreach ($path in $paths) {
            Get-ChildItem -LiteralPath (Join-Path $repository $path) -Recurse -File |
                Where-Object { $_.Extension -in '.md', '.xml', '.ps1', '.cs' -and $_.FullName -notmatch '[/\\](bin|obj)[/\\]' }
        }
        $pattern = '(?<![\w-])(' + (($fixture.Retired | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')(?![\w-]|\.ps1\b)'
        $violations = foreach ($file in $files) {
            Select-String -LiteralPath $file.FullName -Pattern $pattern | ForEach-Object {
                '{0}:{1}: {2}' -f $file.FullName, $_.LineNumber, $_.Line.Trim()
            }
        }
        @($violations).Count | Should -Be 0 -Because ($violations -join "`n")
    }
}
