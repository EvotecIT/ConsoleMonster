#Requires -Version 7.0
<#
.SYNOPSIS
    Lays out example PowerShell scripts at 100 columns, the way they are written by hand.
.DESCRIPTION
    A line longer than -Width breaks only where PowerShell itself allows a line break, so no backtick is ever added:
    between the entries of a hashtable (data rows fill their lines), inside an inline script block, after a pipe, and
    after the commas of an array or method argument list. The outermost structure on the line breaks first. A semicolon
    that separated two entries or statements is dropped when a line break takes its place, and the result is checked
    token for token against the original.

    A command that still does not fit, and starts its own statement, has its named parameters moved into a hashtable it
    splats (`$dashboardParameters = @{ ... }` then `New-ConsoleDashboard @dashboardParameters`); script block arguments and
    positional values stay on the command. That step is checked by binding: the same parameters with the same values.
.PARAMETER Path
    Scripts to lay out.
.PARAMETER Check
    Reports the scripts that are not laid out instead of writing them, and exits with 1 when there are any.
.EXAMPLE
    pwsh Website/build/Format-ExampleScript.ps1 -Path Website/examples/gallery/fleet/fleet.ps1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, ValueFromPipeline)][string[]] $Path,
    [int] $Width = 100,
    [switch] $Check
)
begin {
    $ErrorActionPreference = 'Stop'
    $Step = 4

    function Read-Script([string] $Text) {
        $tokens = $null
        $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($Text, [ref] $tokens, [ref] $errors)
        [pscustomobject] @{ Ast = $ast; Tokens = $tokens; Errors = $errors }
    }

    # Line starts, so offsets map to lines and columns.
    function Get-LineStart([string] $Text) {
        $starts = [System.Collections.Generic.List[int]]::new()
        $starts.Add(0)
        for ($i = 0; $i -lt $Text.Length; $i++) {
            if ($Text[$i] -eq "`n") { $starts.Add($i + 1) }
        }
        , $starts
    }

    function Get-LineOf($Starts, [int] $Offset) {
        $low = 0
        $high = $Starts.Count - 1
        while ($low -lt $high) {
            $mid = [int][math]::Ceiling(($low + $high) / 2)
            if ($Starts[$mid] -le $Offset) { $low = $mid } else { $high = $mid - 1 }
        }
        $low
    }

    function Get-Indent([string] $Text, $Starts, [int] $Offset) {
        $i = $Starts[(Get-LineOf $Starts $Offset)]
        $n = 0
        while ($i + $n -lt $Text.Length -and $Text[$i + $n] -eq ' ') { $n++ }
        $n
    }

    # The gap before a break point: from the end of the previous non-space text (a separating ';' included) to the point.
    function New-Break([string] $Text, [int] $Offset, [int] $Indent, [switch] $DropSeparator) {
        $start = $Offset
        while ($start -gt 0 -and $Text[$start - 1] -eq ' ') { $start-- }
        if ($DropSeparator -and $start -gt 0 -and $Text[$start - 1] -eq ';') { $start-- }
        [pscustomobject] @{ Start = $start; End = $Offset; Text = "`n" + (' ' * $Indent) }
    }

    function Test-Broken([string] $Text, [int] $Offset) {
        $i = $Offset - 1
        while ($i -ge 0 -and ($Text[$i] -eq ' ' -or $Text[$i] -eq ';')) { $i-- }
        $i -lt 0 -or $Text[$i] -eq "`n"
    }

    function Test-OnOneLine($Starts, $Extent) {
        (Get-LineOf $Starts $Extent.StartOffset) -eq (Get-LineOf $Starts ($Extent.EndOffset - 1))
    }

    # The break points of one node, outermost structures first: [Kind, Offsets, Fill, Drop].
    function Get-Point($Node, [string] $Text, $Starts) {
        switch ($Node) {
            { $_ -is [System.Management.Automation.Language.HashtableAst] -and $_.KeyValuePairs.Count -ge 2 } {
                return @{ Kind = 'Entries'; Offsets = @($_.KeyValuePairs | Select-Object -Skip 1 | ForEach-Object { $_.Item1.Extent.StartOffset }); Fill = $true; Drop = $true; Tier = 0 }
            }
            { $_ -is [System.Management.Automation.Language.ScriptBlockExpressionAst] -and (Test-OnOneLine $Starts $_.Extent) } {
                $body = $_.ScriptBlock.EndBlock
                if ($null -eq $body -or $body.Statements.Count -eq 0 -or $_.ScriptBlock.ParamBlock) { return $null }
                return @{ Kind = 'Block'; Offsets = @($body.Statements | ForEach-Object { $_.Extent.StartOffset }); Close = $_.Extent.EndOffset - 1; Fill = $false; Drop = $true; Tier = 0 }
            }
            { $_ -is [System.Management.Automation.Language.StatementBlockAst] -and $_.Statements.Count -gt 0 -and (Test-OnOneLine $Starts $_.Extent) -and $Text[$_.Extent.StartOffset] -eq '{' } {
                return @{ Kind = 'Block'; Offsets = @($_.Statements | ForEach-Object { $_.Extent.StartOffset }); Close = $_.Extent.EndOffset - 1; Fill = $false; Drop = $true; Tier = 0 }
            }
            { $_ -is [System.Management.Automation.Language.IfStatementAst] -and ($_.Clauses.Count -ge 2 -or $_.ElseClause) } {
                # elseif and else start their own lines, at the if's own indent.
                $offsets = @()
                for ($i = 1; $i -lt $_.Clauses.Count; $i++) {
                    $from = $_.Clauses[$i - 1].Item2.Extent.EndOffset
                    $offsets += $from + $Text.Substring($from, $_.Clauses[$i].Item1.Extent.StartOffset - $from).IndexOf('elseif')
                }
                if ($_.ElseClause) {
                    $from = $_.Clauses[$_.Clauses.Count - 1].Item2.Extent.EndOffset
                    $offsets += $from + $Text.Substring($from, $_.ElseClause.Extent.StartOffset - $from).IndexOf('else')
                }
                return @{ Kind = 'Clauses'; Offsets = $offsets; Fill = $false; Drop = $false; Tier = 0 }
            }
            { $_ -is [System.Management.Automation.Language.PipelineAst] -and $_.PipelineElements.Count -ge 2 } {
                # A short pipeline inside ( ), $( ) or @( ) breaks only when nothing else helps.
                $holder = if ($_.Parent -is [System.Management.Automation.Language.StatementBlockAst]) { $_.Parent.Parent } else { $_.Parent }
                $nested = $holder -is [System.Management.Automation.Language.ParenExpressionAst] -or $holder -is [System.Management.Automation.Language.SubExpressionAst] -or $holder -is [System.Management.Automation.Language.ArrayExpressionAst]
                $tier = if ($nested -and $_.PipelineElements.Count -lt 3) { 3 } else { 0 }
                return @{ Kind = 'Pipeline'; Offsets = @($_.PipelineElements | Select-Object -Skip 1 | ForEach-Object { $_.Extent.StartOffset }); Fill = $false; Drop = $false; Tier = $tier }
            }
            { $_ -is [System.Management.Automation.Language.ArrayLiteralAst] -and $_.Elements.Count -ge 2 } {
                return @{ Kind = 'Array'; Offsets = @($_.Elements | Select-Object -Skip 1 | ForEach-Object { $_.Extent.StartOffset }); Fill = $true; Drop = $false; Tier = 1 }
            }
            { $_ -is [System.Management.Automation.Language.InvokeMemberExpressionAst] -and $_.Arguments.Count -ge 2 } {
                return @{ Kind = 'Arguments'; Offsets = @($_.Arguments | Select-Object -Skip 1 | ForEach-Object { $_.Extent.StartOffset }); Fill = $true; Drop = $false; Tier = 1 }
            }
        }
        $null
    }

    function Invoke-Break([string] $Text, $Line) {
        $script = Read-Script $Text
        $starts = Get-LineStart $Text
        $lineStart = $starts[$Line]
        $lineEnd = if ($Line + 1 -lt $starts.Count) { $starts[$Line + 1] - 1 } else { $Text.Length }
        $lineIndent = $lineStart + (Get-Indent $Text $starts $lineStart)
        # Strings that span lines (here-strings) cannot move.
        $nodes = $script.Ast.FindAll({ param($n) $n.Extent.StartOffset -lt $lineEnd -and $n.Extent.EndOffset -gt $lineStart }, $true)
        $candidates = foreach ($node in $nodes) {
            $point = Get-Point $node $Text $starts
            if ($null -eq $point) { continue }
            # Nothing inside a string's $( ) moves: the line break would become part of the string.
            $inString = $false
            for ($parent = $node.Parent; $parent; $parent = $parent.Parent) {
                if ($parent -is [System.Management.Automation.Language.ExpandableStringExpressionAst]) { $inString = $true; break }
            }
            if ($inString) { continue }
            if ($node.FindAll({ param($n) $n -is [System.Management.Automation.Language.StringConstantExpressionAst] -or $n -is [System.Management.Automation.Language.ExpandableStringExpressionAst] }, $true) |
                Where-Object { -not (Test-OnOneLine $starts $_.Extent) }) { continue }
            $open = @($point.Offsets | Where-Object { -not (Test-Broken $Text $_) })
            if (-not ($open | Where-Object { $_ -gt $lineIndent -and $_ -lt $lineEnd })) { continue }
            [pscustomobject] @{ Node = $node; Point = $point; Open = $open; Whole = $open.Count -eq $point.Offsets.Count; Tier = $point.Tier; Order = $node.Extent.StartOffset }
        }
        $candidates = @($candidates | Sort-Object -Stable -Property Tier)
        $splatTried = $false
        foreach ($candidate in $candidates) {
            if ($candidate.Tier -ge 3 -and -not $splatTried) {
                $splatTried = $true
                $splatted = Invoke-Splat $Text $script $starts $lineStart $lineEnd
                if ($null -ne $splatted) { return $splatted }
            }
            $indent = (Get-Indent $Text $starts $candidate.Node.Extent.StartOffset) + $Step
            $edits = [System.Collections.Generic.List[object]]::new()
            if ($candidate.Point.Kind -eq 'Clauses') {
                foreach ($offset in $candidate.Open) { $edits.Add((New-Break $Text $offset ($indent - $Step))) }
                # The clauses keep the if's indent, so lines inside them do not move.
                $builder = [System.Text.StringBuilder]::new($Text)
                foreach ($edit in @($edits | Sort-Object -Property Start -Descending)) { $null = $builder.Remove($edit.Start, $edit.End - $edit.Start).Insert($edit.Start, $edit.Text) }
                return $builder.ToString()
            } elseif ($candidate.Point.Kind -eq 'Block') {
                foreach ($offset in $candidate.Point.Offsets) { $edits.Add((New-Break $Text $offset $indent -DropSeparator)) }
                $edits.Add((New-Break $Text $candidate.Point.Close ($indent - $Step)))
            } elseif ($candidate.Point.Fill -and $candidate.Whole) {
                # Values fill their lines: a break goes in only where the next part would pass the width.
                $offsets = $candidate.Point.Offsets
                $column = $offsets[0] - $starts[(Get-LineOf $starts $offsets[0])]
                for ($i = 0; $i -lt $offsets.Count; $i++) {
                    $end = if ($i + 1 -lt $offsets.Count) { $offsets[$i + 1] } else { $lineEnd }
                    $part = $Text.Substring($offsets[$i], $end - $offsets[$i])
                    $length = $part.TrimEnd(' ', ';').Length
                    if ($column + $length -gt $Width -and $column -gt $indent) {
                        $edits.Add((New-Break $Text $offsets[$i] $indent -DropSeparator:$candidate.Point.Drop))
                        $column = $indent
                    }
                    $column += $part.Length
                }
                if ($edits.Count -eq 0) { continue }
            } else {
                foreach ($offset in $candidate.Open) { $edits.Add((New-Break $Text $offset $indent -DropSeparator:$candidate.Point.Drop)) }
            }
            # Lines already inside the node move with the part they belong to.
            $first = ($edits | Measure-Object -Property Start -Minimum).Minimum
            for ($i = $first; $i -lt $candidate.Node.Extent.EndOffset; $i++) {
                if ($Text[$i] -eq "`n" -and $i + 1 -lt $Text.Length -and $Text[$i + 1] -ne "`n") {
                    $edits.Add([pscustomobject] @{ Start = $i + 1; End = $i + 1; Text = ' ' * $Step })
                }
            }
            $builder = [System.Text.StringBuilder]::new($Text)
            foreach ($edit in @($edits | Sort-Object -Property Start -Descending)) {
                $null = $builder.Remove($edit.Start, $edit.End - $edit.Start).Insert($edit.Start, $edit.Text)
            }
            return $builder.ToString()
        }
        if ($splatTried) { return $null }
        Invoke-Splat $Text $script $starts $lineStart $lineEnd
    }

    # The statement a command is part of, when that statement starts its line: Cmd ..., $x | Cmd ..., $x = Cmd ...,
    # return Cmd ...
    function Get-OwnStatement($Command, [string] $Text, $Starts) {
        $pipeline = $Command.Parent
        if ($pipeline -isnot [System.Management.Automation.Language.PipelineAst]) { return $null }
        $statement = $pipeline
        if ($pipeline.Parent -is [System.Management.Automation.Language.AssignmentStatementAst] -and $pipeline.Parent.Right -eq $pipeline) { $statement = $pipeline.Parent }
        elseif ($pipeline.Parent -is [System.Management.Automation.Language.ReturnStatementAst]) { $statement = $pipeline.Parent }
        # The first condition of an if is evaluated first anyway, so its hashtable can go before the if.
        elseif ($pipeline.Parent -is [System.Management.Automation.Language.IfStatementAst] -and $pipeline.Parent.Clauses[0].Item1 -eq $pipeline) { $statement = $pipeline.Parent }
        $start = $statement.Extent.StartOffset
        if ($start -ne $Starts[(Get-LineOf $Starts $start)] + (Get-Indent $Text $Starts $start)) { return $null }
        $statement
    }

    # A value as a hashtable entry writes it: a command-mode bareword (-ErrorAction Stop) becomes a quoted string.
    function ConvertTo-Value($Value) {
        if ($null -eq $Value) { return '$true' }
        if ($Value -is [System.Management.Automation.Language.StringConstantExpressionAst] -and $Value.StringConstantType -eq 'BareWord') {
            return "'" + $Value.Value.Replace("'", "''") + "'"
        }
        if ($Value -is [System.Management.Automation.Language.ArrayLiteralAst]) { return ($Value.Elements | ForEach-Object { ConvertTo-Value $_ }) -join ', ' }
        $Value.Extent.Text
    }

    # Named parameters with their values; script blocks, positional values and splats stay on the command.
    function Get-Splattable($Command) {
        $elements = $Command.CommandElements
        $named = [System.Collections.Generic.List[object]]::new()
        for ($i = 1; $i -lt $elements.Count; $i++) {
            $element = $elements[$i]
            if ($element -isnot [System.Management.Automation.Language.CommandParameterAst]) { continue }
            $value = $element.Argument
            $last = $element
            if ($null -eq $value -and $i + 1 -lt $elements.Count -and $elements[$i + 1] -isnot [System.Management.Automation.Language.CommandParameterAst]) {
                $value = $elements[$i + 1]
                $last = $value
            }
            if ($value -is [System.Management.Automation.Language.ScriptBlockExpressionAst]) { continue }
            if ($null -ne $value -and $value.Extent.Text.Contains("`n")) { continue }
            $named.Add([pscustomobject] @{ Name = $element.ParameterName; Value = ConvertTo-Value $value; First = $element; Last = $last })
        }
        , $named
    }

    function Get-SplatName($Command, $Ast) {
        $noun = ($Command.GetCommandName() -split '-', 2)[-1]
        if ($noun -cmatch '^Console(?=[A-Z])') { $noun = $noun.Substring(7) }
        $base = $noun.Substring(0, 1).ToLowerInvariant() + $noun.Substring(1) + 'Parameters'
        # A name already used for something other than these splats gets a number.
        for ($n = 1; ; $n++) {
            $name = if ($n -eq 1) { $base } else { $base + $n }
            $uses = $Ast.FindAll({ param($v) $v -is [System.Management.Automation.Language.VariableExpressionAst] -and $v.VariablePath.UserPath -eq $name }, $true)
            $foreign = $uses | Where-Object { -not $_.Splatted -and -not ($_.Parent -is [System.Management.Automation.Language.AssignmentStatementAst] -and $_.Parent.Left -eq $_ -and
                    $_.Parent.Right.Expression -is [System.Management.Automation.Language.HashtableAst]) }
            if (-not $foreign) { return $name }
        }
    }

    function Invoke-Splat([string] $Text, $Script, $Starts, [int] $LineStart, [int] $LineEnd) {
        if ($script:NoSplat) { return $null }
        # Commands whose moved parameters sit on the long line, not commands whose script block merely contains it.
        $commands = $Script.Ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.Extent.StartOffset -lt $LineEnd -and $n.Extent.EndOffset -gt $LineStart }, $true)
        foreach ($command in $commands) {
            $inString = $false
            for ($parent = $command.Parent; $parent; $parent = $parent.Parent) { if ($parent -is [System.Management.Automation.Language.ExpandableStringExpressionAst]) { $inString = $true; break } }
            # Cmdlets and functions only (Verb-Noun): a native command's arguments are not parameters.
            if ($inString -or $command.GetCommandName() -notmatch '^[A-Za-z]+-[A-Za-z]+$') { continue }
            $statement = Get-OwnStatement $command $Text $Starts
            if ($null -eq $statement) { continue }
            $named = Get-Splattable $command
            if ($named.Count -lt 2 -or @($named.Name | Sort-Object -Unique).Count -ne $named.Count) { continue }
            if (-not ($named | Where-Object { $_.First.Extent.StartOffset -ge $LineStart -and $_.First.Extent.StartOffset -lt $LineEnd })) { continue }
            $name = Get-SplatName $command $Script.Ast
            $indent = ' ' * (Get-Indent $Text $Starts $statement.Extent.StartOffset)
            $table = '$' + $name + " = @{`n" + (($named | ForEach-Object { $indent + '    ' + $_.Name + ' = ' + $_.Value }) -join "`n") + "`n" + $indent + "}`n" + $indent
            # The command keeps its other elements in order; the splat takes the place of the first moved parameter.
            $moved = @($named | ForEach-Object { $_.First; if ($_.Last -ne $_.First) { $_.Last } })
            $parts = [System.Collections.Generic.List[string]]::new()
            $splatted = $false
            foreach ($element in $command.CommandElements) {
                if ($moved -contains $element) {
                    if (-not $splatted) { $parts.Add('@' + $name); $splatted = $true }
                    continue
                }
                $parts.Add($element.Extent.Text)
            }
            $builder = [System.Text.StringBuilder]::new($Text)
            $null = $builder.Remove($command.Extent.StartOffset, $command.Extent.EndOffset - $command.Extent.StartOffset).Insert($command.Extent.StartOffset, $parts -join ' ')
            $null = $builder.Insert($statement.Extent.StartOffset, $table)
            $script:Splats++
            return $builder.ToString()
        }
        $null
    }

    function Format-Script([string] $Text) {
        $from = 0
        for ($guard = 0; $guard -lt 5000; $guard++) {
            $lines = $Text.Split("`n")
            $line = -1
            for ($i = $from; $i -lt $lines.Count; $i++) {
                if ($lines[$i].TrimEnd("`r").Length -gt $Width -and -not $lines[$i].TrimStart().StartsWith('#')) { $line = $i; break }
            }
            if ($line -lt 0) { return $Text }
            $from = $line
            $broken = Invoke-Break $Text $line
            if ($null -eq $broken) { $from++ } else { $Text = $broken }
        }
        throw 'Script layout did not settle.'
    }

    # Everything but line breaks and the separators they replace must stay exactly the same.
    function Get-Signature([string] $Text) {
        $script = Read-Script $Text
        if ($script.Errors.Count -gt 0) { throw "Parse error: $($script.Errors[0].Message)" }
        ($script.Tokens | Where-Object { $_.Kind -notin 'NewLine', 'Semi', 'EndOfInput', 'LineContinuation' } | ForEach-Object { "$($_.Kind):$($_.Text)" }) -join "`u{1}"
    }

    # Each command with its parameters and values, splats expanded from the hashtable assigned just before: the same
    # binding means the same call.
    function Get-Binding([string] $Text) {
        # Script blocks are compared through the commands inside them, which are listed on their own.
        function Get-Shown($Value) {
            if ($Value -is [System.Management.Automation.Language.ScriptBlockExpressionAst]) { return '{...}' }
            ConvertTo-Value $Value
        }
        $script = Read-Script $Text
        if ($script.Errors.Count -gt 0) { throw "Parse error: $($script.Errors[0].Message)" }
        $tables = @{}
        $lines = foreach ($command in $script.Ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] }, $true)) {
            $pairs = [System.Collections.Generic.List[string]]::new()
            $elements = $command.CommandElements
            for ($i = 1; $i -lt $elements.Count; $i++) {
                $element = $elements[$i]
                if ($element -is [System.Management.Automation.Language.VariableExpressionAst] -and $element.Splatted) {
                    $assignment = $script.Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.AssignmentStatementAst] -and $a.Left.VariablePath.UserPath -eq $element.VariablePath.UserPath -and
                            $a.Extent.EndOffset -le $command.Extent.StartOffset -and $a.Right.Expression -is [System.Management.Automation.Language.HashtableAst] }, $true) | Select-Object -Last 1
                    if ($assignment -and $element.VariablePath.UserPath -like '*Parameters*') {
                        foreach ($pair in $assignment.Right.Expression.KeyValuePairs) { $pairs.Add('-' + $pair.Item1.Value + '=' + ($pair.Item2.Extent.Text -replace '[\s;]+', ' ')) }
                        continue
                    }
                    $pairs.Add('@' + $element.VariablePath.UserPath)
                    continue
                }
                if ($element -is [System.Management.Automation.Language.CommandParameterAst]) {
                    $value = if ($element.Argument) { Get-Shown $element.Argument }
                    elseif ($i + 1 -lt $elements.Count -and $elements[$i + 1] -isnot [System.Management.Automation.Language.CommandParameterAst] -and -not ($elements[$i + 1] -is [System.Management.Automation.Language.VariableExpressionAst] -and $elements[$i + 1].Splatted)) { $i++; Get-Shown $elements[$i] }
                    else { '$true' }
                    $pairs.Add('-' + $element.ParameterName + '=' + ($value -replace '[\s;]+', ' '))
                    continue
                }
                $pairs.Add(((Get-Shown $element) -replace '[\s;]+', ' '))
            }
            $command.GetCommandName() + ' ' + (($pairs | Sort-Object) -join ' ')
        }
        ($lines | Sort-Object) -join "`n"
    }

    $unformatted = [System.Collections.Generic.List[string]]::new()
}
process {
    foreach ($file in $Path) {
        $full = (Resolve-Path -LiteralPath $file).ProviderPath
        $bytes = [System.IO.File]::ReadAllBytes($full)
        $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $original = [System.IO.File]::ReadAllText($full)
        $crlf = $original.Contains("`r`n")
        $text = $original.Replace("`r`n", "`n")
        $script:NoSplat = $false
        $script:Splats = 0
        $laid = Format-Script $text
        if ($script:Splats -gt 0) {
            $same = try { (Get-Binding $laid) -eq (Get-Binding $text) } catch { $false }
            if (-not $same) {
                # The whitespace layout alone, which is checked token for token below.
                Write-Warning "Splatting would change what a command receives in $file; laid out without it."
                $script:NoSplat = $true
                $script:Splats = 0
                $laid = Format-Script $text
            }
        }
        if ($laid -eq $text) { continue }
        if ($script:Splats -eq 0 -and (Get-Signature $laid) -ne (Get-Signature $text)) { throw "Layout changed more than whitespace in $file." }
        if ($Check) { $unformatted.Add($file); continue }
        if ($crlf) { $laid = $laid.Replace("`n", "`r`n") }
        [System.IO.File]::WriteAllText($full, $laid, [System.Text.UTF8Encoding]::new($bom))
        "laid out $file"
    }
}
end {
    if ($Check -and $unformatted.Count -gt 0) {
        $unformatted | ForEach-Object { "not laid out: $_" }
        exit 1
    }
}
