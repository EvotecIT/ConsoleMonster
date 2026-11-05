BeforeDiscovery {
    $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    # Docs/Proposals holds design proposals whose syntax is intentionally unreleased.
    $documents = @(Get-Item (Join-Path $repositoryRoot 'README.md'))
    $documents += @(Get-ChildItem (Join-Path $repositoryRoot 'Docs') -Filter *.md -File)
    $script:DocumentCases = @(
        foreach ($document in $documents) {
            @{ Name = $document.FullName.Substring($repositoryRoot.Length + 1); Path = $document.FullName }
        }
    )
}

Describe 'PowerShell samples in documentation' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force

        function Get-DocumentPowerShellBlocks {
            param([string] $Path)
            $lines = [IO.File]::ReadAllLines($Path)
            $block = $null
            for ($index = 0; $index -lt $lines.Length; $index++) {
                $line = $lines[$index]
                if ($null -eq $block) {
                    if ($line -match '^\s*(```+|~~~+)\s*(powershell|pwsh|ps1|ps)\s*$') {
                        $block = @{ Fence = $Matches[1]; StartLine = $index + 2; Lines = New-Object System.Collections.Generic.List[string] }
                    } elseif ($line -match '^\s*(```+|~~~+)') {
                        # Skip other fenced blocks so their contents are never read as fences.
                        $fence = $Matches[1]
                        while (++$index -lt $lines.Length -and $lines[$index] -notmatch ('^\s*' + [regex]::Escape($fence) + '\s*$')) { }
                    }
                } elseif ($line -match ('^\s*' + [regex]::Escape($block.Fence) + '\s*$')) {
                    [pscustomobject]@{ StartLine = $block.StartLine; Text = ($block.Lines -join "`n") }
                    $block = $null
                } else {
                    $block.Lines.Add($line)
                }
            }
        }

        function Resolve-SampleParameter {
            param([System.Management.Automation.CommandInfo] $Command, [string] $Name)
            # psbase: a parameter named Values would otherwise shadow the dictionary property.
            $parameters = @($Command.Parameters.psbase.Values)
            $names = foreach ($parameter in $parameters) { $parameter.Name; $parameter.Aliases }
            if ($names -contains $Name) { return $true }
            # PowerShell accepts an unambiguous prefix of a parameter name.
            $prefixed = @($parameters | Where-Object { $_.Name -like "$Name*" -or @($_.Aliases | Where-Object { $_ -like "$Name*" }).Count -gt 0 })
            return $prefixed.Count -eq 1
        }
    }

    It 'uses only real ConsoleMonster commands and parameters in <Name>' -ForEach $script:DocumentCases {
        $commands = @{}
        foreach ($command in Get-Command -Module ConsoleMonster) { $commands[$command.Name] = $command }
        $failures = New-Object System.Collections.Generic.List[string]

        foreach ($block in Get-DocumentPowerShellBlocks $Path) {
            $tokens = $null
            $errors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput($block.Text, [ref] $tokens, [ref] $errors)
            foreach ($parseError in $errors) {
                $failures.Add(('{0}:{1}: parse error: {2}' -f $Name, ($block.StartLine + $parseError.Extent.StartLineNumber - 1), $parseError.Message))
            }
            $commandAsts = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
            foreach ($commandAst in $commandAsts) {
                $commandName = $commandAst.GetCommandName()
                if ($commandName -notmatch '^[A-Za-z]+-Console\w*$') { continue }
                $line = $block.StartLine + $commandAst.Extent.StartLineNumber - 1
                $command = $commands[$commandName]
                if ($null -eq $command) {
                    $failures.Add(('{0}:{1}: unknown command {2}' -f $Name, $line, $commandName))
                    continue
                }
                foreach ($parameterAst in $commandAst.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] }) {
                    if (-not (Resolve-SampleParameter $command $parameterAst.ParameterName)) {
                        $failures.Add(('{0}:{1}: {2} has no parameter -{3}' -f $Name, ($block.StartLine + $parameterAst.Extent.StartLineNumber - 1), $commandName, $parameterAst.ParameterName))
                    }
                }
            }
        }

        ($failures -join "`n") | Should -BeNullOrEmpty -Because 'documented samples must use the shipped commands and parameters'
    }
}
