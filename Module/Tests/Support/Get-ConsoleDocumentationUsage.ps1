function Get-ConsoleDocumentationUsage {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Source
    )

    $snippets = if ([IO.Path]::GetExtension($Path) -eq '.md') {
        # Fences contain runnable examples; inline code also documents command invocations.
        $withoutFences = [regex]::Replace($Source, '(?ms)^```[^\r\n]*\r?\n.*?^```', '')
        @([regex]::Matches($Source, '(?ms)^```(?:powershell|pwsh|ps1)?[^\r\n]*\r?\n(.*?)^```') | ForEach-Object { $_.Groups[1].Value }) +
            @([regex]::Matches($withoutFences, '(?<!`)`([^`\r\n]+)`(?!`)') | ForEach-Object { $_.Groups[1].Value })
    } else { @($Source) }

    foreach ($snippet in $snippets) {
        $tokens = $null
        $parseErrors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($snippet, [ref] $tokens, [ref] $parseErrors)
        foreach ($command in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)) {
            $name = $command.GetCommandName()
            if ($name -notmatch '^(?:ConsoleMonster\\)?[A-Za-z]+-Console[\w]+$') { continue }
            [pscustomobject]@{
                Path = $Path
                Command = ($name -replace '^ConsoleMonster\\', '')
                Parameters = @($command.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] } | ForEach-Object { $_.ParameterName })
                Source = $command.Extent.Text
            }
        }
    }
}
