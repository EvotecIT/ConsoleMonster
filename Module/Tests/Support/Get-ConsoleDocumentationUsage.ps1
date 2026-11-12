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
    } elseif ([IO.Path]::GetExtension($Path) -eq '.cs') {
        # Hosted examples embed PowerShell in C# string literals.
        foreach ($literal in [regex]::Matches($Source, '(?s)"""(?<raw>.*?)"""|@"(?<verbatim>(?:""|[^"])*)"|"(?<escaped>(?:\\.|[^"\\])*)"')) {
            if ($literal.Groups['raw'].Success) { $literal.Groups['raw'].Value }
            elseif ($literal.Groups['verbatim'].Success) { $literal.Groups['verbatim'].Value.Replace('""', '"') }
            else { [regex]::Unescape($literal.Groups['escaped'].Value) }
        }
    } else { @($Source) }

    foreach ($snippet in $snippets) {
        $tokens = $null
        $parseErrors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($snippet, [ref] $tokens, [ref] $parseErrors)
        foreach ($command in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)) {
            $name = $command.GetCommandName()
            if ($name -notmatch '^(?:ConsoleMonster\\)?[A-Za-z]+-Console[\w]*$') { continue }
            $parameters = @($command.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] } | ForEach-Object { $_.ParameterName })
            foreach ($splat in $command.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.VariableExpressionAst] -and $_.Splatted }) {
                # Static hashtables in the same snippet are ordinary documented splats.
                # Dynamic splats cannot be resolved without executing the example.
                $assignments = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true) |
                    Where-Object { $_.Left -is [System.Management.Automation.Language.VariableExpressionAst] -and $_.Left.VariablePath.UserPath -eq $splat.VariablePath.UserPath -and $_.Extent.StartOffset -lt $command.Extent.StartOffset })
                if ($assignments.Count -eq 0) { continue }
                $table = $assignments[-1].Right.Find({ param($node) $node -is [System.Management.Automation.Language.HashtableAst] }, $true)
                if ($null -ne $table) {
                    $parameters += @($table.KeyValuePairs | Where-Object { $_.Item1 -is [System.Management.Automation.Language.StringConstantExpressionAst] } | ForEach-Object { $_.Item1.Value })
                }
            }
            [pscustomobject]@{
                Path = $Path
                Command = ($name -replace '^ConsoleMonster\\', '')
                Parameters = $parameters
                Source = $command.Extent.Text
            }
        }
    }
}
